using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.Genshin;

public class GenshinGachaWiki
{

    [JsonPropertyName("game")]
    public string Game { get; set; }

    [JsonIgnore]
    public string Language { get; set; }

    [JsonPropertyName("all_avatar")]
    public List<GenshinGachaInfo> AllAvatar { get; set; }

    [JsonPropertyName("all_weapon")]
    public List<GenshinGachaInfo> AllWeapon { get; set; }

}