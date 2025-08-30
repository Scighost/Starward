using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.StygianOnslaught;

public class StygianOnslaughtBest
{

    [JsonPropertyName("difficulty")]
    public int Difficulty { get; set; }


    [JsonPropertyName("second")]
    public int Seconds { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}