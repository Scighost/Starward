using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.StarRail;

public class StarRailGachaItem : GachaLogItem
{

    [JsonIgnore]
    public int GachaId { get; set; }


    public override IGachaType GetGachaType() => new StarRailGachaType(GachaType);


}

public class StarRailGachaItemConverter : JsonConverter<StarRailGachaItem>
{
    public override StarRailGachaItem Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var jsonObject = JsonDocument.ParseValue(ref reader).RootElement;
        var gachaItem = (JsonSerializer.Deserialize<StarRailGachaItem>(jsonObject.GetRawText()) ?? new StarRailGachaItem());
        if (jsonObject.TryGetProperty("gacha_id", out var gachaIdProp))
            if (gachaIdProp.ValueKind == JsonValueKind.String && string.IsNullOrEmpty(gachaIdProp.GetString()))
                gachaItem.GachaId = gachaItem.GachaType switch
                {
                    1 => 1001,
                    2 => 4001,
                    11 => 2003,
                    12 => 3003,
                    _ => 1001,
                };
            else
                gachaItem.GachaId = gachaIdProp.GetInt32();

        return gachaItem;
    }

    public override void Write(Utf8JsonWriter writer, StarRailGachaItem value, JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}

