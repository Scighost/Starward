using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace Starward.Services;

public class HoYoPlayService
{


    private readonly ILogger<HoYoPlayService> _logger;

    private readonly HoYoPlayClient _client;

    private readonly HttpClient _httpClient;

    private readonly System.Timers.Timer _timer;


    public HoYoPlayService(ILogger<HoYoPlayService> logger, HoYoPlayClient client, HttpClient httpClient)
    {
        _logger = logger;
        _client = client;
        _httpClient = httpClient;
        _timer = new System.Timers.Timer
        {
            AutoReset = true,
            Enabled = true,
            Interval = TimeSpan.FromMinutes(10).TotalMilliseconds,
        };
        _timer.Elapsed += async (_, _) => await PrepareDataAsync();
    }




    private ConcurrentDictionary<GameBiz, GameInfo> _gameInfo = new();


    private ConcurrentDictionary<GameBiz, GameBackgroundInfo> _gameBackground = new();


    private ConcurrentDictionary<GameBiz, GameContent> _gameContent = new();


    private ConcurrentDictionary<GameBiz, GamePackage> _gamePackage = new();


    private ConcurrentDictionary<GameBiz, GameConfig> _gameConfig = new();



    public void ClearCache()
    {
        _gameInfo.Clear();
        _gameBackground.Clear();
        _gameContent.Clear();
        _gamePackage.Clear();
    }




