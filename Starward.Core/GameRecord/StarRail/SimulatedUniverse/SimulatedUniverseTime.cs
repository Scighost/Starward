using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

internal class SimulatedUniverseTime
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

    [JsonPropertyName("second")]
    public int Second { get; set; }
}


internal class SimulatedUniverseTimeJsonConverter : JsonConverter<DateTime>
{

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (JsonSerializer.Deserialize(ref reader, typeof(SimulatedUniverseTime), GameRecordJsonContext.Default) is SimulatedUniverseTime time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
        }
        else
        {
            return new DateTime();
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        var time = new SimulatedUniverseTime
        {
            Year = value.Year,
            Month = value.Month,
            Day = value.Day,
            Hour = value.Hour,
            Minute = value.Minute,
            Second = value.Second,
        };
        writer.WriteRawValue(JsonSerializer.Serialize(time, typeof(SimulatedUniverseTime), GameRecordJsonContext.Default));
    }

}
