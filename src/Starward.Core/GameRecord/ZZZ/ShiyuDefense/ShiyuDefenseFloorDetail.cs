using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

/// <summary>
/// 式舆防卫战防线
/// </summary>
public class ShiyuDefenseFloorDetail
{

    [JsonPropertyName("layer_id")]
    public int LayerId { get; set; }

    /// <summary>
    /// 第几防线
    /// </summary>
    [JsonPropertyName("layer_index")]
    public int LayerIndex { get; set; }

    /// <summary>
    /// 评分，SABC
    /// </summary>
    [JsonPropertyName("rating")]
    public string Rating { get; set; }

    /// <summary>
    /// buff
    /// </summary>
    [JsonPropertyName("buffs")]
    public List<ShiyuDefenseBuff> Buffs { get; set; }

    /// <summary>
    /// 第一队
    /// </summary>
    [JsonPropertyName("node_1")]
    public ShiyuDefenseNode Node1 { get; set; }

    /// <summary>
    /// 第二队
    /// </summary>
    [JsonPropertyName("node_2")]
    public ShiyuDefenseNode Node2 { get; set; }

    /// <summary>
    /// 战斗时间，秒
    /// </summary>
    [JsonIgnore]
    public int TotalBattleTime => Node1.BattleTime + Node2.BattleTime;

    /// <summary>
    /// 挑战时间
    /// </summary>
    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(TimestampStringJsonConverter))]
    public DateTimeOffset ChallengeTime { get; set; }

    /// <summary>
    /// 防线名称
    /// </summary>
    [JsonPropertyName("zone_name")]
    public string ZoneName { get; set; }

    /// <summary>
    /// 挑战时间
    /// </summary>
    [JsonPropertyName("floor_challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime FloorChallengeTime { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