    public async Task PrepareDataAsync()
    {
        try
        {
            ClearCache();
            string lang = CultureInfo.CurrentUICulture.Name;
            List<Task> tasks = [];
            tasks.Add(PrepareDataForServerAsync(LauncherId.ChinaOfficial, lang));
            tasks.Add(PrepareDataForServerAsync(LauncherId.GlobalOfficial, lang));
            tasks.Add(PrepareDataForBilibiliServerAsync(lang));
            await Task.WhenAll(tasks);
            await PrepareImagesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PrepareDataAsync));
        }
    }



    private async Task PrepareDataForServerAsync(string launcherId, string? language = null)
    {
        try
        {
            language ??= CultureInfo.CurrentUICulture.Name;
            List<GameInfo> infos = await _client.GetGameInfoAsync(launcherId, language);
            foreach (GameInfo item in infos)
            {
                _gameInfo[item.GameBiz] = item;
            }
            List<GameBackgroundInfo> backgrounds = await _client.GetGameBackgroundAsync(launcherId, language);
            foreach (GameBackgroundInfo item in backgrounds)
            {
                _gameBackground[item.GameId.GameBiz] = item;
            }
            foreach (var item in infos)
            {
                GameContent content = await _client.GetGameContentAsync(launcherId, language, item);
                _gameContent[content.GameId.GameBiz] = content;
            }
            List<GamePackage> packages = await _client.GetGamePackageAsync(launcherId, language);
            foreach (GamePackage item in packages)
            {
                _gamePackage[item.GameId.GameBiz] = item;
            }
            List<GameConfig> configs = await _client.GetGameConfigAsync(launcherId, language);
            foreach (GameConfig item in configs)
            {
                _gameConfig[item.GameId.GameBiz] = item;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PrepareDataForServerAsync));
        }
    }



    private async Task PrepareDataForBilibiliServerAsync(string? language = null)
    {
        try
        {
            language ??= CultureInfo.CurrentUICulture.Name;
            foreach ((GameBiz biz, string launcherId) in LauncherId.GetBilibiliLaunchers())
            {
                List<GameInfo> infos = await _client.GetGameInfoAsync(launcherId, language);
                foreach (GameInfo item in infos)
                {
                    _gameInfo[biz] = item;
                }
                List<GameBackgroundInfo> backgrounds = await _client.GetGameBackgroundAsync(launcherId, language);
                foreach (GameBackgroundInfo item in backgrounds)
                {
                    _gameBackground[biz] = item;
                }
                foreach (var item in infos)
                {
                    GameContent content = await _client.GetGameContentAsync(launcherId, language, item);
                    _gameContent[biz] = content;
                }
                List<GamePackage> packages = await _client.GetGamePackageAsync(launcherId, language);
                foreach (GamePackage item in packages)
                {
                    _gamePackage[biz] = item;
                }
                List<GameConfig> configs = await _client.GetGameConfigAsync(launcherId, language);
                foreach (GameConfig item in configs)
                {
                    _gameConfig[biz] = item;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PrepareDataForBilibiliServerAsync));
        }
    }



    private async Task PrepareImagesAsync()
    {
        try
        {
            List<GameBiz> bizs = GetSelectedGameBizs();
            List<string> urls = [];
            foreach (GameBiz biz in bizs)
            {
                if (_gameInfo.TryGetValue(biz, out GameInfo? info))
                {
                    urls.Add(info.Display.Background.Url);
                }
                if (_gameBackground.TryGetValue(biz, out GameBackgroundInfo? background))
                {
                    urls.AddRange(background.Backgrounds.Select(x => x.Background.Url));
                }
            }
            string bg = Path.Combine(AppConfig.UserDataFolder, "bg");
            Directory.CreateDirectory(bg);
            await Parallel.ForEachAsync(urls, async (url, _) =>
            {
                try
                {
                    string name = Path.GetFileName(url);
                    string path = Path.Combine(bg, name);
                    if (!File.Exists(path))
                    {
                        byte[] bytes = await _httpClient.GetByteArrayAsync(url);
                        await File.WriteAllBytesAsync(path, bytes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Download image: {url}", url);
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(PrepareImagesAsync));
        }
    }



    private static List<GameBiz> GetSelectedGameBizs()
    {
        List<GameBiz> bizs = new();
        foreach (string str in AppConfig.SelectedGameBizs?.Split(',') ?? [])
        {
            if (GameBiz.TryParse(str, out GameBiz biz))
            {
                bizs.Add(biz);
            }
        }
        return bizs;
    }




    public async Task<GameInfo> GetGameInfoAsync(GameBiz biz)
    {
        if (!_gameInfo.TryGetValue(biz, out GameInfo? info))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            if (biz.IsBilibili())
            {
                var list = await _client.GetGameInfoAsync(LauncherId.FromGameBiz(biz)!, lang);
                info = list.First();
                _gameInfo[biz] = info;
            }
            else
            {
                var list = await _client.GetGameInfoAsync(LauncherId.FromGameBiz(biz)!, lang);
                foreach (var item in list)
                {
                    _gameInfo[item.GameBiz] = item;
                }
                info = list.First(x => x.GameBiz == biz.ToString());
            }
        }
        return info;
    }



    public async Task<GameBackgroundInfo> GetGameBackgroundAsync(GameBiz biz)
    {
        if (!_gameBackground.TryGetValue(biz, out GameBackgroundInfo? background))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            if (biz.IsBilibili())
            {
                var list = await _client.GetGameBackgroundAsync(LauncherId.FromGameBiz(biz)!, lang);
                background = list.First();
                _gameBackground[biz] = background;
            }
            else
            {
                var list = await _client.GetGameBackgroundAsync(LauncherId.FromGameBiz(biz)!, lang);
                foreach (var item in list)
                {
                    _gameBackground[item.GameId.GameBiz] = item;
                }
                background = list.First(x => x.GameId.GameBiz == biz);
            }
        }
        return background;
    }



    public async Task<GameContent> GetGameContentAsync(GameBiz biz)
    {
        if (!_gameContent.TryGetValue(biz, out GameContent? content))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            if (biz.IsBilibili())
            {
                content = await _client.GetGameContentAsync(LauncherId.FromGameBiz(biz)!, lang, GameId.FromGameBiz(biz)!);
                _gameContent[biz] = content;
            }
            else
            {
                content = await _client.GetGameContentAsync(LauncherId.FromGameBiz(biz)!, lang, GameId.FromGameBiz(biz)!);
                _gameContent[biz] = content;
            }
        }
        return content;
    }



    public async Task<GamePackage> GetGamePackageAsync(GameBiz biz)
    {
        if (!_gamePackage.TryGetValue(biz, out GamePackage? package))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            if (biz.IsBilibili())
            {
                var list = await _client.GetGamePackageAsync(LauncherId.FromGameBiz(biz)!, lang);
                package = list.First();
                _gamePackage[biz] = package;
            }
            else
            {
                var list = await _client.GetGamePackageAsync(LauncherId.FromGameBiz(biz)!, lang);
                foreach (var item in list)
                {
                    _gamePackage[item.GameId.GameBiz] = item;
                }
                package = list.First(x => x.GameId.GameBiz == biz);
            }
        }
        return package;
    }



    public async Task<GameConfig?> GetGameConfigAsync(GameBiz biz)
    {
        if (biz.ToGame() == GameBiz.bh3 && biz.IsGlobalServer())
        {
            return null;
        }
        if (!_gameConfig.TryGetValue(biz, out GameConfig? config))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            if (biz.IsBilibili())
            {
                var list = await _client.GetGameConfigAsync(LauncherId.FromGameBiz(biz)!, lang);
                config = list.First();
                _gameConfig[biz] = config;
            }
            else
            {
                var list = await _client.GetGameConfigAsync(LauncherId.FromGameBiz(biz)!, lang);
                foreach (var item in list)
                {
                    _gameConfig[item.GameId.GameBiz] = item;
                }
                config = list.FirstOrDefault(x => x.GameId.GameBiz == biz);
            }
        }
        return config;
    }



    public async Task<List<GameDeprecatedFile>> GetGameDeprecatedFilesAsync(GameBiz biz)
    {
        if (biz.ToGame() == GameBiz.bh3 && biz.IsGlobalServer())
        {
            biz = GameBiz.bh3_global;
        }
        var launcherId = LauncherId.FromGameBiz(biz);
        var gameId = GameId.FromGameBiz(biz);
        if (launcherId != null && gameId != null)
        {
            var fileConfig = await _client.GetGameDeprecatedFileConfigAsync(launcherId, "en-us", gameId);
            if (fileConfig != null)
            {
                return fileConfig.DeprecatedFiles;
            }
        }
        return [];
    }



    public async Task<GameChannelSDK?> GetGameChannelSDKAsync(GameBiz biz)
    {
        if (biz.ToGame() == GameBiz.bh3 && biz.IsGlobalServer())
        {
            biz = GameBiz.bh3_global;
        }
        var launcherId = LauncherId.FromGameBiz(biz);
        var gameId = GameId.FromGameBiz(biz);
        if (launcherId != null && gameId != null)
        {
            return await _client.GetGameChannelSDKAsync(launcherId, "en-us", gameId);
        }
        return null;
    }


}