using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

public class SimulatedUniverseBuffType
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("cnt")]
    public int Cnt { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


