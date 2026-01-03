using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 式舆防卫战
/// </summary>
public class ShiyuDefenseInfoV2 : ShiyuDefenseInfoBase, IJsonOnDeserialized
{

    [JsonPropertyName("zone_id")]
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

    /// <summary>
    /// 通关第五层
    /// </summary>
    [JsonPropertyName("pass_fifth_floor")]
    public bool PassFifthFloor { get; set; }

    /// <summary>
    /// 通关概要
    /// </summary>
    [JsonPropertyName("brief")]
    public ShiyuDefenseV2Brief Brief { get; set; }

    /// <summary>
    /// 第五层战斗信息
    /// </summary>
    [JsonPropertyName("fitfh_layer_detail")]
    public ShiyuDefenseV2FifthLayerDetail FifthLayerDetail { get; set; }

    /// <summary>
    /// 第四层战斗信息
    /// </summary>
    [JsonPropertyName("fourth_layer_detail")]
    public ShiyuDefenseV2FourthLayerDetail FourthLayerDetail { get; set; }

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


    [JsonIgnore]
    public bool HasData { get; set; }



    public void OnDeserialized()
    {
        Version = "v2";
        HasData = ScheduleId > 0;
        MaxRating = Brief?.Rating ?? "";
        V2Score = Brief?.Score ?? 0;
    }
}
