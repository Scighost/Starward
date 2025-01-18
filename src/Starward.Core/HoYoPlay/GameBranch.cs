using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// Chunk 下载模式的当前版本和预下载分支
/// </summary>
public class GameBranch
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }


    /// <summary>
    /// 当前版本
    /// </summary>
    [JsonPropertyName("main")]
    public GameBranchPackage Main { get; set; }


    /// <summary>
    /// 预下载
    /// </summary>
    [JsonPropertyName("pre_download")]
    public GameBranchPackage? PreDownload { get; set; }

}



public class GameBranchPackage
{

    [JsonPropertyName("package_id")]
    public string PackageId { get; set; }

    // main or pre_download
    [JsonPropertyName("branch")]
    public string Branch { get; set; }


    [JsonPropertyName("password")]
    public string Password { get; set; }


    /// <summary>
    /// 游戏版本
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; }

}
