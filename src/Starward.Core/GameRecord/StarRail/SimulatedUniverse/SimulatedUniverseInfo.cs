using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

public class SimulatedUniverseInfo
{
    [JsonPropertyName("role")]
    public SimulatedUniverseGameRole Role { get; set; }

    [JsonPropertyName("basic_info")]
    public SimulatedUniverseBasicStats BasicInfo { get; set; }

    [JsonPropertyName("current_record")]
    public SimulatedUniverseRecord CurrentRecord { get; set; }

    [JsonPropertyName("last_record")]
    public SimulatedUniverseRecord LastRecord { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


