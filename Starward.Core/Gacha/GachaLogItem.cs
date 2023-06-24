using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.Gacha;

public class GachaLogItem
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }


    [JsonPropertyName("gacha_type")]
    [JsonConverter(typeof(GachaTypeJsonConverter))]
    public GachaType GachaType { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("item_type")]
    public string ItemType { get; set; }


    [JsonPropertyName("rank_type")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int RankType { get; set; }


    [JsonPropertyName("time")]
    [JsonConverter(typeof(DateTimeJsonConverter))]
    public DateTime Time { get; set; }


    [JsonPropertyName("item_id")]
    [JsonConverter(typeof(ItemIdJsonConverter))]
    public int ItemId { get; set; }


    [JsonPropertyName("count")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Count { get; set; }


    [JsonPropertyName("lang")]
    public string Lang { get; set; }


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


internal class ItemIdJsonConverter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.Number)
        {
            return reader.GetInt32();
        }
        if ((reader.TokenType is JsonTokenType.String))
        {
            var str = reader.GetString();
            if (int.TryParse(str, out var id))
            {
                return id;
            }
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}



internal class GachaTypeJsonConverter : JsonConverter<GachaType>
{
    public override GachaType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var str = reader.GetString();
        if (int.TryParse(str, out int value))
        {
            return (GachaType)value;
        }
        else
        {
            return 0;
        }
    }

    public override void Write(Utf8JsonWriter writer, GachaType value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(((int)value).ToString());
    }
}