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

}
