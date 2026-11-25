using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Passport;
using Starward.GameInstall.Service;
using Starward.GameInstall.Sophon;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using ZiggyCreatures.Caching.Fusion;

namespace Starward.GameInstall;


public class HoYoPlayService
{

    private readonly ILogger<HoYoPlayService> _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly IFusionCache _cache;

    private readonly IHttpClientFactory _httpClientFactory;

    private readonly ConcurrentDictionary<GameId, IHoYoPlayClient> _clients;

    private readonly ReadOnlyCollection<string> _tags = [nameof(HoYoPlayService)];

    private readonly FusionCacheEntryOptions _skipL2CacheOptions;


    public HoYoPlayService(ILogger<HoYoPlayService> logger, IServiceProvider serviceProvider, IFusionCache cache, IHttpClientFactory httpClientFactory)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _httpClientFactory = httpClientFactory;
        _clients = new();
        RestoreClients();
        _skipL2CacheOptions = _cache.CreateEntryOptions().SetSkipDistributedCache(true, true);
    }



    public void RestoreClients()
    {
        try
        {
            var store = _cache.GetOrDefault<List<HoYoPlayClientStore>>(nameof(HoYoPlayClientStore));
            if (store is not null)
            {
                foreach (var item in store)
                {
                    IHoYoPlayClient client = item.LauncherConfig.IsBeta
                        ? new HoYoPlayBetaClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = item.LauncherConfig, Language = CultureInfo.CurrentUICulture.Name, PassportUserInfo = this.PassportUserInfo }
                        : new HoYoPlayClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = item.LauncherConfig, Language = CultureInfo.CurrentUICulture.Name };
                    foreach (var gameId in item.GameIds)
                    {
                        _clients[gameId] = client;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HoYoPlayService restore clients.");
        }
    }


    private Lock _storeLock = new();

    private CancellationTokenSource _storeTokenSource;

    public async void StoreClients()
    {
        try
        {
            _storeTokenSource?.Cancel();
            _storeTokenSource = new();
            await Task.Delay(3000, _storeTokenSource.Token).ConfigureAwait(false);
            lock (_storeLock)
            {
                List<(GameId GameId, LauncherConfig Config)> list = _clients.Select(x => (x.Key, x.Value.LauncherConfig)).ToList();
                var store = list.GroupBy(x => x.Config.Id).Select(x => new HoYoPlayClientStore
                {
                    LauncherConfig = x.First().Config,
                    GameIds = x.Select(y => y.GameId).ToList(),
                });
                _cache.Set(nameof(HoYoPlayClientStore), store, _cache.CreateEntryOptions(duration: TimeSpan.MaxValue));
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HoYoPlayService store clients.");
        }
    }


    public void ResetUILanguage()
    {
        string lang = CultureInfo.CurrentUICulture.Name;
        foreach (IHoYoPlayClient item in _clients.Values)
        {
            item.Language = lang;
        }
        _cache.RemoveByTag(nameof(HoYoPlayService));
    }


    public LauncherConfig? GetLauncherConfig(GameId gameId)
    {
        return _clients.GetValueOrDefault(gameId)?.LauncherConfig;
    }



    public PassportUserInfo? PassportUserInfo
    {
        get; set
        {
            field = value;
            foreach (IHoYoPlayClient item in _clients.Values)
            {
                if (item is HoYoPlayBetaClient client)
                {
                    client.PassportUserInfo = value;
                }
            }
        }
    }


    private IHoYoPlayClient GetClient(GameId gameId)
    {
        if (_clients.TryGetValue(gameId, out IHoYoPlayClient? client))
        {
            return client;
        }
        throw new NotSupportedException($"GameId {gameId.Id} ({gameId.GameBiz}) is not supported.");
    }




    public async Task UpdateLauncherClientMapAsync(IEnumerable<LauncherConfig> launcherConfigs, CancellationToken cancellation = default)
    {
        List<(GameId GameId, IHoYoPlayClient Client)> list = new();
        await Parallel.ForEachAsync(launcherConfigs, cancellation, async (config, token) =>
        {
            IHoYoPlayClient client = config.IsBeta
                ? new HoYoPlayBetaClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = config, Language = CultureInfo.CurrentUICulture.Name, PassportUserInfo = this.PassportUserInfo }
                : new HoYoPlayClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = config, Language = CultureInfo.CurrentUICulture.Name };
            List<GameInfo> games = await client.GetGameInfoAsync(cancellation).ConfigureAwait(false);
            await _cache.SetAsync($"{nameof(GameInfo)}_{config.Id}", games, _cache.CreateEntryOptions().SetFailSafe(true, TimeSpan.MaxValue), tags: _tags, token: cancellation).ConfigureAwait(false);
            lock (list)
            {
                foreach (GameInfo game in games)
                {
                    if (game.GameServerConfigs?.Count > 0)
                    {
                        foreach (GameServerConfig server in game.GameServerConfigs)
                        {
                            list.Add((new GameId { Id = server.GameId, GameBiz = game.GameBiz }, client));
                        }
                    }
                    else
                    {
                        list.Add((new GameId { Id = game.Id, GameBiz = game.GameBiz }, client));
                    }
                }
            }
        }).ConfigureAwait(false);
        _clients.Clear();
        foreach (var item in list)
        {
            _clients[item.GameId] = item.Client;
        }
        StoreClients();
    }


    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    public async Task<List<GameInfo>> GetGameInfosAsync(LauncherConfig launcherConfig, CancellationToken cancellation = default)
    {
        IHoYoPlayClient? client = _clients.Values.FirstOrDefault(x => x.LauncherConfig.Id == launcherConfig.Id);
        if (client is null || client.LauncherConfig != launcherConfig)
        {
            client = launcherConfig.IsBeta
                ? new HoYoPlayBetaClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = launcherConfig, Language = CultureInfo.CurrentUICulture.Name, PassportUserInfo = this.PassportUserInfo }
                : new HoYoPlayClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = launcherConfig, Language = CultureInfo.CurrentUICulture.Name };
            await _cache.ExpireAsync($"{nameof(GameInfo)}_{launcherConfig.Id}", null, cancellation);
        }
        List<GameInfo> infos = await _cache.GetOrSetAsync($"{nameof(GameInfo)}_{launcherConfig.Id}", async token =>
        {
            List<GameInfo> games = await client.GetGameInfoAsync(cancellation).ConfigureAwait(false);
            foreach (GameInfo game in games)
            {
                if (game.GameServerConfigs?.Count > 0)
                {
                    foreach (GameServerConfig server in game.GameServerConfigs)
                    {
                        _clients[new GameId { Id = server.GameId, GameBiz = game.GameBiz }] = client;
                    }
                }
                else
                {
                    _clients[new GameId { Id = game.Id, GameBiz = game.GameBiz }] = client;
                }
            }
            StoreClients();
            return games;
        }, _cache.CreateEntryOptions().SetFailSafe(true, TimeSpan.MaxValue), tags: _tags, token: cancellation).ConfigureAwait(false);
        return infos;
    }


    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    public async Task<GameInfo?> GetGameInfoAsync(GameId gameId, CancellationToken cancellation = default)
    {
        if (gameId.GameBiz == GameBiz.bh3_global)
        {
            gameId = new GameId(GameId.bh3_glb, GameBiz.bh3_global);
        }
        IHoYoPlayClient client = GetClient(gameId);
        List<GameInfo> games = await _cache.GetOrSetAsync($"{nameof(GameInfo)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameInfoAsync(token).ConfigureAwait(false);
        }, _cache.CreateEntryOptions().SetFailSafe(true, TimeSpan.MaxValue), tags: _tags, token: cancellation).ConfigureAwait(false);
        return games?.FirstOrDefault(x => x.Id == gameId.Id);
    }


    public GameInfo? GetCachedGameInfo(GameId gameId)
    {
        try
        {
            if (gameId.GameBiz == GameBiz.bh3_global)
            {
                gameId = new GameId(GameId.bh3_glb, GameBiz.bh3_global);
            }
            IHoYoPlayClient client = GetClient(gameId);
            return _cache.GetOrDefault<List<GameInfo>>($"{nameof(GameInfo)}_{client.LauncherConfig.Id}")?.FirstOrDefault(x => x.Id == gameId.Id);
        }
        catch { }
        return null;
    }


    /// <summary>
    /// 版本背景图和版本亮点
    /// </summary>
    public async Task<GameBackgroundInfo?> GetGameBackgroundInfoAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        List<GameBackgroundInfo> infos = await _cache.GetOrSetAsync($"{nameof(GameBackgroundInfo)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameBackgroundAsync(token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return infos?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 轮播图、资讯、媒体标签
    /// </summary>
    public async Task<GameContent?> GetGameContentAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        return await _cache.GetOrSetAsync($"{nameof(GameContent)}_{gameId.Id}", async token =>
        {
            return await client.GetGameContentAsync(gameId, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
    }


    /// <summary>
    /// 游戏安装包
    /// </summary>
    public async Task<GamePackage?> GetGamePackageAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        List<GamePackage> list;
        if (client.LauncherConfig.IsBeta)
        {
            list = await _cache.GetOrSetAsync($"{nameof(GamePackage)}_{client.LauncherConfig.Id}_{client.PassportUserInfo?.Aid}", async token =>
            {
                return await client.GetGamePackageAsync(null, token).ConfigureAwait(false);
            }, _skipL2CacheOptions, tags: _tags, token: cancellation).ConfigureAwait(false);
        }
        else
        {
            list = await _cache.GetOrSetAsync($"{nameof(GamePackage)}_{client.LauncherConfig.Id}", async token =>
            {
                return await client.GetGamePackageAsync(null, token).ConfigureAwait(false);
            }, tags: _tags, token: cancellation).ConfigureAwait(false);
        }
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    public async Task<GameChannelSDK?> GetGameChannelSDKAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameChannelSDK)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameChannelSDKAsync(null, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 需要删除的文件
    /// </summary>
    public async Task<GameDeprecatedFileConfig?> GetGameDeprecatedFileConfigAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameDeprecatedFileConfig)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameDeprecatedFileConfigAsync(null, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 游戏配置
    /// </summary>
    public async Task<GameConfig?> GetGameConfigAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameConfig)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameConfigAsync(null, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 获取游戏扫描信息，不同版本exe的md5
    /// </summary>
    public async Task<GameScanInfo?> GetGameScanInfoAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameScanInfo)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameScanInfosAsync(null, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return list?.FirstOrDefault(x => x.GameId == gameId.Id);
    }


    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    public async Task<GameBranch?> GetGameBranchAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameBranch)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameBranchAsync(null, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// WPF Package
    /// </summary>
    public async Task<WPFPackageInfo?> GetWPFPackageAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        List<WPFPackageInfo> list;
        if (client.LauncherConfig.IsBeta)
        {
            list = await _cache.GetOrSetAsync($"{nameof(WPFPackageInfo)}_{client.LauncherConfig.Id}_{client.PassportUserInfo?.Aid}", async token =>
            {
                return await client.GetWPFPackagesAsync(null, token).ConfigureAwait(false);
            }, _skipL2CacheOptions, tags: _tags, token: cancellation).ConfigureAwait(false);
        }
        else
        {
            list = await _cache.GetOrSetAsync($"{nameof(WPFPackageInfo)}_{client.LauncherConfig.Id}", async token =>
            {
                return await client.GetWPFPackagesAsync(null, token).ConfigureAwait(false);
            }, tags: _tags, token: cancellation).ConfigureAwait(false);
        }
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 游戏插件
    /// </summary>
    public async Task<GamePluginRelease?> GetGamePluginAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GamePluginRelease)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGamePluginsAsync(null, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 获取 DirectX 配置
    /// </summary>
    public async Task<GameDXConfig?> GetDXConfigAsync(GameId gameId, IEnumerable<GPUInfo> gpuInfos, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        string key = $"{nameof(GameDXConfig)}_{client.LauncherConfig.Id}_{string.Join("", gpuInfos.Select(x => $"{x.Name}{x.DriverVersion}"))}";
        var list = await _cache.GetOrSetAsync(key, async token =>
        {
            return await client.GetDXConfigsAsync(gpuInfos, null, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 游戏预约页面
    /// </summary>
    public async Task<GameReservationContent?> GetGameReservationContentAsync(GameId gameId, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        return await _cache.GetOrSetAsync($"{nameof(GameReservationContent)}_{gameId.Id}", async token =>
        {
            return await client.GetGameReservationContentAsync(gameId, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
    }


    /// <summary>
    /// Chunk 下载模式文件清单
    /// </summary>
    public async Task<GameSophonChunkBuild?> GetGameSophonChunkBuildAsync(GameId gameId, GameBranchPackage gameBranchPackage, string? version = null, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        if (client.LauncherConfig.IsBeta)
        {
            return await _cache.GetOrSetAsync($"{nameof(GameSophonChunkBuild)}_{gameBranchPackage.PackageId}_{gameBranchPackage.Branch}_{version}_{client.PassportUserInfo?.Aid}", async token =>
            {
                return await client.GetGameSophonChunkBuildAsync(gameBranchPackage, version, token).ConfigureAwait(false);
            }, _skipL2CacheOptions, tags: _tags, token: cancellation).ConfigureAwait(false);
        }
        else
        {
            return await _cache.GetOrSetAsync($"{nameof(GameSophonChunkBuild)}_{gameBranchPackage.PackageId}_{gameBranchPackage.Branch}_{version}", async token =>
            {
                return await client.GetGameSophonChunkBuildAsync(gameBranchPackage, version, token).ConfigureAwait(false);
            }, tags: _tags, token: cancellation).ConfigureAwait(false);
        }
    }


    /// <summary>
    /// Chunk 下载模式的增量更新补丁文件清单
    /// </summary>
    public async Task<GameSophonPatchBuild?> GetGameSophonPatchBuildAsync(GameId gameId, GameBranchPackage gameBranchPackage, CancellationToken cancellation = default)
    {
        IHoYoPlayClient client = GetClient(gameId);
        return await _cache.GetOrSetAsync($"{nameof(GameSophonPatchBuild)}_{gameBranchPackage.PackageId}_{gameBranchPackage.Branch}", async token =>
        {
            return await client.GetGameSophonPatchBuildAsync(gameBranchPackage, token).ConfigureAwait(false);
        }, tags: _tags, token: cancellation).ConfigureAwait(false);
    }




    public async Task<SophonChunkManifest> DownloadAndParseChunkManifestAsync(GameSophonManifestUrl manifestUrl, GameSophonManifestFile manifestFile, CancellationToken cancellation = default)
    {
        using var stream = await DownloadManifestWithCacheAsync(manifestUrl, manifestFile, cancellation).ConfigureAwait(false);
        return SophonChunkManifest.Parser.ParseFrom(stream);
    }


    public async Task<SophonPatchManifest> DownloadAndParsePatchManifestAsync(GameSophonManifestUrl manifestUrl, GameSophonManifestFile manifestFile, CancellationToken cancellation = default)
    {
        using var stream = await DownloadManifestWithCacheAsync(manifestUrl, manifestFile, cancellation).ConfigureAwait(false);
        return SophonPatchManifest.Parser.ParseFrom(stream);
    }


    private async Task<Stream> DownloadManifestWithCacheAsync(GameSophonManifestUrl manifestUrl, GameSophonManifestFile manifestFile, CancellationToken cancellationToken = default)
    {
        string cachePath = GetManifestCachePath(manifestFile.Id);

        if (await HashService.CheckFileMD5Async(cachePath, manifestFile.UncompressedSize, manifestFile.Checksum, cancellationToken).ConfigureAwait(false))
        {
            return File.OpenRead(cachePath);
        }

        string url = manifestUrl.BuildUrl(manifestFile.Id);

        using var client = _httpClientFactory.CreateClient();
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

        string? folder = Path.GetDirectoryName(cachePath);
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
        using var fs = File.Create(cachePath);
        if (manifestUrl.Compression is 0)
        {
            await hs.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            using var decompressedStream = new ZstandardStream(hs, CompressionMode.Decompress);
            await decompressedStream.CopyToAsync(fs, cancellationToken).ConfigureAwait(false);
        }
        fs.Position = 0;
        string hash = Convert.ToHexStringLower(await MD5.HashDataAsync(fs, cancellationToken).ConfigureAwait(false));
        if (!string.Equals(hash, manifestFile.Checksum, StringComparison.OrdinalIgnoreCase))
        {
            fs.Dispose();
            File.Delete(cachePath);
            throw new InvalidDataException($"Checksum mismatch for manifest {manifestFile.Id}: expected {manifestFile.Checksum}, got {hash}");
        }
        fs.Dispose();
        return File.OpenRead(cachePath);
    }



    private static string GetManifestCachePath(string manifestId)
    {
        return Path.Combine(GameInstallEnv.CacheFolder, "game", manifestId);
    }



    public class HoYoPlayClientStore
    {
        public LauncherConfig LauncherConfig { get; set; }

        public List<GameId> GameIds { get; set; }
    }


}
