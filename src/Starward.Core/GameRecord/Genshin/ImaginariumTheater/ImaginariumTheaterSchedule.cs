using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterSchedule
{
    /// <summary>
    /// 开始时间，timestamp
    /// </summary>
    [JsonPropertyName("start_time")]
    public string StartTime { get; set; }

    /// <summary>
    /// 结束时间，timestamp
    /// </summary>
    [JsonPropertyName("end_time")]
    public string EndTime { get; set; }

    [JsonPropertyName("schedule_type")]
    public int ScheduleType { get; set; }

    [JsonPropertyName("schedule_id")]
    public int ScheduleId { get; set; }

    [JsonPropertyName("start_date_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime StartDateTime { get; set; }

    [JsonPropertyName("end_date_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime EndDateTime { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

