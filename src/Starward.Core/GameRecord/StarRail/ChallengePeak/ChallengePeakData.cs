using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.ChallengePeak;

public class ChallengePeakData
{
    [JsonPropertyName("challenge_peak_records")]
    public List<ChallengePeakRecord> ChallengePeakRecords { get; set; }

    [JsonPropertyName("has_more_boss_record")]
    public bool HasMoreBossRecord { get; set; }

    [JsonPropertyName("challenge_peak_best_record_brief")]
    public ChallengePeakBestRecordBrief ChallengePeakBestRecordBrief { get; set; }

    [JsonPropertyName("role")]
    public ChallengePeakRole Role { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    [JsonIgnore]
    public long Uid { get; set; }

    [JsonIgnore]
    public int GroupId { get; set; }

    [JsonIgnore]
    public string GameVersion { get; set; }

    [JsonIgnore]
    public int BossStars { get; set; }

    [JsonIgnore]
    public int MobStars { get; set; }

    [JsonIgnore]
    public string BossIcon { get; set; }

}
