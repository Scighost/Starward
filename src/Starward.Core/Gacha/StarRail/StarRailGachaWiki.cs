using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.StarRail;

public class StarRailGachaWiki
{

    [JsonPropertyName("game")]
    public string Game { get; set; }

    [JsonIgnore]
    public string Language { get; set; }

    [JsonPropertyName("avatar")]
    public List<StarRailGachaInfo> Avatar { get; set; }

    [JsonPropertyName("equipment")]
    public List<StarRailGachaInfo> Equipment { get; set; }

}

