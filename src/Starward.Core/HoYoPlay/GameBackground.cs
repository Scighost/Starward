using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;


/// <summary>
/// 游戏版本背景图和版本亮点
/// </summary>
public class GameBackgroundInfo
{

    [JsonPropertyName("game")]
    public GameId GameId { get; set; }


    [JsonPropertyName("backgrounds")]
    public List<GameBackground> Backgrounds { get; set; }

}


/// <summary>
/// 游戏版本背景图和版本亮点
/// </summary>
public class GameBackground
{

    [JsonPropertyName("id")]
    public string Id { get; set; }

    /// <summary>
    /// 背景图
    /// </summary>
    [JsonPropertyName("background")]
    public GameImage Background { get; set; }

    /// <summary>
    /// 版本亮点
    /// </summary>
    [JsonPropertyName("icon")]
    public GameImage Icon { get; set; }

    /// <summary>
    /// 仅 <see cref="GameImage.Url"/> 有效
    /// </summary>
    [JsonPropertyName("video")]
    public GameImage Video { get; set; }

    /// <summary>
    /// 视频背景上的叠加图片
    /// </summary>
    [JsonPropertyName("theme")]
    public GameImage Theme { get; set; }

    /// <summary>
    /// <see cref="BACKGROUND_TYPE_UNSPECIFIED"/> or <see cref="BACKGROUND_TYPE_VIDEO"/>
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; }


    [JsonIgnore]
    public bool StopVideo { get; set; }


    public const string BACKGROUND_TYPE_UNSPECIFIED = nameof(BACKGROUND_TYPE_UNSPECIFIED);
    public const string BACKGROUND_TYPE_VIDEO = nameof(BACKGROUND_TYPE_VIDEO);
    public const string BACKGROUND_TYPE_POSTER = nameof(BACKGROUND_TYPE_POSTER);
    public const string BACKGROUND_TYPE_CUSTOM = nameof(BACKGROUND_TYPE_CUSTOM);


    public static GameBackground FromPosterUrl(string url)
    {
        return new GameBackground
        {
            Id = BACKGROUND_TYPE_POSTER,
            Background = new GameImage { Url = url },
            Type = BACKGROUND_TYPE_POSTER,
        };
    }


    public static GameBackground FromCustomFile(string path)
    {
        return new GameBackground
        {
            Id = BACKGROUND_TYPE_CUSTOM,
            Background = new GameImage { Url = path },
            Type = BACKGROUND_TYPE_CUSTOM,
        };
    }


}
