using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterSplendourBuffSummary
{

    [JsonPropertyName("total_level")]
    public int TotalLevel { get; set; }


    [JsonPropertyName("desc")]
    public string Desc { get; set; }

}
