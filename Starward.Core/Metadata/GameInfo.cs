using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.Metadata;

public class GameInfo
{


    public string Name { get; set; }


    [JsonConverter(typeof(EnumStringJsonConverter<GameBiz>))]
    public GameBiz GameBiz { get; set; }


    public string Slogan { get; set; }


    public string Description { get; set; }


    public string HomePage { get; set; }


    public string Logo { get; set; }


    public string Poster { get; set; }


    public string HoYoSlogan { get; set; }


    public List<string> Fonts { get; set; }


}



internal class EnumStringJsonConverter<T> : JsonConverter<T> where T : struct
{
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (Enum.TryParse(reader.GetString(), out T result))
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
        writer.WriteStringValue(value.ToString());
    }
}