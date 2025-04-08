using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DeadlyAssault;

public class DeadlyAssaultInfo
{

    [JsonIgnore]
    public int Uid { get; set; }

    [JsonPropertyName("start_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime StartTime { get; set; }

    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime EndTime { get; set; }

    [JsonPropertyName("rank_percent")]
    [JsonConverter(typeof(PercentIntJsonConverter))]
    public string RankPercent { get; set; }

    [JsonPropertyName("list")]
    public List<DeadlyAssaultNode> AllNodes { get; set; }

    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }

    [JsonPropertyName("nick_name")]
    public string NickName { get; set; }

    [JsonPropertyName("avatar_icon")]
    public string AvatarIcon { get; set; }

    [JsonPropertyName("total_score")]
    public int TotalScore { get; set; }

    [JsonPropertyName("total_star")]
    public int TotalStar { get; set; }

    [JsonPropertyName("zone_id")]
    public int ZoneId { get; set; }


}
