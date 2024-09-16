using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ApocalypticShadow;

public class ApocalypticShadowMeta
{

    [JsonPropertyName("schedule_id")]
    public int ScheduleId { get; set; }

    [JsonPropertyName("begin_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime BeginTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("name_mi18n")]
    public string Name { get; set; }

    [JsonPropertyName("upper_boss")]
    public ApocalypticShadowBossMeta UpperBoss { get; set; }

    [JsonPropertyName("lower_boss")]
    public ApocalypticShadowBossMeta LowerBoss { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



