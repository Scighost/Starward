using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.ZZZ;

public class ZZZGachaWiki
{

    [JsonPropertyName("avatar")]
    public List<ZZZGachaInfo> Avatar { get; set; }


    [JsonPropertyName("weapon")]
    public List<ZZZGachaInfo> Weapon { get; set; }


    [JsonPropertyName("buddy")]
    public List<ZZZGachaInfo> Buddy { get; set; }

}
