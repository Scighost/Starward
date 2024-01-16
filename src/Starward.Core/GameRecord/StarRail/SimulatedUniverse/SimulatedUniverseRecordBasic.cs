using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

public class SimulatedUniverseRecordBasic
{
    [JsonPropertyName("id")]
    public int ScheduleId { get; set; }

    /// <summary>
    /// 通关次数
    /// </summary>
    [JsonPropertyName("finish_cnt")]
    public int FinishCount { get; set; }

    [JsonPropertyName("schedule_begin")]
    [JsonConverter(typeof(SimulatedUniverseTimeJsonConverter))]
    public DateTime ScheduleBegin { get; set; }

    [JsonPropertyName("schedule_end")]
    [JsonConverter(typeof(SimulatedUniverseTimeJsonConverter))]
    public DateTime ScheduleEnd { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


