using Starward.Core.Gacha;
using System.Text.Json.Serialization;

namespace Starward.Core.SelfQuery;

public class GenshinQueryItem
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }

    [JsonPropertyName("add_num")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long AddNum { get; set; }

    [JsonPropertyName("reason")]
    public string Reason { get; set; }

    [JsonPropertyName("datetime")]
    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime DateTime { get; set; }

    [JsonPropertyName("type")]
    public GenshinQueryType Type { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("level")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Level { get; set; }

    [JsonPropertyName("quality")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Quality { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }


}
