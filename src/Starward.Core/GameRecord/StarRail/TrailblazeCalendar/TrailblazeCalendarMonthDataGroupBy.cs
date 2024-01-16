using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.TrailblazeCalendar;

/// <summary>
/// 开拓月历-每月分组统计数据
/// </summary>
public class TrailblazeCalendarMonthDataGroupBy
{
    [JsonPropertyName("action")]
    public string Action { get; set; }

    [JsonPropertyName("num")]
    public int Num { get; set; }

    [JsonPropertyName("percent")]
    public int Percent { get; set; }

    [JsonPropertyName("action_name")]
    public string ActionName { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}




