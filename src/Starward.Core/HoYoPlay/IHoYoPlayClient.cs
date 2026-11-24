using Starward.Core.Passport;

namespace Starward.Core.HoYoPlay;

/// <summary>
/// HoYoPlay 启动器 API 客户端。
/// </summary>
public interface IHoYoPlayClient
{

    public LauncherConfig LauncherConfig { get; set; }

    public string Language { get; set; }

    public PassportUserInfo? PassportUserInfo { get; set; }


    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameInfo>> GetGameInfoAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// 版本背景图和版本亮点
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameBackgroundInfo>> GetGameBackgroundAsync(CancellationToken cancellationToken = default);


    /// <summary>
    /// 轮播图、资讯、媒体标签
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<GameContent> GetGameContentAsync(GameId gameId, CancellationToken cancellationToken = default);


    /// <summary>
    /// 游戏安装包
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GamePackage>> GetGamePackageAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameChannelSDK>> GetGameChannelSDKAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// 需要删除的文件
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameDeprecatedFileConfig>> GetGameDeprecatedFileConfigAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// 游戏配置
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameConfig>> GetGameConfigAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取游戏扫描信息，不同版本exe的md5
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameScanInfo>> GetGameScanInfosAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameBranch>> GetGameBranchAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// Chunk 下载模式文件清单
    /// </summary>
    /// <param name="gameBranchPackage"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Task<GameSophonChunkBuild> GetGameSophonChunkBuildAsync(GameBranchPackage gameBranchPackage, string? version = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// Chunk 下载模式的增量更新补丁文件清单
    /// </summary>
    /// <param name="gameBranchPackage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public Task<GameSophonPatchBuild> GetGameSophonPatchBuildAsync(GameBranchPackage gameBranchPackage, CancellationToken cancellationToken = default);


    /// <summary>
    /// WPF Package
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<WPFPackageInfo>> GetWPFPackagesAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取 DirectX 配置
    /// </summary>
    /// <param name="gpuInfos"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameDXConfig>> GetDXConfigsAsync(IEnumerable<GPUInfo> gpuInfos, IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// 游戏插件
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GamePluginRelease>> GetGamePluginsAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default);


    /// <summary>
    /// 游戏预约页面
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public Task<GameReservationContent> GetGameReservationContentAsync(GameId gameId, CancellationToken cancellation = default);

}
