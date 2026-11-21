using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

/// <summary>
/// 游戏预约页面
/// </summary>
public class GameReservationContent
{
    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("resolved_language")]
    public string ResolvedLanguage { get; set; }

    [JsonPropertyName("introduction")]
    public string Introduction { get; set; }

    [JsonPropertyName("logo")]
    public string Logo { get; set; }

    [JsonPropertyName("tags")]
    public List<GameReservationTag> Tags { get; set; }

    //[JsonPropertyName("news")]
    //public List<object> News { get; set; }

    [JsonPropertyName("icons")]
    public List<GameReservationIcon> Icons { get; set; }
}


public class GameReservationTag
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}


public class GameReservationMedia
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// <see cref="GameReservationMediaType"/>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("cover")]
    public GameImage Cover { get; set; }

    [JsonPropertyName("resource_url")]
    public string ResourceUrl { get; set; }

    [JsonPropertyName("effective_time")]
    public object? EffectiveTime { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}


public class GameReservationIcon
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("icon")]
    public GameImage Icon { get; set; }

    [JsonPropertyName("link")]
    public string Link { get; set; }

    [JsonPropertyName("enable_red_dot")]
    public bool EnableRedDot { get; set; }

    [JsonPropertyName("red_dot_content")]
    public string RedDotContent { get; set; }

    [JsonPropertyName("qr_image")]
    public GameImage? QrImage { get; set; }

    [JsonPropertyName("links")]
    public List<object> Links { get; set; }

    [JsonPropertyName("order")]
    public int Order { get; set; }
}


public abstract class GameReservationMediaType
{
    public const string RESERVATION_MEDIA_TYPE_IMAGE = "RESERVATION_MEDIA_TYPE_IMAGE";

    public const string RESERVATION_MEDIA_TYPE_VIDEO = "RESERVATION_MEDIA_TYPE_VIDEO";
}
