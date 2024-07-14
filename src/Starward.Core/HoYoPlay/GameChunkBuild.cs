using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

public class GameChunkBuild
{

    // json string build_id
    [JsonPropertyName("build_id")]
    public string BuildId { get; set; }


    // string tag
    [JsonPropertyName("tag")]
    public string Tag { get; set; }


    [JsonPropertyName("manifests")]
    public List<GameChunkManifest> Manifests { get; set; }


}



public class GameChunkManifest
{

    // string category_id
    [JsonPropertyName("category_id")]
    public string CategoryId { get; set; }

    // string category_name
    [JsonPropertyName("category_name")]
    public string CategoryName { get; set; }

    // string matching_field
    [JsonPropertyName("matching_field")]
    public string MatchingField { get; set; }

    // gamechunkmanifestfile manifest
    [JsonPropertyName("manifest")]
    public GameChunkManifestFile Manifest { get; set; }


    // gamechunkmanifesturl chunk_download
    [JsonPropertyName("chunk_download")]
    public GameChunkManifestUrl ChunkDownload { get; set; }


    // gamechunkmanifesturl manifest_download
    [JsonPropertyName("manifest_download")]
    public GameChunkManifestUrl ManifestDownload { get; set; }


    // gamechunkmanifeststats stats
    [JsonPropertyName("stats")]
    public GameChunkManifestStats Stats { get; set; }


    // gamechunkmanifeststats deduplicated_stats
    [JsonPropertyName("deduplicated_stats")]
    public GameChunkManifestStats DeduplicatedStats { get; set; }

}



public class GameChunkManifestFile
{

    [JsonPropertyName("id")]
    public string Id { get; set; }


    [JsonPropertyName("checksum")]
    public string Checksum { get; set; }


    [JsonPropertyName("compressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long CompressedSize { get; set; }


    [JsonPropertyName("uncompressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long UncompressedSize { get; set; }

}




public class GameChunkManifestUrl
{

    [JsonPropertyName("encryption")]
    public int Encryption { get; set; }


    [JsonPropertyName("password")]
    public string Password { get; set; }


    [JsonPropertyName("compression")]
    public int Compression { get; set; }


    [JsonPropertyName("url_prefix")]
    public string UrlPrefix { get; set; }


    [JsonPropertyName("url_suffix")]
    public string UrlSuffix { get; set; }

}



public class GameChunkManifestStats
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
