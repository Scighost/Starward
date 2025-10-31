using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.Genshin;

public class GenshinBeyondGachaItem
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


    [JsonPropertyName("op_gacha_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int OpGachaType { get; set; }


    [JsonPropertyName("schedule_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int ScheduleId { get; set; }


    [JsonPropertyName("item_type")]
    public string ItemType { get; set; }


    [JsonPropertyName("item_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int ItemId { get; set; }


    [JsonPropertyName("item_name")]
    public string ItemName { get; set; }


    [JsonPropertyName("rank_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int RankType { get; set; }


    [JsonPropertyName("is_up")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int IsUp { get; set; }


    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeStringJsonConverter))]
    public DateTime Time { get; set; }

}
