using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class LauncherBanner
{
    [JsonPropertyName("banner_id")]
    public string BannerId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("img")]
    public string Img { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("order")]
    public string Order { get; set; }
}
