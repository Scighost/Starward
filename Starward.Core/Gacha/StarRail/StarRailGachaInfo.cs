using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.StarRail;

public class StarRailGachaInfo : IJsonOnDeserialized
{

    [JsonPropertyName("item_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int ItemId { get; set; }

    [JsonPropertyName("item_name")]
    public string ItemName { get; set; }

    [JsonPropertyName("icon_url")]
    public string IconUrl { get; set; }

    [JsonPropertyName("item_url")]
    public string ItemUrl { get; set; }

    [JsonPropertyName("damage_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int DamageType { get; set; }

    [JsonPropertyName("rarity")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Rarity { get; set; }

    [JsonPropertyName("avatar_base_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int AvatarBaseType { get; set; }

    [JsonPropertyName("wiki_url")]
    public string WikiUrl { get; set; }

    [JsonPropertyName("is_system")]
    public bool IsSystem { get; set; }


    public void OnDeserialized()
    {
        if (string.IsNullOrWhiteSpace(IconUrl))
        {
            IconUrl = ItemUrl;
        }
    }

}
