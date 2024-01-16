using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.TrailblazeCalendar;

/// <summary>
/// 开拓月历-每日数据
/// </summary>
public class TrailblazeCalendarDayData
{
    /// <summary>
    /// 今天的星琼
    /// </summary>
    [JsonPropertyName("current_hcoin")]
    public int CurrentHcoin { get; set; }

    /// <summary>
    /// 今天的星轨票
    /// </summary>
    [JsonPropertyName("current_rails_pass")]
    public int CurrentRailsPass { get; set; }

    /// <summary>
    /// 昨天的星琼
    /// </summary>
    [JsonPropertyName("last_hcoin")]
    public int LastHcoin { get; set; }

    /// <summary>
    /// 昨天的星轨票
    /// </summary>
    [JsonPropertyName("last_rails_pass")]
    public int LastRailsPass { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}




