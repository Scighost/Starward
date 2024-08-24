using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterAvatar
{

    [JsonPropertyName("avatar_id")]
    public int AvatarId { get; set; }

    /// <summary>
    /// 1 自己角色，2 试用角色，3 助演角色
    /// </summary>
    [JsonPropertyName("avatar_type")]
    public int AvatarType { get; set; }


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


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}

