using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DeadlyAssault;

/// <summary>
/// 危局强袭战
/// </summary>
public class DeadlyAssaultInfo
{

    [JsonIgnore]
    public int Uid { get; set; }


    [JsonPropertyName("zone_id")]
    public int ZoneId { get; set; }

    /// <summary>
    /// 开始时间
    /// </summary>
    [JsonPropertyName("start_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime StartTime { get; set; }

    /// <summary>
    /// 结束时间
    /// </summary>
    [JsonPropertyName("end_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime EndTime { get; set; }

    /// <summary>
    /// 当前排名，以0.01%为单位
    /// </summary>
    [JsonPropertyName("rank_percent")]
    public int RankPercent { get; set; }

    [JsonPropertyName("list")]
    public List<DeadlyAssaultNode> AllNodes { get; set; }


    [JsonPropertyName("has_data")]
    public bool HasData { get; set; }

    /// <summary>
    /// 昵称
    /// </summary>
    [JsonPropertyName("nick_name")]
    public string NickName { get; set; }

    /// <summary>
    /// 头像
    /// </summary>
    [JsonPropertyName("avatar_icon")]
    public string AvatarIcon { get; set; }

    /// <summary>
    /// 总分
    /// </summary>
    [JsonPropertyName("total_score")]
    public int TotalScore { get; set; }

    /// <summary>
    /// 总星数
    /// </summary>
    [JsonPropertyName("total_star")]
    public int TotalStar { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


}
