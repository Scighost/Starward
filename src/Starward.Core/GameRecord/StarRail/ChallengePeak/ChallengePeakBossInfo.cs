using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakBossInfo
{
    [JsonPropertyName("maze_id")]
    public int MazeId { get; set; }

    [JsonPropertyName("name_mi18n")]
    public string NameMi18n { get; set; }

    [JsonPropertyName("hard_mode_name_mi18n")]
    public string HardModeNameMi18n { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
