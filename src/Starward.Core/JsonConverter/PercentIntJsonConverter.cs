using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.JsonConverter;

internal class PercentIntJsonConverter : JsonConverter<string>
{
    public override string Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return $"{(reader.GetInt32() / 10000.0):0.00%}";
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        if (value.EndsWith("%") && double.TryParse(value.TrimEnd('%'), out double doubleValue))
        {
            writer.WriteNumberValue((int)(doubleValue * 100));
        }
        else
        {
            throw new JsonException("Invalid percentage format.");
        }
    }
}