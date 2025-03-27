using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.DailyNote;

/// <summary>
/// 星穹铁道实时便笺
/// </summary>
public class StarRailDailyNote
{

    /// <summary>
    /// 当前开拓力
    /// </summary>
    [JsonPropertyName("current_stamina")]
    public int CurrentStamina { get; set; }

    /// <summary>
    /// 最大开拓力
    /// </summary>
    [JsonPropertyName("max_stamina")]
    public int MaxStamina { get; set; }

    /// <summary>
    /// 开拓力已回满
    /// </summary>
    [JsonIgnore]
    public bool IsStaminaFull => CurrentStamina >= MaxStamina;

    /// <summary>
    /// 开拓力恢复时间，秒
    /// </summary>
    [JsonPropertyName("stamina_recover_time")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan StaminaRecoverTime { get; set; }

    /// <summary>
    /// 开拓力完全恢复的时间戳，秒
    /// </summary>
    [JsonPropertyName("stamina_full_ts")]
    [JsonConverter(typeof(TimestampNumberJsonConverter))]
    public DateTimeOffset StaminaFullTime { get; set; }


    /// <summary>
    /// 明天开拓力回满
    /// </summary>
    [JsonIgnore]
    public bool StaminaTomorrowRecovery => StaminaFullTime.LocalDateTime.Date > CurrentTime.LocalDateTime.Date;

    /// <summary>
    /// 已接取委托次数
    /// </summary>
    [JsonPropertyName("accepted_epedition_num")]
    public int AcceptedExpeditionNum { get; set; }

    /// <summary>
    /// 总委托次数
    /// </summary>
    [JsonPropertyName("total_expedition_num")]
    public int TotalExpeditionNum { get; set; }

    /// <summary>
    /// 委托列表
    /// </summary>
    [JsonPropertyName("expeditions")]
    public List<StarRailExpedition> Expeditions { get; set; }


    /// <summary>
    /// 有委托进行中
    /// </summary>
    [JsonIgnore]
    public bool HasExpeditionsNotFinished => AcceptedExpeditionNum > 0 && (Expeditions?.All(e => e.Status == "Ongoing") ?? false);

    /// <summary>
    /// 所有委托已完成
    /// </summary>
    [JsonIgnore]
    public bool ExpeditionsAllFinished => AcceptedExpeditionNum > 0 && (Expeditions?.All(e => e.Status == "Finished") ?? false);

    /// <summary>
    /// 所有委托已完成的时间
    /// </summary>
    [JsonIgnore]
    public DateTimeOffset ExpeditionsFinishTime => Expeditions?.OrderByDescending(x => x.FinishTime).FirstOrDefault()?.FinishTime ?? DateTimeOffset.MinValue;

    /// <summary>
    /// 当前训练分数（每日任务）
    /// </summary>
    [JsonPropertyName("current_train_score")]
    public int CurrentTrainScore { get; set; }

    /// <summary>
    /// 最大训练分数（每日任务）
    /// </summary>
    [JsonPropertyName("max_train_score")]
    public int MaxTrainScore { get; set; }

    /// <summary>
    /// 当前模拟/差分宇宙积分
    /// </summary>
    [JsonPropertyName("current_rogue_score")]
    public int CurrentRogueScore { get; set; }

    /// <summary>
    /// 模拟/差分宇宙每周最大积分
    /// </summary>
    [JsonPropertyName("max_rogue_score")]
    public int MaxRogueScore { get; set; }

    /// <summary>
    /// 历战余响剩余次数（周本）
    /// </summary>
    [JsonPropertyName("weekly_cocoon_cnt")]
    public int WeeklyCocoonCnt { get; set; }

    /// <summary>
    /// 历战余响次数上限（周本）
    /// </summary>
    [JsonPropertyName("weekly_cocoon_limit")]
    public int WeeklyCocoonLimit { get; set; }

    /// <summary>
    /// 后备开拓力
    /// </summary>
    [JsonPropertyName("current_reserve_stamina")]
    public int CurrentReserveStamina { get; set; }

    /// <summary>
    /// 后备开拓力已满
    /// </summary>
    [JsonPropertyName("is_reserve_stamina_full")]
    public bool IsReserveStaminaFull { get; set; }

    /// <summary>
    /// 差分宇宙已解锁
    /// </summary>
    [JsonPropertyName("rogue_tourn_weekly_unlocked")]
    public bool RogueTournWeeklyUnlocked { get; set; }

    /// <summary>
    /// 差分宇宙周期演算最大拟合值
    /// </summary>
    [JsonPropertyName("rogue_tourn_weekly_max")]
    public int RogueTournWeeklyMax { get; set; }

    /// <summary>
    /// 差分宇宙周期演算当前额外拟合值
    /// </summary>
    [JsonPropertyName("rogue_tourn_weekly_cur")]
    public int RogueTournWeeklyCur { get; set; }

    /// <summary>
    /// 当前时间戳
    /// </summary>
    [JsonPropertyName("current_ts")]
    [JsonConverter(typeof(TimestampNumberJsonConverter))]
    public DateTimeOffset CurrentTime { get; set; }

    /// <summary>
    /// 差分宇宙等级已满
    /// </summary>
    [JsonPropertyName("rogue_tourn_exp_is_full")]
    public bool RogueTournExpIsFull { get; set; }


}
