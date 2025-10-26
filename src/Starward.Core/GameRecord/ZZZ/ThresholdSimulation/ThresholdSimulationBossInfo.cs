using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

public class ThresholdSimulationBossInfo
{

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("race_icon")]
    public string RaceIco { get; set; }

    [JsonPropertyName("bg_icon")]
    public string BgIcon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
