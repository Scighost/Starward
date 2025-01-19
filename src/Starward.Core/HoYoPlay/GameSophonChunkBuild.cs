using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


public class GameSophonChunkBuild
{

    [JsonPropertyName("build_id")]
    public string BuildId { get; set; }


    /// <summary>
    /// 游戏版本
    /// </summary>
    [JsonPropertyName("tag")]
    public string Tag { get; set; }


    /// <summary>
    /// 文件清单
    /// </summary>
    [JsonPropertyName("manifests")]
    public List<GameSophonChunkManifest> Manifests { get; set; }


}


/// <summary>
/// Chunk 下载模式文件清单
/// </summary>
public class GameSophonChunkManifest
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
    public GameSophonManifestFile Manifest { get; set; }


    /// <summary>
    /// 分块文件下载链接前缀
    /// </summary>
    [JsonPropertyName("chunk_download")]
    public GameSophonManifestUrl ChunkDownload { get; set; }


    /// <summary>
    /// 清单文件下载链接前缀
    /// </summary>
    [JsonPropertyName("manifest_download")]
    public GameSophonManifestUrl ManifestDownload { get; set; }


    /// <summary>
    /// 文件统计信息
    /// </summary>
    [JsonPropertyName("stats")]
    public GameSophonManifestStats Stats { get; set; }


    /// <summary>
    /// 去重后的文件统计信息
    /// </summary>
    [JsonPropertyName("deduplicated_stats")]
    public GameSophonManifestStats DeduplicatedStats { get; set; }

}



public class GameSophonManifestFile
{
    /// <summary>
    /// 文件名，与下载链接前缀拼接为下载链接
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// 解压后的MD5
    /// </summary>
    [JsonPropertyName("checksum")]
    public string Checksum { get; set; }


    [JsonPropertyName("compressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long CompressedSize { get; set; }


    [JsonPropertyName("uncompressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long UncompressedSize { get; set; }

}



/// <summary>
/// 文件下载链接前缀
/// </summary>
public class GameSophonManifestUrl
{
    /// <summary>
    /// 加密
    /// </summary>
    [JsonPropertyName("encryption")]
    public int Encryption { get; set; }

    /// <summary>
    /// 密码
    /// </summary>
    [JsonPropertyName("password")]
    public string Password { get; set; }

    /// <summary>
    /// 压缩
    /// </summary>
    [JsonPropertyName("compression")]
    public int Compression { get; set; }

    /// <summary>
    /// 前缀
    /// </summary>
    [JsonPropertyName("url_prefix")]
    public string UrlPrefix { get; set; }

    /// <summary>
    /// 后缀
    /// </summary>
    [JsonPropertyName("url_suffix")]
    public string UrlSuffix { get; set; }

}




public class GameSophonManifestStats
{

    [JsonPropertyName("compressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long CompressedSize { get; set; }


    [JsonPropertyName("uncompressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long UncompressedSize { get; set; }


    [JsonPropertyName("file_count")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int FileCount { get; set; }


    [JsonPropertyName("chunk_count")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int ChunkCount { get; set; }


}

