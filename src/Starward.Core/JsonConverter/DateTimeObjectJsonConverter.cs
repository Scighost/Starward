using Starward.Core.GameRecord;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.JsonConverter;


internal class DateTimeObjectJsonConverter : JsonConverter<DateTime>
{

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (JsonSerializer.Deserialize(ref reader, typeof(DateTimeObject), GameRecordJsonContext.Default) is DateTimeObject time)
        {
            return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second);
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
            var time = new DateTimeObject
            {
                Year = value.Year,
                Month = value.Month,
                Day = value.Day,
                Hour = value.Hour,
                Minute = value.Minute,
                Second = value.Second,
            };
            writer.WriteRawValue(JsonSerializer.Serialize(time, typeof(DateTimeObject), GameRecordJsonContext.Default));
        }
        else
        {
            writer.WriteNullValue();
        }
    }


    internal class DateTimeObject
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

}

