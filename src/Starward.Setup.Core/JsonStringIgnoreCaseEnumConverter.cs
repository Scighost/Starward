using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Setup.Core;

public class JsonStringIgnoreCaseEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? value = reader.GetString();
        if (Enum.TryParse(value, true, out T result))
        {
            return result;
        }
        else
        {
            return default;
        }
    }

    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString().ToLower());
    }
}