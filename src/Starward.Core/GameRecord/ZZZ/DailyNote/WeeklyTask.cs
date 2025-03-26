using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DailyNote;

/// <summary>
/// 丽都周纪积分
/// </summary>
public class WeeklyTask
{

    /// <summary>
    /// 剩余刷新时间，秒
    /// </summary>
    [JsonPropertyName("refresh_time")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan RefreshTime { get; set; }

    /// <summary>
    /// 当前积分
    /// </summary>
    [JsonPropertyName("cur_point")]
    public int CurPoint { get; set; }

    /// <summary>
    /// 最大积分
    /// </summary>
    [JsonPropertyName("max_point")]
    public int MaxPoint { get; set; }
}