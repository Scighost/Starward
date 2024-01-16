using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

public class SimulatedUniverseBasicStats
{
    /// <summary>
    /// 祝福
    /// </summary>
    [JsonPropertyName("unlocked_buff_num")]
    public int UnlockedBuffNum { get; set; }

    /// <summary>
    /// 奇物
    /// </summary>
    [JsonPropertyName("unlocked_miracle_num")]
    public int UnlockedMiracleNum { get; set; }

    /// <summary>
    /// 技能树
    /// </summary>
    [JsonPropertyName("unlocked_skill_points")]
    public int UnlockedSkillPoints { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


