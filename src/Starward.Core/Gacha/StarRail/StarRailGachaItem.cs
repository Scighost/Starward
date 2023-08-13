using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.StarRail;

public class StarRailGachaItem : GachaLogItem
{

    [JsonPropertyName("gacha_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int GachaId { get; set; }

}

