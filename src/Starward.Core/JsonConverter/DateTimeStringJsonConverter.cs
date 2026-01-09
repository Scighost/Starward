using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.JsonConverter;

internal class DateTimeStringJsonConverter : JsonConverter<DateTime>
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
            return default;
        }
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        if (value == default)
        {
            writer.WriteStringValue("");
        }
        else
        {
            writer.WriteStringValue(value.ToString("yyyy-MM-dd HH:mm:ss"));
        }
    }
}
