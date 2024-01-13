using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ForgottenHall;

internal class ForgottenHallTime
{
    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("month")]
    public int Month { get; set; }

    [JsonPropertyName("day")]
    public int Day { get; set; }

    [JsonPropertyName("hour")]
    public int Hour { get; set; }

    [JsonPropertyName("minute")]
    public int Minute { get; set; }
}


internal class ForgottenHallTimeJsonConverter : JsonConverter<DateTime>
{

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (JsonSerializer.Deserialize(ref reader, typeof(ForgottenHallTime), GameRecordJsonContext.Default) is ForgottenHallTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, 0);
        }
        else
        {
            return DateTime.MinValue;
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value != DateTime.MinValue)
        {
            var time = new ForgottenHallTime
            {
                Year = value.Year,
                Month = value.Month,
                Day = value.Day,
                Hour = value.Hour,
                Minute = value.Minute,
            };
            writer.WriteRawValue(JsonSerializer.Serialize(time, typeof(ForgottenHallTime), GameRecordJsonContext.Default));
        }
    }

}

