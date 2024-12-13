using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Frameworks;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace Starward.Features.HoYoPlay;

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
        LoadCachedGameInfo();
    }




    private ConcurrentDictionary<GameId, GameInfo> _gameInfo = new();


    private ConcurrentDictionary<GameId, GameBackgroundInfo> _gameBackground = new();


    private ConcurrentDictionary<GameId, GameContent> _gameContent = new();


    private ConcurrentDictionary<GameId, GamePackage> _gamePackage = new();


    private ConcurrentDictionary<GameId, GameConfig> _gameConfig = new();



    private void LoadCachedGameInfo()
    {
        try
        {
            string? json = AppSetting.CachedGameInfo;
            if (!string.IsNullOrWhiteSpace(json))
            {
                var infos = JsonSerializer.Deserialize<List<GameInfo>>(json);
                if (infos is not null)
                {
                    foreach (var item in infos)
                    {
                        _gameInfo[item] = item;
                    }
                }
            }
        }
        catch { }
    }


    private void CacheGameInfo()
    {
        try
        {
            var infos = _gameInfo.Values.ToList();
            string json = JsonSerializer.Serialize(infos);
            AppSetting.CachedGameInfo = json;
        }
        catch { }
    }



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
            CacheGameInfo();
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
                _gameInfo[item] = item;
            }
            List<GameBackgroundInfo> backgrounds = await _client.GetGameBackgroundAsync(launcherId, language);
            foreach (GameBackgroundInfo item in backgrounds)
            {
                _gameBackground[item.GameId] = item;
            }
            foreach (var item in infos)
            {
                GameContent content = await _client.GetGameContentAsync(launcherId, language, item);
                _gameContent[content.GameId] = content;
            }
            List<GamePackage> packages = await _client.GetGamePackageAsync(launcherId, language);
            foreach (GamePackage item in packages)
            {
                _gamePackage[item.GameId] = item;
            }
            List<GameConfig> configs = await _client.GetGameConfigAsync(launcherId, language);
            foreach (GameConfig item in configs)
            {
                _gameConfig[item.GameId] = item;
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
                    _gameInfo[item] = item;
                }
                List<GameBackgroundInfo> backgrounds = await _client.GetGameBackgroundAsync(launcherId, language);
                foreach (GameBackgroundInfo item in backgrounds)
                {
                    _gameBackground[item.GameId] = item;
                }
                foreach (GameInfo item in infos)
                {
                    GameContent content = await _client.GetGameContentAsync(launcherId, language, item);
                    _gameContent[item] = content;
                }
                List<GamePackage> packages = await _client.GetGamePackageAsync(launcherId, language);
                foreach (GamePackage item in packages)
                {
                    _gamePackage[item.GameId] = item;
                }
                List<GameConfig> configs = await _client.GetGameConfigAsync(launcherId, language);
                foreach (GameConfig item in configs)
                {
                    _gameConfig[item.GameId] = item;
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
            List<(string Url, bool InBg)> urls = [];
            foreach (GameBiz biz in bizs)
            {
                if (GameId.FromGameBiz(biz) is GameId gameId)
                {
                    if (_gameInfo.TryGetValue(gameId, out GameInfo? info))
                    {
                        urls.Add((info.Display.Background.Url, true));
                    }
                    if (_gameBackground.TryGetValue(gameId, out GameBackgroundInfo? background))
                    {
                        urls.AddRange(background.Backgrounds.Select(x => (x.Background.Url, false)));
                    }
                }
            }
            string bg = Path.Combine(AppConfig.UserDataFolder, "bg");
            string cache = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\cache");
            Directory.CreateDirectory(bg);
            Directory.CreateDirectory(cache);
            await Parallel.ForEachAsync(urls, async (item, _) =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(item.Url))
                    {
                        return;
                    }
                    string name = Path.GetFileName(item.Url);
                    string path = item.InBg ? Path.Combine(bg, name) : Path.Combine(cache, name);
                    if (!File.Exists(path))
                    {
                        byte[] bytes = await _httpClient.GetByteArrayAsync(item.Url);
                        await File.WriteAllBytesAsync(path, bytes);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Download image: {url}", item);
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




    public async Task<GameInfo> GetGameInfoAsync(GameId gameId)
    {
        if (!_gameInfo.TryGetValue(gameId, out GameInfo? info))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameInfoAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _gameInfo[item] = item;
            }
            info = list.First(x => x == gameId);
        }
        return info;
    }



    public GameInfo? GetCachedGameInfo(GameBiz gameBiz)
    {
        return _gameInfo.Values.FirstOrDefault(x => x.GameBiz == gameBiz);
    }


    public List<GameInfo> GetCachedGameInfos()
    {
        return _gameInfo.Values.ToList();
    }



    public async Task<GameBackgroundInfo> GetGameBackgroundAsync(GameId gameId)
    {
        if (!_gameBackground.TryGetValue(gameId, out GameBackgroundInfo? background))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameBackgroundAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _gameBackground[item.GameId] = item;
            }
            background = list.First(x => x.GameId == gameId);
        }
        return background;
    }



    public async Task<GameContent> GetGameContentAsync(GameId gameId)
    {
        if (!_gameContent.TryGetValue(gameId, out GameContent? content))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            content = await _client.GetGameContentAsync(LauncherId.FromGameId(gameId)!, lang, gameId);
            _gameContent[gameId] = content;
        }
        return content;
    }



    public async Task<GamePackage> GetGamePackageAsync(GameId gameId)
    {
        if (!_gamePackage.TryGetValue(gameId, out GamePackage? package))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGamePackageAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _gamePackage[item.GameId] = item;
            }
            package = list.First(x => x.GameId == gameId);
        }
        return package;
    }



    public async Task<GameConfig?> GetGameConfigAsync(GameId gameId)
    {
        if (!_gameConfig.TryGetValue(gameId, out GameConfig? config))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameConfigAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _gameConfig[item.GameId] = item;
            }
            config = list.FirstOrDefault(x => x.GameId == gameId);
        }
        return config;
    }



    public async Task<List<GameDeprecatedFile>> GetGameDeprecatedFilesAsync(GameId gameId)
    {
        var launcherId = LauncherId.FromGameId(gameId);
        if (launcherId is not null)
        {
            var fileConfig = await _client.GetGameDeprecatedFileConfigAsync(launcherId, "en-us", gameId);
            if (fileConfig != null)
            {
                return fileConfig.DeprecatedFiles;
            }
        }
        return [];
    }



    public async Task<GameChannelSDK?> GetGameChannelSDKAsync(GameId gameId)
    {
        var launcherId = LauncherId.FromGameId(gameId);
        if (launcherId is not null)
        {
            return await _client.GetGameChannelSDKAsync(launcherId, "en-us", gameId);
        }
        return null;
    }


}
