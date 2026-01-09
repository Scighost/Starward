using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 通关概要
/// </summary>
public class ShiyuDefenseV2Brief
{
    [JsonPropertyName("cur_period_zone_layer_count")]
    public int CurPeriodZoneLayerCount { get; set; }

    /// <summary>
    /// 得分
    /// </summary>
    [JsonPropertyName("score")]
    public int Score { get; set; }

    /// <summary>
    /// 排名，例如 1234，意为 12.34%
    /// </summary>
    [JsonPropertyName("rank_percent")]
    public int RankPercent { get; set; }

    /// <summary>
    /// 通关总时长，秒
    /// </summary>
    [JsonPropertyName("battle_time")]
    public int BattleTime { get; set; }

    /// <summary>
    /// S A B
    /// </summary>
    [JsonPropertyName("rating")]
    public string Rating { get; set; }

    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime ChallengeTime { get; set; }

    /// <summary>
    /// 最高总分
    /// </summary>
    [JsonPropertyName("max_score")]
    public int MaxScore { get; set; }
}