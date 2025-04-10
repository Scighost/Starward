using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ;

public class ZZZBuddy
{

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("rarity")]
    public string Rarity { get; set; }

    [JsonPropertyName("bangboo_rectangle_url")]
    public string RectangleUrl { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



