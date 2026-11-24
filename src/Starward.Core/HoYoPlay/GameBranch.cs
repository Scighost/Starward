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


    [JsonPropertyName("enable_base_pkg_predownload")]
    public bool EnableBasePackagePreDownload { get; set; }

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


    [JsonPropertyName("required_client_version")]
    public string RequiredClientVersion { get; set; }

}



public class GameBranchPackageCategory
{

    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; }


    [JsonPropertyName("matching_field")]
    public string MatchingField { get; set; }

    /// <summary>
    /// <see cref="CategoryType"/>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    /// <summary>
    /// <see cref="CategoryScenario"/>
    /// </summary>
    [JsonPropertyName("scenarios")]
    public List<string> Scenarios { get; set; }

}


public abstract class CategoryType
{
    public const string CATEGORY_TYPE_RESOURCE = "CATEGORY_TYPE_RESOURCE";

    public const string CATEGORY_TYPE_AUDIO = "CATEGORY_TYPE_AUDIO";

}

public abstract class CategoryScenario
{
    /// <summary>
    /// url query: scenarios_filter[]=1
    /// </summary>
    public const string CATEGORY_SCENARIO_FULL = "CATEGORY_SCENARIO_FULL";

    /// <summary>
    /// url query: scenarios_filter[]=2
    /// </summary>
    public const string CATEGORY_SCENARIO_BASE = "CATEGORY_SCENARIO_BASE";

}