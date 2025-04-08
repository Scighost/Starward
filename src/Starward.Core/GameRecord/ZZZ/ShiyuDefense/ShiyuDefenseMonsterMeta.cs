using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseMonsterMeta
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("weak_element_type")]
    public int WeakElementType { get; set; }

    [JsonPropertyName("ice_weakness")]
    public int IceWeakness { get; set; }

    [JsonPropertyName("fire_weakness")]
    public int FireWeakness { get; set; }

    [JsonPropertyName("elec_weakness")]
    public int ElecWeakness { get; set; }

    [JsonPropertyName("ether_weakness")]
    public int EtherWeakness { get; set; }

    [JsonPropertyName("physics_weakness")]
    public int PhysicsWeakness { get; set; }

    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    [JsonPropertyName("race_icon")]
    public string RaceIcon { get; set; }

    [JsonPropertyName("bg_icon")]
    public string BgIcon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



