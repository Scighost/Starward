using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class BackgroundImage
{
    [JsonPropertyName("background")]
    public string Background { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("bg_checksum")]
    public string BgChecksum { get; set; }
}
