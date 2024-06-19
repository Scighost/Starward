using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class LauncherBanner
{
    [JsonPropertyName("image")]
    public BannerImage Image { get; set; }
}

public class BannerImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; }
}