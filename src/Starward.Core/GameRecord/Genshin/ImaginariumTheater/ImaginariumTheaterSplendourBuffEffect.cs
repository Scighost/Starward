using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterSplendourBuffEffect
{

    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("desc")]
    public string Desc { get; set; }

}