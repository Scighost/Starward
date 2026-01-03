using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseV2FifthLayerDetail
{
    [JsonPropertyName("layer_challenge_info_list")]
    public List<ShiyuDefenseV2FifthLayerChallengeInfo> LayerChallenges { get; set; }
}