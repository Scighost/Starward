using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using System.Collections.Concurrent;
using System.Globalization;
using System.Security.Cryptography;
using ZiggyCreatures.Caching.Fusion;

namespace Starward.GameManagement;


public class HoYoPlayService
{

    private readonly ILogger<HoYoPlayService> _logger;

    private readonly IServiceProvider _serviceProvider;

    private readonly IFusionCache _cache;

    private readonly ConcurrentDictionary<GameId, HoYoPlayClient> _clients;

    private readonly ConcurrentDictionary<string, HoYoPlayClient> _launchers;


    public HoYoPlayService(ILogger<HoYoPlayService> logger, IServiceProvider serviceProvider, IFusionCache cache)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _cache = cache;
        _clients = new();
        _launchers = new();
    }



    public void ClearCache()
    {
        _cache?.Clear();
    }


    private HoYoPlayClient GetClient(GameId gameId)
    {
        if (_clients.TryGetValue(gameId, out HoYoPlayClient? client))
        {
            return client;
        }
        throw new NotSupportedException($"GameId {gameId.Id} ({gameId.GameBiz}) is not supported.");
    }


    public async Task InitializeClientsAsync(IEnumerable<LauncherConfig> launcherConfigs, CancellationToken cancellation = default)
    {
        await Parallel.ForEachAsync(launcherConfigs, cancellation, async (config, token) =>
        {
            HoYoPlayClient client = new HoYoPlayClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = config, Language = CultureInfo.CurrentUICulture.Name };
            _launchers[config.Id] = client;
            List<GameInfo> games = await client.GetGameInfoAsync(token);
            _cache.Set($"{nameof(GameInfo)}_{config.Id}", games, token: token);
            foreach (GameInfo game in games)
            {
                _clients[new GameId { Id = game.Id, GameBiz = game.GameBiz }] = client;
            }
        });
    }


    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    public async Task<List<GameInfo>> GetGameInfosAsync(LauncherConfig launcherConfig, CancellationToken cancellation = default)
    {
        if (!_launchers.TryGetValue(launcherConfig.Id, out HoYoPlayClient? client))
        {
            client = new HoYoPlayClient(_serviceProvider.GetRequiredService<HttpClient>()) { LauncherConfig = launcherConfig, Language = CultureInfo.CurrentUICulture.Name };
            _launchers[launcherConfig.Id] = client;
        }
        return await _cache.GetOrSetAsync($"{nameof(GameInfo)}_{launcherConfig.Id}", async token =>
        {
            return await client.GetGameInfoAsync(cancellation);
        }, token: cancellation);
    }


    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    public async Task<GameInfo?> GetGameInfoAsync(GameId gameId, CancellationToken cancellation = default)
    {
        HoYoPlayClient client = GetClient(gameId);
        List<GameInfo> games = await _cache.GetOrSetAsync($"{nameof(GameInfo)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameInfoAsync(token);
        }, token: cancellation);
        return games?.FirstOrDefault(x => x.Id == gameId.Id);
    }


    /// <summary>
    /// 版本背景图和版本亮点
    /// </summary>
    public async Task<GameBackgroundInfo?> GetGameBackgroundInfoAsync(GameId gameId, CancellationToken cancellation = default)
    {
        HoYoPlayClient client = GetClient(gameId);
        var infos = await _cache.GetOrSetAsync($"{nameof(GameBackgroundInfo)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameBackgroundAsync(token);
        }, token: cancellation);
        return infos?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 轮播图、资讯、媒体标签
    /// </summary>
    public async Task<GameContent?> GetGameContentAsync(GameId gameId, CancellationToken cancellation = default)
    {
        HoYoPlayClient client = GetClient(gameId);
        return await _cache.GetOrSetAsync($"{nameof(GameContent)}_{gameId.Id}", async token =>
        {
            return await client.GetGameContentAsync(gameId, token);
        }, token: cancellation);
    }


    /// <summary>
    /// 游戏安装包
    /// </summary>
    public async Task<GamePackage?> GetGamePackageAsync(GameId gameId, CancellationToken cancellation = default)
    {
        HoYoPlayClient client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GamePackage)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGamePackageAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    public async Task<GameChannelSDK?> GetGameChannelSDKAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameChannelSDK)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameChannelSDKAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 需要删除的文件
    /// </summary>
    public async Task<GameDeprecatedFileConfig?> GetGameDeprecatedFileConfigAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameDeprecatedFileConfig)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameDeprecatedFileConfigAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 游戏配置
    /// </summary>
    public async Task<GameConfig?> GetGameConfigAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameConfig)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameConfigAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 获取游戏扫描信息，不同版本exe的md5
    /// </summary>
    public async Task<GameScanInfo?> GetGameScanInfoAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameScanInfo)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameScanInfosAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId.Id);
    }


    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    public async Task<GameBranch?> GetGameBranchAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameBranch)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGameBranchAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// WPF Package
    /// </summary>
    public async Task<WPFPackageInfo?> GetWPFPackageAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(WPFPackageInfo)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetWPFPackagesAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 游戏插件
    /// </summary>
    public async Task<GamePluginRelease?> GetGamePluginAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GamePluginRelease)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetGamePluginsAsync(null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 获取 DirectX 配置
    /// </summary>
    public async Task<GameDXConfig?> GetDXConfigAsync(GameId gameId, IEnumerable<GPUInfo> gpuInfos, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        var list = await _cache.GetOrSetAsync($"{nameof(GameDXConfig)}_{client.LauncherConfig.Id}", async token =>
        {
            return await client.GetDXConfigsAsync(gpuInfos, null, token);
        }, token: cancellation);
        return list?.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 游戏预约页面
    /// </summary>
    public async Task<GameReservationContent?> GetGameReservationContentAsync(GameId gameId, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        return await _cache.GetOrSetAsync($"{nameof(GameReservationContent)}_{gameId.Id}", async token =>
        {
            return await client.GetGameReservationContentAsync(gameId, token);
        }, token: cancellation);
    }


    /// <summary>
    /// Chunk 下载模式文件清单
    /// </summary>
    public async Task<GameSophonChunkBuild?> GetGameSophonChunkBuildAsync(GameId gameId, GameBranchPackage gameBranchPackage, string? version = null, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        return await _cache.GetOrSetAsync($"{nameof(GameSophonChunkBuild)}_{gameBranchPackage.PackageId}_{version}", async token =>
        {
            return await client.GetGameSophonChunkBuildAsync(gameBranchPackage, version, token);
        }, token: cancellation);
    }


    /// <summary>
    /// Chunk 下载模式的增量更新补丁文件清单
    /// </summary>
    public async Task<GameSophonPatchBuild?> GetGameSophonPatchBuildAsync(GameId gameId, GameBranchPackage gameBranchPackage, CancellationToken cancellation = default)
    {
        var client = GetClient(gameId);
        return await _cache.GetOrSetAsync($"{nameof(GameSophonPatchBuild)}_{gameBranchPackage.PackageId}", async token =>
        {
            return await client.GetGameSophonPatchBuildAsync(gameBranchPackage, token);
        }, token: cancellation);
    }




    public async Task<SophonChunkManifest> DownloadAndParseChunkManifestAsync(GameSophonManifestUrl manifestUrl, GameSophonManifestFile manifestFile, CancellationToken cancellation = default)
    {
        using var stream = await DownloadManifestWithCacheAsync(manifestUrl, manifestFile, cancellation);
        return SophonChunkManifest.Parser.ParseFrom(stream);
    }


    public async Task<SophonPatchManifest> DownloadAndParsePatchManifestAsync(GameSophonManifestUrl manifestUrl, GameSophonManifestFile manifestFile, CancellationToken cancellation = default)
    {
        using var stream = await DownloadManifestWithCacheAsync(manifestUrl, manifestFile, cancellation);
        return SophonPatchManifest.Parser.ParseFrom(stream);
    }


    private async Task<Stream> DownloadManifestWithCacheAsync(GameSophonManifestUrl manifestUrl, GameSophonManifestFile manifestFile, CancellationToken cancellationToken = default)
    {
        string cachePath = GetManifestCachePath(manifestFile.Id);

        Stream? stream = await GetCachedManifestStreamAsync(cachePath, manifestFile.UncompressedSize, manifestFile.Checksum, cancellationToken);
        if (stream is not null)
        {
            return stream;
        }

        string url = manifestUrl.BuildUrl(manifestFile.Id);

        using var response = await _serviceProvider.GetRequiredService<HttpClient>().GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        using var hs = await response.Content.ReadAsStreamAsync(cancellationToken);

        string? folder = Path.GetDirectoryName(cachePath);
        if (folder is not null)
        {
            Directory.CreateDirectory(folder);
        }
        using var fs = File.Create(cachePath);
        if (manifestUrl.Compression is 0)
        {
            await hs.CopyToAsync(fs, cancellationToken);
        }
        else
        {
            using var decompressedStream = new ZstdSharp.DecompressionStream(hs);
            await decompressedStream.CopyToAsync(fs, cancellationToken);
        }
        fs.Position = 0;
        string hash = Convert.ToHexStringLower(await MD5.HashDataAsync(fs, cancellationToken));
        if (!string.Equals(hash, manifestFile.Checksum, StringComparison.OrdinalIgnoreCase))
        {
            fs.Dispose();
            File.Delete(cachePath);
            throw new InvalidDataException($"Checksum mismatch for manifest {manifestFile.Id}: expected {manifestFile.Checksum}, got {hash}");
        }
        fs.Dispose();
        return File.OpenRead(cachePath);
    }


    private static async Task<Stream?> GetCachedManifestStreamAsync(string cachePath, long size, string checksum, CancellationToken cancellationToken = default)
    {
        if (File.Exists(cachePath))
        {
            using var fs = File.OpenRead(cachePath);
            if (fs.Length == size)
            {
                string hash = Convert.ToHexStringLower(await MD5.HashDataAsync(fs, cancellationToken));
                if (string.Equals(hash, checksum, StringComparison.OrdinalIgnoreCase))
                {
                    return File.OpenRead(cachePath);
                }
            }
        }
        return null;
    }


    private string GetManifestCachePath(string manifestId)
    {
        return Path.Combine(GameInstallEnvironment.CacheFolder, "game", manifestId);
    }





}

