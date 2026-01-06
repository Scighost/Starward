using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakMobRecord
{
    [JsonPropertyName("maze_id")]
    public int MazeId { get; set; }

    [JsonPropertyName("has_challenge_record")]
    public bool HasChallengeRecord { get; set; }

    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime ChallengeTime { get; set; }

    [JsonPropertyName("avatars")]
    public List<ChallengePeakAvatar> Avatars { get; set; }

    [JsonPropertyName("round_num")]
    public int RoundNum { get; set; }

    [JsonPropertyName("star_num")]
    public int StarNum { get; set; }

    [JsonPropertyName("is_fast")]
    public bool IsFast { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
