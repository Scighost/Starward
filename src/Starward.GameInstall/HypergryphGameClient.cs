using Starward.Core;
using Starward.Core.Hypergryph;
using Starward.Core.HoYoPlay;
using Starward.Core.Passport;
using System.Text.Json;
using ZiggyCreatures.Caching.Fusion;

namespace Starward.GameInstall;

internal sealed class HypergryphGameClient(HttpClient http, IFusionCache cache, IEnumerable<string> cacheTags) : IHoYoPlayClient
{
    private readonly HypergryphLauncherClient _client = new(http);
    public LauncherConfig LauncherConfig { get; set; }
    public string Language { get; set; }
    public PassportUserInfo? PassportUserInfo { get; set; }

    private GameId[] Games => LauncherConfig.Id switch
    {
        HypergryphGameConstants.LauncherAppCode =>
            [GameId.FromGameBiz(GameBiz.arknights_cn)!, GameId.FromGameBiz(GameBiz.endfield_cn)!],
        HypergryphGameConstants.GlobalLauncherAppCode => [GameId.FromGameBiz(GameBiz.endfield_global)!],
        _ when HypergryphGameConstants.GetEndfieldChannelGameId(LauncherConfig.Id) is GameId gameId => [gameId],
        _ => [],
    };

    public async Task<List<GameInfo>> GetGameInfoAsync(CancellationToken cancellationToken = default)
    {
        return (await Task.WhenAll(Games.Select(gameId => GetGameInfoAsync(gameId, cancellationToken)))).ToList();
    }

    public async Task<GameInfo> GetGameInfoAsync(GameId gameId, CancellationToken cancellationToken)
    {
        return HypergryphGameMetadata.CreateGameInfo(gameId, await GetArtworkAsync(gameId, cancellationToken));
    }

    public GameInfo GetCachedGameInfo(GameId gameId)
    {
        return HypergryphGameMetadata.CreateGameInfo(gameId,
            cache.GetOrDefault<HypergryphLauncherContent>(GetArtworkCacheKey(gameId)));
    }

    private string GetArtworkCacheKey(GameId gameId) => $"HypergryphArtwork_{gameId.Id}_{Language}";

    private async Task<HypergryphLauncherContent?> GetArtworkAsync(GameId gameId, CancellationToken cancellationToken)
    {
        try
        {
            return await cache.GetOrSetAsync(GetArtworkCacheKey(gameId), async token =>
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
                timeout.CancelAfter(TimeSpan.FromSeconds(5));
                return await _client.GetGameArtworkAsync(gameId, Language, timeout.Token);
            }, tags: cacheTags, token: cancellationToken);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidDataException or JsonException)
        {
            return null;
        }
    }
    public async Task<List<GameBackgroundInfo>> GetGameBackgroundAsync(CancellationToken cancellationToken = default)
    {
        return (await Task.WhenAll(Games.Select(async gameId =>
            HypergryphGameMetadata.CreateBackgroundInfo(gameId, await GetArtworkAsync(gameId, cancellationToken))))).ToList();
    }

    public async Task<GameContent> GetGameContentAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return HypergryphGameMetadata.CreateGameContent(gameId, await _client.GetGameContentAsync(gameId, Language, cancellationToken));
    }

    public async Task<List<GamePackage>> GetGamePackageAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        var packages = new List<GamePackage>();
        foreach (GameId gameId in Games.Where(id => gameIds is null || gameIds.Contains(id)))
        {
            var latest = await _client.GetLatestGameAsync(gameId, null, cancellationToken);
            packages.Add(HypergryphGameMetadata.CreateGamePackage(gameId, latest));
        }
        return packages;
    }

    public Task<List<GameChannelSDK>> GetGameChannelSDKAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GameChannelSDK>>([]);
    }

    public Task<List<GameDeprecatedFileConfig>> GetGameDeprecatedFileConfigAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GameDeprecatedFileConfig>>([]);
    }

    public Task<List<GameConfig>> GetGameConfigAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GameConfig>>(Games.Select(HypergryphGameMetadata.CreateGameConfig).ToList());
    }

    public Task<List<GameScanInfo>> GetGameScanInfosAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GameScanInfo>>([]);
    }

    public Task<List<GameBranch>> GetGameBranchAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GameBranch>>([]);
    }

    public Task<GameSophonChunkBuild> GetGameSophonChunkBuildAsync(GameBranchPackage gameBranchPackage, string? version = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<GameSophonPatchBuild> GetGameSophonPatchBuildAsync(GameBranchPackage gameBranchPackage, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();
    }

    public Task<List<WPFPackageInfo>> GetWPFPackagesAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<WPFPackageInfo>>([]);
    }

    public Task<List<GameDXConfig>> GetDXConfigsAsync(IEnumerable<GPUInfo> gpuInfos, IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GameDXConfig>>([]);
    }

    public Task<List<GamePluginRelease>> GetGamePluginsAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GamePluginRelease>>([]);
    }

    public Task<GameReservationContent> GetGameReservationContentAsync(GameId gameId, CancellationToken cancellation = default)
    {
        throw new NotSupportedException();
    }

}
