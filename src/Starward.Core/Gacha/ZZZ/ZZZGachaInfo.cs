using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.ZZZ;

public class ZZZGachaInfo
{

    [JsonPropertyName("id")]
    public int Id { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }

}