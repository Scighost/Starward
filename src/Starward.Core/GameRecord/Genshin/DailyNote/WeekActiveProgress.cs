using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.DailyNote;

/// <summary>
/// 砺行修远
/// </summary>
public class WeekActiveProgress
{

    /// <summary>
    /// 本周砺行目标当前进度
    /// </summary>
    [JsonPropertyName("progress_current")]
    public int ProgressCurrent { get; set; }


    /// <summary>
    /// 本周砺行目标进度上限
    /// </summary>
    [JsonPropertyName("progress_total")]
    public int ProgressTotal { get; set; }


    /// <summary>
    /// 本期砺行目标当前进度
    /// </summary>
    [JsonPropertyName("period_progress_current")]
    public int PeriodProgressCurrent { get; set; }


    /// <summary>
    /// 本期砺行目标进度上限
    /// </summary>
    [JsonPropertyName("period_progress_total")]
    public int PeriodProgressTotal { get; set; }


    /// <summary>
    /// 是否解锁
    /// </summary>
    [JsonPropertyName("unlock")]
    public bool IsUnlock { get; set; }


    /// <summary>
    /// 本周每日砺行目标完成情况
    /// </summary>
    [JsonPropertyName("progress_current_arr")]
    public List<int> ProgressCurrentArr { get; set; }


    /// <summary>
    /// 是否处于活动期间
    /// </summary>
    [JsonPropertyName("is_active_period")]
    public bool IsActivePeriod { get; set; }


    /// <summary>
    /// 当前星期几，周一为 1
    /// </summary>
    [JsonPropertyName("current_weekday")]
    public int CurrentWeekday { get; set; }


    [JsonIgnore]
    public bool IsDisplay => IsUnlock && IsActivePeriod;

}


