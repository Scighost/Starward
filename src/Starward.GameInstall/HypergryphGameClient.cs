using Starward.Core;
using Starward.Core.Hypergryph;
using Starward.Core.HoYoPlay;
using Starward.Core.Passport;

namespace Starward.GameInstall;

internal sealed class HypergryphGameClient(HttpClient http) : IHoYoPlayClient
{
    private readonly HypergryphLauncherClient _client = new(http);
    public LauncherConfig LauncherConfig { get; set; }
    public string Language { get; set; }
    public PassportUserInfo? PassportUserInfo { get; set; }

    private GameBiz[] Games => LauncherConfig.Id == HypergryphGameConstants.LauncherAppCode
        ? [GameBiz.arknights_cn, GameBiz.endfield_cn] : [GameBiz.endfield_global];

    public Task<List<GameInfo>> GetGameInfoAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<List<GameInfo>>(Games.Select(HypergryphGameMetadata.CreateGameInfo).ToList());
    }

    public async Task<List<GameBackgroundInfo>> GetGameBackgroundAsync(CancellationToken cancellationToken = default)
    {
        return (await Task.WhenAll(Games.Select(async biz => HypergryphGameMetadata.CreateBackgroundInfo(biz, await _client.GetGameContentAsync(biz, Language, cancellationToken))))).ToList();
    }

    public async Task<GameContent> GetGameContentAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        return HypergryphGameMetadata.CreateGameContent(gameId.GameBiz, await _client.GetGameContentAsync(gameId.GameBiz, Language, cancellationToken));
    }

    public async Task<List<GamePackage>> GetGamePackageAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        var packages = new List<GamePackage>();
        foreach (GameBiz biz in Games.Where(biz => gameIds is null || gameIds.Any(id => id.GameBiz == biz)))
        {
            var latest = await _client.GetLatestGameAsync(biz, null, cancellationToken);
            packages.Add(HypergryphGameMetadata.CreateGamePackage(biz, latest));
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
