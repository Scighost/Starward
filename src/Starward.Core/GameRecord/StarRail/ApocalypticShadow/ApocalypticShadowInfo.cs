using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ApocalypticShadow;

public class ApocalypticShadowInfo
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("schedule_id")]
    public int ScheduleId { get; set; }

    [JsonPropertyName("begin_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime BeginTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("upper_boss_icon")]
    public string UpperBossIcon { get; set; }

    [JsonPropertyName("lower_boss_icon")]
    public string LowerBossIcon { get; set; }

    [JsonPropertyName("star_num")]
    public int StarNum { get; set; }

    [JsonPropertyName("max_floor")]
    public string MaxFloor { get; set; }

    [JsonPropertyName("battle_num")]
    public int BattleNum { get; set; }

    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }

    [JsonPropertyName("all_floor_detail")]
    public List<ApocalypticShadowFloorDetail> AllFloorDetail { get; set; }

    /// <summary>
    /// 第一个是当期，第二个是上期
    /// </summary>
    [JsonPropertyName("groups")]
    public List<ApocalypticShadowMeta>? Metas { get; set; }

    [JsonIgnore]
    public ApocalypticShadowMeta? Meta
    {
        get
        {
            return Metas?.FirstOrDefault(x => x.ScheduleId == this.ScheduleId);
        }
    }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


}
