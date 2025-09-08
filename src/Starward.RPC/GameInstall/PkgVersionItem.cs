using System.Text.Json.Serialization;

namespace Starward.RPC.GameInstall;

internal class PkgVersionItem
{

    [JsonPropertyName("remoteName")]
    public string RemoteName { get; set; }

    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

    [JsonPropertyName("fileSize")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
    public long FileSize { get; set; }

}



internal class BlacklistItem
{

    [JsonPropertyName("fileName")]
    public string FileName { get; set; }

}