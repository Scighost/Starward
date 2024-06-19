using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class LauncherPost
{
    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<PostType>))]
    public PostType Type { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonPropertyName("date")]
    public string Date { get; set; }
}