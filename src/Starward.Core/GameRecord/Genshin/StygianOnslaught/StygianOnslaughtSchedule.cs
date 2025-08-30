using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtSchedule
{

    [JsonPropertyName("schedule_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int ScheduleId { get; set; }


    [JsonPropertyName("start_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset StartTime { get; set; }


    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset EndTime { get; set; }


    [JsonPropertyName("start_date_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime StartDateTime { get; set; }


    [JsonPropertyName("end_date_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime EndDateTime { get; set; }


    [JsonPropertyName("is_valid")]
    public bool IsValid { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}