using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

/// <summary>
/// 主要挑战节点
/// </summary>
public class ThresholdSimulationMainChallengeRecord
{

    [JsonPropertyName("battle_id")]
    public int BattleId { get; set; }


    [JsonPropertyName("node_id")]
    public int NodeId { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("score")]
    public int Score { get; set; }

    /// <summary>
    /// S A B C
    /// </summary>
    [JsonPropertyName("star")]
    public string Star { get; set; }

    /// <summary>
    /// 得分倍，如 1.2
    /// </summary>
    [JsonPropertyName("score_ratio")]
    public string ScoreRatio { get; set; }


    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime ChallengeTime { get; set; }


    [JsonPropertyName("buffer")]
    public ThresholdSimulationBuff Buff { get; set; }

    /// <summary>
    /// 最高分数
    /// </summary>
    [JsonPropertyName("max_score")]
    public int MaxScore { get; set; }


    [JsonPropertyName("avatar_list")]
    public List<ZZZAvatar> AvatarList { get; set; }


    [JsonPropertyName("buddy")]
    public ZZZBuddy Buddy { get; set; }


    [JsonPropertyName("sub_challenge_record")]
    public List<ThresholdSimulationSubChallengeRecord> SubChallengeRecords { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
