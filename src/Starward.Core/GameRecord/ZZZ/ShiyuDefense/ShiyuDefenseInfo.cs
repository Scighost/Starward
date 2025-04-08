using Starward.Core.GameRecord.ZZZ.Common;
using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseInfo
{

    [JsonIgnore]
    public int Uid { get; set; }

    [JsonPropertyName("schedule_id")]
    public int ScheduleId { get; set; }

    [JsonPropertyName("begin_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset BeginTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset EndTime { get; set; }

    [JsonPropertyName("rating_list")]
    public List<ShiyuDefenseTotalRatings> RatingList { get; set; }

    private ZZZRating? _rating;

    private int? _ratingCount;

    [JsonIgnore]
    public ZZZRating Rating
    {
        get => _rating ?? RatingList?.FirstOrDefault()?.Rating ?? ZZZRating.C;
        set => _rating = value;
    }

    [JsonIgnore]
    public int RatingCount
    {
        get => _ratingCount ?? RatingList?.FirstOrDefault()?.Times ?? 0;
        set => _ratingCount = value;
    }

    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }

    [JsonPropertyName("all_floor_detail")]
    public List<ShiyuDefenseFloorDetail> AllFloorDetail { get; set; }

    [JsonPropertyName("fast_layer_time")]
    public int FastLayerTime { get; set; }

    [JsonPropertyName("max_layer")]
    public int MaxLayer { get; set; }

    [JsonPropertyName("hadal_begin_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime HadalBeginTime { get; set; }

    [JsonPropertyName("hadal_end_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime HadalEndTime { get; set; }

    [JsonPropertyName("battle_time_47")]
    public int BattleTime47 { get; set; }

    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


}
