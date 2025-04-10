using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 式舆防卫战
/// </summary>
public class ShiyuDefenseInfo : IJsonOnDeserialized
{

    [JsonIgnore]
    public int Uid { get; set; }


    [JsonPropertyName("schedule_id")]
    public int ScheduleId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("begin_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset BeginTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset EndTime { get; set; }


    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }

    /// <summary>
    /// 评分列表
    /// </summary>
    [JsonPropertyName("rating_list")]
    public List<ShiyuDefenseRatingTimes> RatingList { get; set; }

    /// <summary>
    /// 最高评分
    /// </summary>
    [JsonIgnore]
    public string MaxRating { get; set; }

    /// <summary>
    /// 最高评分次数
    /// </summary>
    [JsonIgnore]
    public int MaxRatingTimes { get; set; }

    /// <summary>
    /// 式舆防卫战防线
    /// </summary>
    [JsonPropertyName("all_floor_detail")]
    public List<ShiyuDefenseFloorDetail> AllFloorDetail { get; set; }

    /// <summary>
    /// 最快通关时常，秒
    /// </summary>
    [JsonPropertyName("fast_layer_time")]
    public int FastLayerTime { get; set; }

    /// <summary>
    /// 最高通关防线
    /// </summary>
    [JsonPropertyName("max_layer")]
    public int MaxLayer { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("hadal_begin_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime HadalBeginTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("hadal_end_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime HadalEndTime { get; set; }

    /// <summary>
    /// 4-7层通关时常，秒
    /// </summary>
    [JsonPropertyName("battle_time_47")]
    public int BattleTime47 { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    [JsonIgnore]
    public int RatingSTimes { get; set; }

    [JsonIgnore]
    public int RatingATimes { get; set; }

    [JsonIgnore]
    public int RatingBTimes { get; set; }


    public void OnDeserialized()
    {
        MaxRating = RatingList?.FirstOrDefault()?.Rating ?? "";
        MaxRatingTimes = RatingList?.FirstOrDefault()?.Times ?? 0;
        RatingSTimes = RatingList?.FirstOrDefault(x => x.Rating == "S")?.Times ?? 0;
        RatingATimes = RatingList?.FirstOrDefault(x => x.Rating == "A")?.Times ?? 0;
        RatingBTimes = RatingList?.FirstOrDefault(x => x.Rating == "B")?.Times ?? 0;
    }

}
