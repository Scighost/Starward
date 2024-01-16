using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.SimulatedUniverse;

public class SimulatedUniverseMiracleItem
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}


