using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class LauncherPost
{
    [JsonPropertyName("post_id")]
    public string PostId { get; set; }

    [JsonPropertyName("type")]
    [JsonConverter(typeof(JsonStringEnumConverter<PostType>))]
    public PostType Type { get; set; }

    [JsonPropertyName("tittle")]
    public string Tittle { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("show_time")]
    public string ShowTime { get; set; }

    [JsonPropertyName("order")]
    public string Order { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}