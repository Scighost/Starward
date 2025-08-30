using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

/// <summary>
/// 赋光之人
/// </summary>
public class StygianOnslaughtBlingAvatar
{

    [JsonPropertyName("avatar_id")]
    public int AvatarId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("element")]
    public string Element { get; set; }

    [JsonPropertyName("image")]
    public string Image { get; set; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }

    /// <summary>
    /// 最终是否上榜
    /// </summary>
    [JsonPropertyName("is_plus")]
    public bool IsPlus { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}