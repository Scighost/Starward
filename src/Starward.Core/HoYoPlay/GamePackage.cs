using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 游戏安装包信息
/// </summary>
public class GamePackage
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }

    /// <summary>
    /// 当前版本
    /// </summary>
    [JsonPropertyName("main")]
    public GamePackageVersion Main { get; set; }

    /// <summary>
    /// 预下载版本
    /// </summary>
    [JsonPropertyName("pre_download")]
    public GamePackageVersion PreDownload { get; set; }

}


/// <summary>
/// 安装包版本
/// </summary>
public class GamePackageVersion
{
    /// <summary>
    /// 完整安装包，无预下载时为空
    /// </summary>
    [JsonPropertyName("major")]
    public GamePackageResource? Major { get; set; }

    /// <summary>
    /// 增量更新包
    /// </summary>
    [JsonPropertyName("patches")]
    public List<GamePackageResource> Patches { get; set; }

}



/// <summary>
/// 安装包资源
/// </summary>
public class GamePackageResource
{

    /// <summary>
    /// 完整安装包的版本，增量更新包的对应版本
    /// </summary>
    [JsonPropertyName("version")]
    public string Version { get; set; }

    /// <summary>
    /// 游戏本体
    /// </summary>
    [JsonPropertyName("game_pkgs")]
    public List<GamePackageFile> GamePackages { get; set; }

    /// <summary>
    /// 本地化音频资源
    /// </summary>
    [JsonPropertyName("audio_pkgs")]
    public List<GamePackageFile> AudioPackages { get; set; }

    /// <summary>
    /// 散装文件前缀，可能是空字符串
    /// </summary>
    [JsonPropertyName("res_list_url")]
    public string ResListUrl { get; set; }

}



/// <summary>
/// 游戏安装包文件
/// </summary>
public class GamePackageFile
{

    /// <summary>
    /// 本地音频资源的语言
    /// </summary>
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    /// <summary>
    /// 下载链接
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }

    /// <summary>
    /// 全是小写
    /// </summary>
    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

    /// <summary>
    /// 压缩包大小
    /// </summary>
    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    /// <summary>
    /// 压缩包大小 + 解压后所有文件
    /// </summary>
    [JsonPropertyName("decompressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long DecompressedSize { get; set; }

}


