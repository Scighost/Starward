using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

/// <summary>
/// 图片、带有链接的图片
/// </summary>
public class GameImage
{

    /// <summary>
    /// 图片链接
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("hover_url")]
    public string? HoverUrl { get; set; }

    /// <summary>
    /// 点击图片后打开的链接
    /// </summary>
    [JsonPropertyName("link")]
    public string Link { get; set; }

}
