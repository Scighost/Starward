using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class BackgroundImage
{
    [JsonPropertyName("background")]
    public string Background { get; set; }

    /// <summary>
    /// 原神版本热点图标
    /// </summary>
    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    /// <summary>
    /// 原神版本热点链接
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("bg_checksum")]
    public string BgChecksum { get; set; }
}
