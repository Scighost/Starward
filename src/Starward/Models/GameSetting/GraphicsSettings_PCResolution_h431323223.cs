using System.Text.Json.Serialization;

namespace Starward.Models.GameSetting;

public class GraphicsSettings_PCResolution_h431323223
{
    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("isFullScreen")]
    public bool IsFullScreen { get; set; }
}