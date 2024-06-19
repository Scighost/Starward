using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class BackgroundImage
{
    [JsonPropertyName("url")]
    public string Url { get; set; }
}

public class BackgroundImageWrapper
{
    [JsonPropertyName("background")]
    public BackgroundImage BackgroundImage { get; set; }
}
