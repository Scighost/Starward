using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.JsonConverter;

public class GameBizJsonConverter : JsonConverter<GameBiz>
{
    public override GameBiz Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new GameBiz(reader.GetString());
    }

    public override void Write(Utf8JsonWriter writer, GameBiz value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Value);
    }
}
