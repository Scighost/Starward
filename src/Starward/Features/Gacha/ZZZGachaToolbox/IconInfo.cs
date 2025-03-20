using System.Collections.Generic;
using System.Text.Json.Serialization;


namespace Starward.Features.Gacha.ZZZGachaToolbox;

/// <summary>
/// https://act-api-takumi.mihoyo.com/event/nap_cultivate_tool/user/icon_info
/// https://sg-public-api.hoyolab.com/event/nap_cultivate_tool/user/icon_info
/// </summary>
internal class IconInfo
{

    [JsonPropertyName("avatar_icon")]
    public Dictionary<string, IconInfoItem> AvatarIcons { get; set; }


    [JsonPropertyName("buddy_icon")]
    public Dictionary<string, IconInfoItem> BuddyIcons { get; set; }

}



internal class IconInfoItem
{

    [JsonPropertyName("square_avatar")]
    public string SquareAvatar { get; set; }

    [JsonPropertyName("rectangle_avatar")]
    public string RectangleAvatar { get; set; }

    [JsonPropertyName("vertical_painting")]
    public string VerticalPainting { get; set; }

    [JsonPropertyName("vertical_painting_color")]
    public string VerticalPaintingColor { get; set; }

    [JsonPropertyName("avatar_us_full_name")]
    public string AvatarUsFullName { get; set; }

    [JsonPropertyName("teaser_avatar")]
    public string TeaserAvatar { get; set; }

}