using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

public class ThresholdSimulationBossChallengeRecord
{

    /// <summary>
    /// BOSS 信息
    /// </summary>
    [JsonPropertyName("boss_info")]
    public ThresholdSimulationBossInfo BossInfo { get; set; }

    /// <summary>
    /// 挑战信息
    /// </summary>
    [JsonPropertyName("main_challenge_record")]
    public ThresholdSimulationMainChallengeRecord MainChallengeRecord { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
