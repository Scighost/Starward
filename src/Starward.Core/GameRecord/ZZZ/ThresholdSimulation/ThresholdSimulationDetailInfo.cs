using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

public class ThresholdSimulationDetailInfo
{

    /// <summary>
    /// 主要信息
    /// </summary>
    [JsonPropertyName("void_front_battle_abstract_info_brief")]
    public ThresholdSimulationBrief Brief { get; set; }

    /// <summary>
    /// BOSS 节点
    /// </summary>
    [JsonPropertyName("boss_challenge_record")]
    public ThresholdSimulationBossChallengeRecord BossChallengeRecord { get; set; }

    /// <summary>
    /// BOSS 前节点
    /// </summary>
    [JsonPropertyName("main_challenge_record_list")]
    public List<ThresholdSimulationMainChallengeRecord> MainChallengeRecordList { get; set; }

    /// <summary>
    /// 账号信息
    /// </summary>
    [JsonPropertyName("role_basic_info")]
    public ThresholdSimulationRoleBasicInfo RoleBasicInfo { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



public class ThresholdSimulationAbstractInfo
{

    [JsonPropertyName("has_detail_record")]
    public bool HasDetailRecord { get; set; }


    [JsonPropertyName("void_front_battle_abstract_info_brief")]
    public ThresholdSimulationBrief Brief { get; set; }

}