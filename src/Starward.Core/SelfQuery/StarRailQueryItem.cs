using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.SelfQuery;

public class StarRailQueryItem
{

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }


    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("type")]
    public StarRailQueryType Type { get; set; }


    [JsonPropertyName("action")]
    public string Action { get; set; }


    [JsonPropertyName("add_num")]
    public long AddNum { get; set; }


    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime Time { get; set; }


    [JsonPropertyName("relic_name")]
    public string RelicName { get; set; }


    [JsonPropertyName("relic_level")]
    public int RelicLevel { get; set; }


    [JsonPropertyName("relic_rarity")]
    public int RelicRarity { get; set; }


    [JsonPropertyName("equipment_name")]
    public string EquipmentName { get; set; }


    [JsonPropertyName("equipment_level")]
    public int EquipmentLevel { get; set; }


    [JsonPropertyName("equipment_rarity")]
    public int EquipmentRarity { get; set; }

}
