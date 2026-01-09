using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseV2FourthLayerChallengeInfo
{
    [JsonPropertyName("layer_id")]
    public int LayerId { get; set; }

    [JsonPropertyName("battle_time")]
    public int BattleTime { get; set; }

    [JsonPropertyName("avatar_list")]
    public List<ZZZAvatar> AvatarList { get; set; }

    [JsonPropertyName("buddy")]
    public ZZZBuddy Buddy { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
