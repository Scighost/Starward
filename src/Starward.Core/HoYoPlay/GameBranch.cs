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

    // main or predownload
    [JsonPropertyName("branch")]
    public string Branch { get; set; }


    [JsonPropertyName("password")]
    public string Password { get; set; }


    /// <summary>
    /// 游戏版本
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; }


    /// <summary>
    /// 可使用 LDIFF 更新的游戏版本
    /// </summary>
    [JsonPropertyName("diff_tags")]
    public List<string> DiffTags { get; set; }


    /// <summary>
    /// 
    /// </summary>
    [JsonPropertyName("categories")]
    public List<GameBranchPackageCategory> Categories { get; set; }

}



public class GameBranchPackageCategory
{

    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; }


    [JsonPropertyName("matching_field")]
    public string MatchingField { get; set; }

}