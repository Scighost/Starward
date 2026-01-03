using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseV2FifthLayerChallengeInfo : ShiyuDefenseV2FourthLayerChallengeInfo
{
    [JsonPropertyName("rating")]
    public string Rating { get; set; }

    [JsonPropertyName("buffer")]
    public ShiyuDefenseBuff Buff { get; set; }

    [JsonPropertyName("score")]
    public int Score { get; set; }

    [JsonPropertyName("monster_pic")]
    public string MonsterPic { get; set; }

    [JsonPropertyName("max_score")]
    public int MaxScore { get; set; }
}