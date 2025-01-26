using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.UpgradeGuide;

public class UpgradeGuidIconInfo
{

    [JsonPropertyName("avatar_icon")]
    public Dictionary<int, UpgradeGuidIconInfoItem> AvatarIcon { get; set; }


    [JsonPropertyName("buddy_icon")]
    public Dictionary<int, UpgradeGuidIconInfoItem> BuddyIcon { get; set; }

}



public class UpgradeGuidIconInfoItem
{

    [JsonPropertyName("square_avatar")]
    public string SquareAvatar { get; set; }


    [JsonPropertyName("rectangle_avatar")]
    public string RectangleAvatar { get; set; }


    [JsonPropertyName("vertical_painting")]
    public string? VerticalPainting { get; set; }


    [JsonPropertyName("vertical_painting_color")]
    public string? VerticalPaintingColor { get; set; }


    [JsonPropertyName("avatar_us_full_name")]
    public string? AvatarUsFullName { get; set; }

}