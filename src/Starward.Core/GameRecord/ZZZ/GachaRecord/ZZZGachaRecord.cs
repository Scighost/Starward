using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.GachaRecord;

public class ZZZGachaRecordData
{

    [JsonPropertyName("gacha_item_list")]
    public List<ZZZGachaRecordItem> GachaItemList { get; set; } = new();


    [JsonPropertyName("has_more")]
    public bool HasMore { get; set; }

}


public class ZZZGachaRecordItem
{

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }


    [JsonPropertyName("item_type")]
    public string ItemType { get; set; } = "";


    [JsonPropertyName("item_id")]
    public int ItemId { get; set; }


    [JsonPropertyName("item_name")]
    public string ItemName { get; set; } = "";


    [JsonPropertyName("rarity")]
    public string Rarity { get; set; } = "";


    [JsonPropertyName("date")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime Date { get; set; }

}
