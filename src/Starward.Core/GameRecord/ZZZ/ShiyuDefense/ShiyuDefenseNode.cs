using Starward.Core.GameRecord.ZZZ.Common;
using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseNode
{

    [JsonPropertyName("avatars")]
    public List<ZZZAvatar> Avatars { get; set; }

    [JsonPropertyName("buddy")]
    public ZZZBuddy Buddy { get; set; }

    [JsonPropertyName("element_type_list")]
    public List<int> ElementTypeList { get; set; }

    [JsonPropertyName("monster_info")]
    public ShiyuDefenseMonsterInfo MonsterInfo { get; set; }

    [JsonPropertyName("battle_time")]
    public int BattleTime { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



