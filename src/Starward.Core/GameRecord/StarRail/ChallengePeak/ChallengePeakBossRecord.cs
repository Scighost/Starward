using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakBossRecord
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

    [JsonPropertyName("buff")]
    public ChallengePeakBuff Buff { get; set; }

    [JsonPropertyName("hard_mode")]
    public bool HardMode { get; set; }

    [JsonPropertyName("round_num")]
    public int RoundNum { get; set; }

    [JsonPropertyName("star_num")]
    public int StarNum { get; set; }

    [JsonPropertyName("finish_color_medal")]
    public bool FinishColorMedal { get; set; }

    [JsonPropertyName("challenge_peak_rank_icon_type")]
    public string ChallengePeakRankIconType { get; set; }

    [JsonPropertyName("challenge_peak_rank_icon")]
    public string ChallengePeakRankIcon { get; set; }

    [JsonPropertyName("record_unique_key")]
    public string RecordUniqueKey { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }
}
