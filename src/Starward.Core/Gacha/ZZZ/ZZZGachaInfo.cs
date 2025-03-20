using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.ZZZ;

public class ZZZGachaInfo
{

    [JsonPropertyName("id")]
    public int Id { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }


    [JsonPropertyName("element_type")]
    public int ElementType { get; set; }


    [JsonPropertyName("profession")]
    public int Profession { get; set; }

}