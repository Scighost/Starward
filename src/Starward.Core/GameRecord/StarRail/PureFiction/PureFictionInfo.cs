using Starward.Core.GameRecord.StarRail.ForgottenHall;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.PureFiction;

public class PureFictionInfo : IJsonOnDeserialized
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("schedule_id")]
    public int ScheduleId { get; set; }

    [JsonPropertyName("begin_time")]
    [JsonConverter(typeof(ForgottenHallTimeJsonConverter))]
    public DateTime BeginTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(ForgottenHallTimeJsonConverter))]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("star_num")]
    public int StarNum { get; set; }

    [JsonPropertyName("max_floor")]
    public string MaxFloor { get; set; }

    [JsonPropertyName("battle_num")]
    public int BattleNum { get; set; }

    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }

    [JsonPropertyName("all_floor_detail")]
    public List<PureFictionFloorDetail> AllFloorDetail { get; set; }

    [JsonPropertyName("groups")]
    public List<PureFictionMeta>? Metas { get; set; }

    [JsonIgnore]
    public string? Name
    {
        get
        {
            if (Metas?.FirstOrDefault(x => x.ScheduleId == this.ScheduleId) is PureFictionMeta meta)
            {
                return meta.Name;
            }
            else
            {
                return null;
            }
        }
    }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    public void OnDeserialized()
    {
        if (ScheduleId == 0 && Metas?.Count == 1)
        {
            ScheduleId = Metas[0].ScheduleId;
        }
        if (BeginTime == DateTime.MinValue && Metas?.Count == 1)
        {
            BeginTime = Metas[0].BeginTime;
        }
        if (EndTime == DateTime.MinValue && Metas?.Count == 1)
        {
            EndTime = Metas[0].EndTime;
        }
    }

}
