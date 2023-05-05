using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;

public class GachaLogResult
{

    [JsonPropertyName("page")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Page { get; set; }


    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Size { get; set; }


    [JsonPropertyName("list")]
    public List<GachaLogItem> List { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


    [JsonPropertyName("region_time_zone")]
    public int RegionTimeZone { get; set; }


}




