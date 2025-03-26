using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DailyNote;

/// <summary>
/// 电量（体力）
/// </summary>
public class Energy
{
    /// <summary>
    /// 当前电量、最大电量
    /// </summary>
    [JsonPropertyName("progress")]
    public EnergyProgress Progress { get; set; }

    /// <summary>
    /// 电量恢复时间，秒
    /// </summary>
    [JsonPropertyName("restore")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan Restore { get; set; }

    /// <summary>
    /// 电量恢复慢的日期，1：今日； 2：明日
    /// </summary>
    [JsonPropertyName("day_type")]
    public int DayType { get; set; }

    /// <summary>
    /// 电量恢复慢的时间，小时
    /// </summary>
    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    /// <summary>
    /// 电量恢复慢的时间，分钟
    /// </summary>
    [JsonPropertyName("minute")]
    public int Minute { get; set; }


    [JsonIgnore]
    public bool IsFull => Progress.Current >= Progress.Max;

}


/// <summary>
/// 电量（体力）进度
/// </summary>
public class EnergyProgress
{

    [JsonPropertyName("max")]
    public int Max { get; set; }

    [JsonPropertyName("current")]
    public int Current { get; set; }

}