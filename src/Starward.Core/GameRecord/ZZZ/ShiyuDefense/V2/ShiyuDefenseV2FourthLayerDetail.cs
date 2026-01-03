using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseV2FourthLayerDetail
{
    [JsonPropertyName("buffer")]
    public ShiyuDefenseBuff Buff { get; set; }

    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime ChallengeTime { get; set; }

    [JsonPropertyName("rating")]
    public string Rating { get; set; }

    [JsonPropertyName("layer_challenge_info_list")]
    public List<ShiyuDefenseV2FourthLayerChallengeInfo> LayerChallenges { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
