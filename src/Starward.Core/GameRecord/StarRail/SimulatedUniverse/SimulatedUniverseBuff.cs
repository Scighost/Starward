using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

public class SimulatedUniverseBuff
{
    [JsonPropertyName("base_type")]
    public SimulatedUniverseBuffType BuffType { get; set; }

    [JsonPropertyName("items")]
    public List<SimulatedUniverseBuffItem> Items { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


