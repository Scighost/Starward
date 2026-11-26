using System.Text.Json.Serialization;

namespace Starward.GameInstall.HotUpdate;

public class HotPatchItem
{
    [JsonPropertyName("remoteName")]
    public string RemoteName { get; set; }

    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("fileSize")]
    public long FileSize { get; set; }

    [JsonPropertyName("category")]
    public int Category { get; set; }

    [JsonPropertyName("localName")]
    public string? LocalName { get; set; }

    [JsonPropertyName("isPatch")]
    public bool IsPatch { get; set; }

    [JsonIgnore]
    public string Source { get; set; }

    public const string Res = "Res";
    public const string Data = "Data";
    public const string Silence = "Silence";
}