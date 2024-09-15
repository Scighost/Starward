using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterSplendourBuffItem
{

    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonPropertyName("level")]
    public int Level { get; set; }

    /// <summary>
    /// 祝福不同等级的效果
    /// </summary>
    [JsonPropertyName("level_effect")]
    public List<ImaginariumTheaterSplendourBuffEffect> LevelEffect { get; set; }

}
