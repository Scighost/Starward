using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DailyNote;

/// <summary>
/// 绝区零实时便笺
/// </summary>
public class ZZZDailyNote
{

    /// <summary>
    /// 电量（体力）
    /// </summary>
    [JsonPropertyName("energy")]
    public Energy Energy { get; set; }

    /// <summary>
    /// 活跃度
    /// </summary>
    [JsonPropertyName("vitality")]
    public Vitality Vitality { get; set; }

    /// <summary>
    /// 录像店经营
    /// </summary>
    [JsonPropertyName("vhs_sale")]
    public VhsSale VhsSale { get; set; }

    /// <summary>
    /// 刮刮卡 <see cref="CardSignState"/>
    /// </summary>
    [JsonPropertyName("card_sign")]
    public string CardSign { get; set; }

    /// <summary>
    /// 刮刮卡未完成
    /// </summary>
    [JsonIgnore]
    public bool IsCardSignNo => CardSign is CardSignState.CardSignNo;

    /// <summary>
    /// 刮刮卡已完成
    /// </summary>
    [JsonIgnore]
    public bool IsCardSignDone => CardSign is CardSignState.CardSignDone;


    /// <summary>
    /// 每日任务已完成
    /// </summary>
    [JsonIgnore]
    public bool IsDailyMissionComplete => Vitality.Current >= Vitality.Max && IsCardSignDone && VhsSale.IsSaleStateDoing;


    /// <summary>
    /// 零号空洞悬赏委托
    /// </summary>
    [JsonPropertyName("bounty_commission")]
    public BountyCommission? BountyCommission { get; set; }

    /// <summary>
    /// null
    /// </summary>
    [JsonPropertyName("survey_points")]
    public object SurveyPoints { get; set; }

    /// <summary>
    /// 深渊刷新剩余时间，秒
    /// </summary>
    [JsonPropertyName("abyss_refresh")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan AbyssRefresh { get; set; }

    /// <summary>
    /// null
    /// </summary>
    [JsonPropertyName("coffee")]
    public object Coffee { get; set; }

    /// <summary>
    /// 丽都周纪积分
    /// </summary>
    [JsonPropertyName("weekly_task")]
    public WeeklyTask? WeeklyTask { get; set; }


}
