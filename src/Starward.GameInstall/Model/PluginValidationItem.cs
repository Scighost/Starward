using System.Text.Json.Serialization;

namespace Starward.GameInstall.Model;

internal class PluginValidationItem
{
    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long? Size { get; set; }
}