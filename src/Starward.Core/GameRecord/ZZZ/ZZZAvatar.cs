using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ;

public class ZZZAvatar
{

    [JsonPropertyName("id")]
    public int Id { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// 稀有度 S A
    /// </summary>
    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }


    [JsonPropertyName("element_type")]
    public int ElementType { get; set; }


    [JsonPropertyName("avatar_profession")]
    public int Profession { get; set; }

    /// <summary>
    /// 影画
    /// </summary>
    [JsonPropertyName("rank")]
    public int Rank { get; set; }


    [JsonPropertyName("role_square_url")]
    public string RoleSquareUrl { get; set; }


    [JsonPropertyName("sub_element_type")]
    public int SubElementType { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



