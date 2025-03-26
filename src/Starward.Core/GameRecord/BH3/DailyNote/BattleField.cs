using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.BH3.DailyNote;

/// <summary>
/// 记忆战场
/// </summary>
public class BattleField
{

    /// <summary>
    /// 记忆战场结束时间戳，秒
    /// </summary>
    [JsonPropertyName("schedule_end")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset ScheduleEnd { get; set; }

    /// <summary>
    /// 已获取挑战奖励
    /// </summary>
    [JsonPropertyName("cur_reward")]
    public int CurReward { get; set; }

    /// <summary>
    /// 最大挑战奖励
    /// </summary>
    [JsonPropertyName("max_reward")]
    public int MaxReward { get; set; }

    /// <summary>
    /// 已获取SSS奖励
    /// </summary>
    [JsonPropertyName("cur_sss_reward")]
    public int CurSssReward { get; set; }

    /// <summary>
    /// 最大SSS奖励
    /// </summary>
    [JsonPropertyName("max_sss_reward")]
    public int MaxSssReward { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [JsonPropertyName("is_open")]
    public bool IsOpen { get; set; }
}
