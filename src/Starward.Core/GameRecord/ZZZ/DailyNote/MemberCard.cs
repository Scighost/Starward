using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DailyNote;

/// <summary>
/// 绳网会员
/// </summary>
public class MemberCard
{

    [JsonPropertyName("is_open")]
    public bool IsOpen { get; set; }

    /// <summary>
    /// 未领取 MemberCardStateNo
    /// 已领取 MemberCardStateACK
    /// </summary>
    [JsonPropertyName("member_card_state")]
    public string MemberCardState { get; set; }

    // seconds 未购买是 0
    [JsonPropertyName("exp_time")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public int ExpTime { get; set; }

    /// <summary>
    /// 剩余天数
    /// </summary>
    public int RemainingDays => Math.Clamp(ExpTime / 24 / 3600, 0, int.MaxValue);

    /// <summary>
    /// 菲林已领取
    /// </summary>
    public bool PolychromesClaimed => MemberCardState is "MemberCardStateACK";


}
