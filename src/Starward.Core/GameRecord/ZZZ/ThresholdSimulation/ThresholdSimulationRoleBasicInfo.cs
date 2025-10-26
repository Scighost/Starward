using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ThresholdSimulation;

public class ThresholdSimulationRoleBasicInfo
{

    [JsonPropertyName("server")]
    public string Server { get; set; }


    [JsonPropertyName("nickname")]
    public string NickName { get; set; }


    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}