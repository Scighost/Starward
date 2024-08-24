using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.ImaginariumTheater;

public class ImaginariumTheaterLinks
{
    [JsonPropertyName("lineup_link")]
    public string LineupLink { get; set; }

    [JsonPropertyName("lineup_link_pc")]
    public string LineupLinkPc { get; set; }

    [JsonPropertyName("strategy_link")]
    public string StrategyLink { get; set; }

    [JsonPropertyName("lineup_publish_link")]
    public string LineupPublishLink { get; set; }

    [JsonPropertyName("lineup_publish_link_pc")]
    public string LineupPublishLinkPc { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}

