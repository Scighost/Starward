using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.BH3.DailyNote;

/// <summary>
/// 往事乐土
/// </summary>
public class GodWar
{

    /// <summary>
    /// 结束时间戳，秒
    /// </summary>
    [JsonPropertyName("schedule_end")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset ScheduleEnd { get; set; }

    /// <summary>
    /// 当前分数
    /// </summary>
    [JsonPropertyName("cur_reward")]
    public int CurReward { get; set; }

    /// <summary>
    /// 最大分数
    /// </summary>
    [JsonPropertyName("max_reward")]
    public int MaxReward { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [JsonPropertyName("is_open")]
    public bool IsOpen { get; set; }
}