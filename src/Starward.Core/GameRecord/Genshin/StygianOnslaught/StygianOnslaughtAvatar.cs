using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtAvatar
{

    [JsonPropertyName("avatar_id")]
    public int AvatarId { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("element")]
    public string Element { get; set; }


    [JsonPropertyName("image")]
    public string Image { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }


    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }

    /// <summary>
    /// 命座
    /// </summary>
    [JsonPropertyName("rank")]
    public int Rank { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
