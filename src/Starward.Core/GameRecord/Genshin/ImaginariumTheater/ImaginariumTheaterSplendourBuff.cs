using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterSplendourBuff
{

    [JsonPropertyName("summary")]
    public ImaginariumTheaterSplendourBuffSummary Summary { get; set; }


    [JsonPropertyName("buffs")]
    public List<ImaginariumTheaterSplendourBuffItem> Buffs { get; set; }

}
