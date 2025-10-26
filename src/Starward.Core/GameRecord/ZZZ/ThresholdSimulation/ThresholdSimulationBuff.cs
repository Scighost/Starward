using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

/// <summary>
/// buff
/// </summary>
public class ThresholdSimulationBuff
{

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}
