using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

public class WPFPackageInfo
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }

    [JsonPropertyName("wpf_package")]
    public WPFPackage WPFPackage { get; set; }

}


public class WPFPackage
{

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

}