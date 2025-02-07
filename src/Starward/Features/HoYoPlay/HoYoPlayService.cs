using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Frameworks;
using System;
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

    private readonly IMemoryCache _memoryCache;


    public HoYoPlayService(ILogger<HoYoPlayService> logger, HoYoPlayClient client, HttpClient httpClient, IMemoryCache memoryCache)
    {
        _logger = logger;
        _client = client;
        _httpClient = httpClient;
        _memoryCache = memoryCache;
    }





    public async Task<GameInfo> GetGameInfoAsync(GameId gameId)
    {
        if (!_memoryCache.TryGetValue($"{nameof(GameInfo)}_{gameId.Id}", out GameInfo? info))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameInfoAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _memoryCache.Set($"{nameof(GameInfo)}_{item.Id}", item, TimeSpan.FromMinutes(10));
            }
            info = list.FirstOrDefault(x => x == gameId);
        }
        return info!;
    }



    public async Task<List<GameInfo>> UpdateGameInfoListAsync(CancellationToken cancellationToken = default)
    {
        if (_memoryCache is MemoryCache cache)
        {
            cache.Clear();
        }
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
        foreach (var item in infos)
        {
            _memoryCache.Set($"{nameof(GameInfo)}_{item.Id}", item, TimeSpan.FromMinutes(10));
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
        if (!_memoryCache.TryGetValue($"{nameof(GameBackgroundInfo)}_{gameId.Id}", out GameBackgroundInfo? background))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameBackgroundAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _memoryCache.Set($"{nameof(GameBackgroundInfo)}_{item.GameId.Id}", item, TimeSpan.FromMinutes(1));
            }
            background = list.FirstOrDefault(x => x.GameId == gameId);
        }
        return background!;
    }



    public async Task<GameContent> GetGameContentAsync(GameId gameId)
    {
        if (!_memoryCache.TryGetValue($"{nameof(GameContent)}_{gameId.Id}", out GameContent? content))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            content = await _client.GetGameContentAsync(LauncherId.FromGameId(gameId)!, lang, gameId);
            _memoryCache.Set($"{nameof(GameContent)}_{content.GameId.Id}", content, TimeSpan.FromMinutes(1));
        }
        return content!;
    }



    public async Task<GamePackage> GetGamePackageAsync(GameId gameId)
    {
        if (!_memoryCache.TryGetValue($"{nameof(GamePackage)}_{gameId.Id}", out GamePackage? package))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGamePackageAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _memoryCache.Set($"{nameof(GamePackage)}_{item.GameId.Id}", item, TimeSpan.FromMinutes(1));
            }
            package = list.FirstOrDefault(x => x.GameId == gameId);
        }
        return package!;
    }



    public async Task<GameConfig?> GetGameConfigAsync(GameId gameId)
    {
        if (!_memoryCache.TryGetValue($"{nameof(GameConfig)}_{gameId.Id}", out GameConfig? config))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameConfigAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _memoryCache.Set($"{nameof(GameConfig)}_{item.GameId.Id}", item, TimeSpan.FromMinutes(1));
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
        if (!_memoryCache.TryGetValue($"{nameof(GameChannelSDK)}_{gameId.Id}", out GameChannelSDK? sdk))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameChannelSDKAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _memoryCache.Set($"{nameof(GameChannelSDK)}_{item.GameId.Id}", item, TimeSpan.FromMinutes(1));
            }
            sdk = list.FirstOrDefault(x => x.GameId == gameId);
        }
        return sdk;
    }



    public async Task<GameBranch?> GetGameBranchAsync(GameId gameId)
    {
        if (!_memoryCache.TryGetValue($"{nameof(GameBranch)}_{gameId.Id}", out GameBranch? branch))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            var list = await _client.GetGameBranchAsync(LauncherId.FromGameId(gameId)!, lang);
            foreach (var item in list)
            {
                _memoryCache.Set($"{nameof(GameBranch)}_{item.GameId.Id}", item, TimeSpan.FromMinutes(1));
            }
            branch = list.FirstOrDefault(x => x.GameId == gameId);
        }
        return branch;
    }




    public async Task<GameSophonChunkBuild?> GetGameSophonChunkBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage)
    {
        if (!_memoryCache.TryGetValue($"{nameof(GameSophonChunkBuild)}_{gameBranchPackage.PackageId}", out GameSophonChunkBuild? build))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            build = await _client.GetGameSophonChunkBuildAsync(gameBranch, gameBranchPackage, gameBranchPackage.Tag);
            _memoryCache.Set($"{nameof(GameSophonChunkBuild)}_{gameBranchPackage.PackageId}", build, TimeSpan.FromMinutes(1));
        }
        return build;
    }




    public async Task<GameSophonPatchBuild?> GetGameSophonPatchBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage)
    {
        if (!_memoryCache.TryGetValue($"{nameof(GameSophonPatchBuild)}_{gameBranchPackage.PackageId}", out GameSophonPatchBuild? build))
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            build = await _client.GetGameSophonPatchBuildAsync(gameBranch, gameBranchPackage);
            _memoryCache.Set($"{nameof(GameSophonPatchBuild)}_{gameBranchPackage.PackageId}", build, TimeSpan.FromMinutes(1));
        }
        return build;
    }




}
