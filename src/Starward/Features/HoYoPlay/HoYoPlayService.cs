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
using System.Threading;
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
        _timer.Elapsed += (_, _) => ClearCache();
        LoadCachedGameInfo();
    }


    private List<GameInfo> _gameInfoList = new();


    private ConcurrentDictionary<GameId, GameInfo> _gameInfo = new();


    private ConcurrentDictionary<GameId, GameBackgroundInfo> _gameBackground = new();


    private ConcurrentDictionary<GameId, GameContent> _gameContent = new();


    private ConcurrentDictionary<GameId, GamePackage> _gamePackage = new();


    private ConcurrentDictionary<GameId, GameConfig> _gameConfig = new();


    private ConcurrentDictionary<GameId, GameChannelSDK> _gameChannelSDK = new();


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
                    _gameInfoList = infos;
                    foreach (var item in infos)
                    {
                        _gameInfo[item] = item;
                    }
                }
            }
        }
        catch { }
    }



    public void ClearCache()
    {
        _gameInfoList.Clear();
        _gameInfo.Clear();
        _gameBackground.Clear();
        _gameContent.Clear();
        _gamePackage.Clear();
        _gameChannelSDK.Clear();
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



    public GameInfo? GetCachedGameInfo(GameBiz biz)
    {
        return _gameInfo.Values.FirstOrDefault(x => x.GameBiz == biz);
    }



    public List<GameInfo> GetCachedGameInfoList()
    {
        return _gameInfoList;
    }



    public async Task<List<GameInfo>> UpdateGameInfoListAsync(CancellationToken cancellationToken = default)
    {
        List<GameInfo> infos = new List<GameInfo>();
        string lang = CultureInfo.CurrentUICulture.Name;
        if (LanguageUtil.FilterLanguage(lang) is "zh-cn")
        {
            infos.AddRange(await _client.GetGameInfoAsync(LauncherId.ChinaOfficial, lang, cancellationToken));
            infos.AddRange(await _client.GetGameInfoAsync(LauncherId.GlobalOfficial, lang, cancellationToken));
        }
        else
        {
            infos.AddRange(await _client.GetGameInfoAsync(LauncherId.GlobalOfficial, lang, cancellationToken));
            infos.AddRange(await _client.GetGameInfoAsync(LauncherId.ChinaOfficial, lang, cancellationToken));
        }
        foreach ((GameBiz _, string launcherId) in LauncherId.GetBilibiliLaunchers())
        {
            infos.AddRange(await _client.GetGameInfoAsync(launcherId, lang, cancellationToken));
        }
        _gameInfoList = infos;
        foreach (var item in infos)
        {
            _gameInfo[item] = item;
        }
        string json = JsonSerializer.Serialize(infos);
        AppSetting.CachedGameInfo = json;
        _ = DownloadGameVersionPosterAsync(infos);
        return infos;
    }



    private async Task DownloadGameVersionPosterAsync(List<GameInfo> infos)
    {
        try
        {
            List<string> urls = new();
            foreach (var info in infos)
            {
                if (!string.IsNullOrWhiteSpace(info.Display.Background?.Url))
                {
                    urls.Add(info.Display.Background.Url);
                }
            }
            if (AppSetting.UserDataFolder is not null)
            {
                string bg = Path.Combine(AppSetting.UserDataFolder, "bg");
                Directory.CreateDirectory(bg);
                await Parallel.ForEachAsync(infos, async (info, _) =>
                {
                    if (string.IsNullOrWhiteSpace(info.Display.Background?.Url))
                    {
                        return;
                    }
                    string url = info.Display.Background.Url;
                    try
                    {
                        string name = Path.GetFileName(url);
                        string path = Path.Combine(bg, name);
                        if (!File.Exists(path))
                        {
                            byte[] bytes = await _httpClient.GetByteArrayAsync(url);
                            await File.WriteAllBytesAsync(path, bytes);
                        }
                        AppSetting.SetVersionPoster(info.GameBiz, name);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Download image: {url}", url);
                    }
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, nameof(DownloadGameVersionPosterAsync));
        }
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
        if (!_gameChannelSDK.TryGetValue(gameId, out GameChannelSDK? sdk))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameChannelSDKAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _gameChannelSDK[item.GameId] = item;
            }
            sdk = list.FirstOrDefault(x => x.GameId == gameId);
        }
        return sdk;
    }


}
