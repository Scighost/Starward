using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.SpiralAbyss;

/// <summary>
/// 深境螺旋层
/// </summary>
public class SpiralAbyssFloor
{

    [JsonPropertyName("index")]
    public int Index { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("is_unlock")]
    public bool IsUnlock { get; set; }


    [JsonPropertyName("settle_time")]
    public string SettleTime { get; set; }


    [JsonPropertyName("star")]
    public int Star { get; set; }


    [JsonPropertyName("max_star")]
    public int MaxStar { get; set; }


    [JsonPropertyName("levels")]
    public List<SpiralAbyssLevel> Levels { get; set; }

    /// <summary>
    /// 地脉异常
    /// </summary>
    [JsonPropertyName("ley_line_disorder")]
    public List<string> LeyLineDisorder { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
