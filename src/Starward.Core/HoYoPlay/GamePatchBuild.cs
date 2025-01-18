using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


public class GamePatchBuild
{

    [JsonPropertyName("build_id")]
    public string BuildId { get; set; }


    [JsonPropertyName("patch_id")]
    public string PatchId { get; set; }


    /// <summary>
    /// 游戏版本
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; }


    /// <summary>
    /// 文件清单
    /// </summary>
    [JsonPropertyName("manifests")]
    public List<GamePatchManifest> Manifests { get; set; }


}


/// <summary>
/// Chunk 下载模式补丁文件清单
/// </summary>
public class GamePatchManifest
{
    /// <summary>
    /// 资源ID
    /// </summary>
    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; }

    /// <summary>
    /// 资源名称
    /// </summary>
    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; }

    /// <summary>
    /// 资源类型，game or 语音类型（zh-cn, en-us, ja-jp, ko-kr）
    /// </summary>
    [JsonPropertyName("matching_field")]
    public string MatchingField { get; set; }

    /// <summary>
    /// 清单文件信息
    /// </summary>
    [JsonPropertyName("manifest")]
    public GameChunkManifestFile Manifest { get; set; }


    /// <summary>
    /// 分块文件下载链接前缀
    /// </summary>
    [JsonPropertyName("diff_download")]
    public GameChunkManifestUrl DiffDownload { get; set; }


    /// <summary>
    /// 清单文件下载链接前缀
    /// </summary>
    [JsonPropertyName("manifest_download")]
    public GameChunkManifestUrl ManifestDownload { get; set; }


    /// <summary>
    /// 文件统计信息
    /// </summary>
    [JsonPropertyName("stats")]
    public Dictionary<string, GameChunkManifestStats> Stats { get; set; }


}
