using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakMobInfo
{
    [JsonPropertyName("maze_id")]
    public int MazeId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("monster_name")]
    public string MonsterName { get; set; }

    [JsonPropertyName("monster_icon")]
    public string MonsterIcon { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
