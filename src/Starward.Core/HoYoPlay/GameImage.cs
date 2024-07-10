using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

/// <summary>
/// 图片、带有链接的图片
/// </summary>
public class GameImage
{

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("hover_url")]
    public string? HoverUrl { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; }

}
