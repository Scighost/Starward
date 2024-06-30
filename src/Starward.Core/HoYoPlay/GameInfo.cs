using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 游戏基本信息
/// </summary>
public class GameInfo : GameId
{

    /// <summary>
    /// 名称、图标、背景、缩略图等
    /// </summary>
    [JsonPropertyName("display")]
    public GameInfoDisplay Display { get; set; }


    /// <summary>
    /// 预约链接
    /// </summary>
    [JsonPropertyName("reservation")]
    public GameInfoReservation? Reservation { get; set; }

    /// <summary>
    /// <see cref="GameInfoDisplayStatus"/>
    /// </summary>
    [JsonPropertyName("display_status")]
    public string DisplayStatus { get; set; }


}


public class GameInfoDisplay
{


    [JsonPropertyName("language")]
    public string Language { get; set; }



    [JsonPropertyName("name")]
    public string Name { get; set; }


    /// <summary>
    /// 图标
    /// </summary>
    [JsonPropertyName("icon")]
    public GameImage Icon { get; set; }



    [JsonPropertyName("title")]
    public string Title { get; set; }



    [JsonPropertyName("subtitle")]
    public string Subtitle { get; set; }


    /// <summary>
    /// 大背景图
    /// </summary>
    [JsonPropertyName("background")]
    public GameImage Background { get; set; }


    /// <summary>
    /// 游戏Logo
    /// </summary>
    [JsonPropertyName("logo")]
    public GameImage Logo { get; set; }


    /// <summary>
    /// 小缩略背景图
    /// </summary>
    [JsonPropertyName("thumbnail")]
    public GameImage Thumbnail { get; set; }

}


/// <summary>
/// 预约链接
/// </summary>
public class GameInfoReservation
{
    [JsonPropertyName("link")]
    public string Link { get; set; }
}



public abstract class GameInfoDisplayStatus
{
    public const string LAUNCHER_GAME_DISPLAY_STATUS_AVAILABLE = "LAUNCHER_GAME_DISPLAY_STATUS_AVAILABLE";

    public const string LAUNCHER_GAME_DISPLAY_STATUS_COMING_SOON = "LAUNCHER_GAME_DISPLAY_STATUS_COMING_SOON";

    public const string LAUNCHER_GAME_DISPLAY_STATUS_RESERVATION_ENABLED = "LAUNCHER_GAME_DISPLAY_STATUS_RESERVATION_ENABLED";
}