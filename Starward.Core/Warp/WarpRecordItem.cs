using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.Warp;

public class WarpRecordItem
{
    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Uid { get; set; }

    [JsonPropertyName("gacha_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int WarpId { get; set; }

    [JsonPropertyName("gacha_type")]
    [JsonConverter(typeof(WarpTypeJsonConverter))]
    public WarpType WarpType { get; set; }

    [JsonPropertyName("item_id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int ItemId { get; set; }

    [JsonPropertyName("count")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Count { get; set; }

    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime Time { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("lang")]
    public string Lang { get; set; }

    [JsonPropertyName("item_type")]
    public string ItemType { get; set; }

    [JsonPropertyName("rank_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int RankType { get; set; }

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }
}



internal class DateTimeJsonConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (DateTime.TryParse(str, out var time))
        {
            return time;
        }
        else
        {
            return DateTime.MinValue;
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
    }
}



internal class WarpTypeJsonConverter : JsonConverter<WarpType>
{
    public override WarpType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (int.TryParse(str, out var num))
        {
            return (WarpType)num;
        }
        else
        {
            return 0;
        }
    }

    public override void Write(Utf8JsonWriter writer, WarpType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(((int)value).ToString());
    }
}