using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ForgottenHall;

public class ForgottenHallAvatar
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("rarity")]
    public int Rarity { get; set; }

    [JsonPropertyName("element")]
    public string Element { get; set; }
}

