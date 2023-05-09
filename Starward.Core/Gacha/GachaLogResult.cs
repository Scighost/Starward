using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;

internal class GachaLogResult<T> where T : GachaLogItem
{

    [JsonPropertyName("page")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Page { get; set; }


    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Size { get; set; }


    [JsonPropertyName("list")]
    public List<T> List { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


}
