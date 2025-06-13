using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 游戏配置
/// </summary>
public class GameConfig
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }


    /// <summary>
    /// exe 文件名（带.exe后缀）
    /// </summary>
    [JsonPropertyName("exe_file_name")]
    public string ExeFileName { get; set; }


    /// <summary>
    /// 默认安装文件夹名称
    /// </summary>
    [JsonPropertyName("installation_dir")]
    public string InstallationDir { get; set; }


    /// <summary>
    /// 音频配置文件（不是文件夹，相对于游戏安装目录，标记哪些语音包已下载）
    /// </summary>
    [JsonPropertyName("audio_pkg_scan_dir")]
    public string AudioPackageScanDir { get; set; }


    /// <summary>
    /// 音频文件路径（相对于游戏安装目录）
    /// </summary>
    [JsonPropertyName("audio_pkg_res_dir")]
    public string AudioPackageResDir { get; set; }


    /// <summary>
    /// 音频资源缓存路径（相对于游戏安装目录）
    /// </summary>
    [JsonPropertyName("audio_pkg_cache_dir")]
    public string AudioPackageCacheDir { get; set; }


    /// <summary>
    /// 游戏资源缓存路径（相对于游戏安装目录）
    /// </summary>
    [JsonPropertyName("game_cached_res_dir")]
    public string GameCachedResDir { get; set; }


    /// <summary>
    /// 截图文件夹（相对于游戏安装目录）
    /// </summary>
    [JsonPropertyName("game_screenshot_dir")]
    public string GameScreenshotDir { get; set; }


    /// <summary>
    /// 游戏日志文件夹（包含 Windows 环境变量路径，如 %UserProfile%）
    /// </summary>
    [JsonPropertyName("game_log_gen_dir")]
    public string GameLogGenDir { get; set; }


    /// <summary>
    /// 崩溃文件（包含 Windows 环境变量路径，如 %UserProfile%）
    /// </summary>
    [JsonPropertyName("game_crash_file_gen_dir")]
    public string GameCrashFileGenDir { get; set; }


    /// <summary>
    /// 默认下载模式，<see cref="DownloadMode"/>
    /// </summary>
    [JsonPropertyName("default_download_mode")]
    public string DefaultDownloadMode { get; set; }


    // bool enable_customer_service
    [JsonPropertyName("enable_customer_service")]
    public bool EnableCustomerService { get; set; }


    [JsonPropertyName("local_res_dir")]
    public string LocalResDir { get; set; }


    [JsonPropertyName("local_res_cache_dir")]
    public string LocalResCacheDir { get; set; }

    /// <summary>
    /// 此文件用于标记已删除的资源目录，没有资源被删除时此文件不存在，文件内容举个例子：
    /// {"category":"10302","is_delete":true}
    /// {"category":"30102012","is_delete":true}
    /// 其中 category 对应 <see cref="GameSophonChunkManifest.MatchingField"/> 和 <see cref="GameSophonPatchManifest.MatchingField"/>
    /// </summary>
    [JsonPropertyName("res_category_dir")]
    public string ResCategoryDir { get; set; }

    /// <summary>
    /// 此文件记录的本地资源目录
    /// </summary>
    [JsonPropertyName("game_res_cut_dir")]
    public string GameResCutDir { get; set; }


    [JsonPropertyName("enable_game_log_export")]
    public bool EnableGameLogExport { get; set; }


    [JsonPropertyName("game_log_export_config")]
    public object? GameLogExportConfig { get; set; }


    [JsonPropertyName("blacklist_dir")]
    public string BlacklistDir { get; set; }


    [JsonPropertyName("wpf_exe_dir")]
    public string WpfExeDir { get; set; }


    [JsonPropertyName("wpf_pkg_version_dir")]
    public string WpfPackageVersionDir { get; set; }

}


public abstract class DownloadMode
{
    public const string DOWNLOAD_MODE_FILE = "DOWNLOAD_MODE_FILE";

    public const string DOWNLOAD_MODE_CHUNK = "DOWNLOAD_MODE_CHUNK";

    public const string DOWNLOAD_MODE_LDIFF = "DOWNLOAD_MODE_LDIFF";
}