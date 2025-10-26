using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

public class ThresholdSimulationSubChallengeRecord
{

    [JsonPropertyName("battle_id")]
    public int BattleId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// S A B C
    /// </summary>
    [JsonPropertyName("star")]
    public string Star { get; set; }

    [JsonPropertyName("avatar_list")]
    public List<ZZZAvatar> AvatarList { get; set; }

    [JsonPropertyName("buddy")]
    public ZZZBuddy Buddy { get; set; }

    [JsonPropertyName("buffer")]
    public ThresholdSimulationBuff Buff { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}