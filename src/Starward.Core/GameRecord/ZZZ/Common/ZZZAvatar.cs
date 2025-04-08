using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.Common;

public class ZZZAvatar
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("rarity")]
    [JsonConverter(typeof(JsonStringEnumConverter<ZZZRarity>))]
    public ZZZRarity Rarity { get; set; }

    [JsonPropertyName("element_type")]
    public int ElementType { get; set; }

    [JsonPropertyName("avatar_profession")]
    public int Profession { get; set; }

    [JsonPropertyName("rank")]
    public int Rank { get; set; }

    [JsonPropertyName("role_square_url")]
    public string RoleSquareUrl { get; set; }

    [JsonPropertyName("sub_element_type")]
    public int SubElementType { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



