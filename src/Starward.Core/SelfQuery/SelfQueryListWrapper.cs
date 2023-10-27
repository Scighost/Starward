using System.Text.Json.Serialization;

namespace Starward.Core.SelfQuery;

internal class SelfQueryListWrapper<T>
{

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Size { get; set; }


    [JsonPropertyName("end_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long EndId { get; set; }


    [JsonPropertyName("page")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Page { get; set; }


    [JsonPropertyName("page_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int PageSize { get; set; }


    [JsonPropertyName("list")]
    public List<T> List { get; set; }

}
