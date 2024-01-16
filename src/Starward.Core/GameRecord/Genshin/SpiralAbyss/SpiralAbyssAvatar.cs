using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.SpiralAbyss;

/// <summary>
/// 深境螺旋角色
/// </summary>
public class SpiralAbyssAvatar
{

    [JsonPropertyName("id")]
    public int AvatarId { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }


    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
