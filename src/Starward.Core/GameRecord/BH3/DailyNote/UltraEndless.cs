using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.BH3.DailyNote;

/// <summary>
/// 超弦空间
/// </summary>
public class UltraEndless
{

    /// <summary>
    /// 结束时间戳，秒
    /// </summary>
    [JsonPropertyName("schedule_end")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset ScheduleEnd { get; set; }

    /// <summary>
    /// 分组
    /// </summary>
    [JsonPropertyName("group_level")]
    public int GroupLevel { get; set; }

    /// <summary>
    /// 挑战分数
    /// </summary>
    [JsonPropertyName("challenge_score")]
    public int ChallengeScore { get; set; }

    /// <summary>
    /// 是否开启
    /// </summary>
    [JsonPropertyName("is_open")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 分组图标
    /// </summary>
    [JsonPropertyName("level_icon")]
    public string LevelIcon { get; set; }
}