using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class LauncherContent
{

    [JsonPropertyName("adv")]
    public BackgroundImage BackgroundImage { get; set; }

    [JsonPropertyName("banner")]
    public List<LauncherBanner> Banner { get; set; }

    [JsonPropertyName("post")]
    public List<LauncherPost> Post { get; set; }

}
