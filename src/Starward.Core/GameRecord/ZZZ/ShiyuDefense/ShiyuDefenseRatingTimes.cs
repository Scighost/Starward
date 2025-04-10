using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 式舆防卫战评分
/// </summary>
public class ShiyuDefenseRatingTimes
{

    /// <summary>
    /// 次数
    /// </summary>
    [JsonPropertyName("times")]
    public int Times { get; set; }

    /// <summary>
    /// 评分，SABC
    /// </summary>
    [JsonPropertyName("rating")]
    public string Rating { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


}
