using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

public class ThresholdSimulationBrief
{


    [JsonPropertyName("void_front_id")]
    public int VoidFrontId { get; set; }

    /// <summary>
    /// 结束超过43天
    /// </summary>
    [JsonPropertyName("end_ts_over_42_days")]
    public bool EndTsOver42Days { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end_ts")]
    public long EndTs { get; set; }

    /// <summary>
    /// 完成结局
    /// </summary>
    [JsonPropertyName("has_ending_record")]
    public bool HasEndingRecord { get; set; }

    /// <summary>
    /// 结局名称
    /// </summary>
    [JsonPropertyName("ending_record_name")]
    public string EndingRecordName { get; set; }

    /// <summary>
    /// 结局图片
    /// </summary>
    [JsonPropertyName("ending_record_bg_pic")]
    public string EndingRecordBgPic { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [JsonPropertyName("total_score")]
    public int TotalScore { get; set; }

    /// <summary>
    /// 排名
    /// </summary>
    [JsonPropertyName("rank_percent")]
    public int RankPercent { get; set; }

    /// <summary>
    /// 最大分数
    /// </summary>
    [JsonPropertyName("max_score")]
    public int MaxScore { get; set; }

    /// <summary>
    /// 剩余时间
    /// </summary>
    [JsonPropertyName("left_ts")]
    public long LeftTs { get; set; }

    /// <summary>
    /// 结局
    /// </summary>
    [JsonPropertyName("ending_record_id")]
    public int EndingRecordId { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}