using System.Text.Json.Serialization;

namespace Starward.GameInstall.HotUpdate;

internal class PatchNodeVersionItem
{
    [JsonPropertyName("branch")]
    public string Branch { get; set; }

    [JsonPropertyName("suffix")]
    public string Suffix { get; set; }

    [JsonPropertyName("revision")]
    public int Revision { get; set; }

    [JsonPropertyName("blkVersionHash")]
    public string BlkVersionHash { get; set; }

    [JsonPropertyName("resVersionHash")]
    public string ResVersionHash { get; set; }

    [JsonPropertyName("blkVersionMd5")]
    public string BlkVersionMd5 { get; set; }

    [JsonPropertyName("resVersionMd5")]
    public string ResVersionMd5 { get; set; }

    [JsonPropertyName("blkVersionFileSize")]
    public int BlkVersionFileSize { get; set; }

    [JsonPropertyName("resVersionFileSize")]
    public int ResVersionFileSize { get; set; }
}