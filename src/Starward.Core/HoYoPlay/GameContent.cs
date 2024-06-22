using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 游戏轮播图和资讯
/// </summary>
public class GameContent
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }


    [JsonPropertyName("language")]
    public string Language { get; set; }

    /// <summary>
    /// 轮播图
    /// </summary>
    [JsonPropertyName("banners")]
    public List<GameBanner> Banners { get; set; }

    /// <summary>
    /// 资讯
    /// </summary>
    [JsonPropertyName("posts")]
    public List<GamePost> Posts { get; set; }


    /// <summary>
    /// 媒体分享图标
    /// </summary>
    [JsonPropertyName("social_media_list")]
    public List<GameSocialMedia> SocialMediaList { get; set; }


}



/// <summary>
/// 轮播图
/// </summary>
public class GameBanner
{

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("image")]
    public GameImage Image { get; set; }

}


/// <summary>
/// 资讯
/// </summary>
public class GamePost
{

    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// 资讯类型，<see cref="GamePostType"/>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }


    [JsonPropertyName("title")]
    public string Title { get; set; }

    /// <summary>
    /// 链接
    /// </summary>
    [JsonPropertyName("link")]
    public string Link { get; set; }

    /// <summary>
    /// 日期，mm/dd
    /// </summary>
    [JsonPropertyName("date")]
    public string Date { get; set; }

}



public abstract class GamePostType
{
    public const string POST_TYPE_ACTIVITY = "POST_TYPE_ACTIVITY";

    public const string POST_TYPE_ANNOUNCE = "POST_TYPE_ANNOUNCE";

    public const string POST_TYPE_INFO = "POST_TYPE_INFO";
}



public class GameSocialMedia
{

    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// 分享图标
    /// </summary>
    [JsonPropertyName("icon")]
    public GameImage Icon { get; set; }

    /// <summary>
    /// 二维码图片
    /// </summary>
    [JsonPropertyName("qr_image")]
    public GameImage QrImage { get; set; }


    [JsonPropertyName("qr_desc")]
    public string QrDesc { get; set; }

    /// <summary>
    /// 更多文字链接
    /// </summary>
    [JsonPropertyName("links")]
    public List<GameSocialMediaLink> Links { get; set; }


    [JsonPropertyName("enable_red_dot")]
    public bool EnableRedDot { get; set; }


    [JsonPropertyName("red_dot_content")]
    public string RedDotContent { get; set; }

}



public class GameSocialMediaLink
{

    [JsonPropertyName("title")]
    public string Title { get; set; }


    [JsonPropertyName("link")]
    public string Link { get; set; }

}