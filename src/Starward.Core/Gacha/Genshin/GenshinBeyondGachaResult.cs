using System.Text.Json.Serialization;

namespace Starward.Core.Gacha.Genshin;

internal class GenshinBeyondGachaResult
{

    [JsonPropertyName("total")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int Total { get; set; }



    [JsonPropertyName("list")]
    public List<GenshinBeyondGachaItem> List { get; set; }

}