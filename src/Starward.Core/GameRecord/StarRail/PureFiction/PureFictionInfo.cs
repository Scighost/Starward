using Starward.Core.GameRecord.StarRail.ForgottenHall;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.PureFiction;

public class PureFictionInfo
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

    /// <summary>
    /// 第一个是当期，第二个是上期
    /// </summary>
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


}
