using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.DailyNote;

public class GenshinDailyNote
{

    /// <summary>
    /// 当前树脂
    /// </summary>
    [JsonPropertyName("current_resin")]
    public int CurrentResin { get; set; }


    /// <summary>
    /// 最大树脂
    /// </summary>
    [JsonPropertyName("max_resin")]
    public int MaxResin { get; set; }


    /// <summary>
    /// 树脂恢复剩余时间，秒
    /// </summary>
    [JsonPropertyName("resin_recovery_time")]
    [JsonConverter(typeof(TimeSpanSecondStringJsonConverter))]
    public TimeSpan ResinRecoveryTime { get; set; }



    /// <summary>
    /// 委托完成数
    /// </summary>
    [JsonPropertyName("finished_task_num")]
    public int FinishedTaskNum { get; set; }



    /// <summary>
    /// 委托总数
    /// </summary>
    [JsonPropertyName("total_task_num")]
    public int TotalTaskNum { get; set; }


    /// <summary>
    /// 4次委托额外奖励是否领取
    /// </summary>
    [JsonPropertyName("is_extra_task_reward_received")]
    public bool IsExtraTaskRewardReceived { get; set; }


    /// <summary>
    /// 周本数值消耗减半剩余次数
    /// </summary>
    [JsonPropertyName("remain_resin_discount_num")]
    public int RemainResinDiscountNum { get; set; }


    /// <summary>
    /// 周本数值消耗减半次数上限
    /// </summary>
    [JsonPropertyName("resin_discount_num_limit")]
    public int ResinDiscountNumLimit { get; set; }


    /// <summary>
    /// 当前派遣数
    /// </summary>
    [JsonPropertyName("current_expedition_num")]
    public int CurrentExpeditionNum { get; set; }


    /// <summary>
    /// 最大派遣数
    /// </summary>
    [JsonPropertyName("max_expedition_num")]
    public int MaxExpeditionNum { get; set; }


    /// <summary>
    /// 派遣
    /// </summary>
    [JsonPropertyName("expeditions")]
    public List<GenshinExpedition> Expeditions { get; set; }


    /// <summary>
    /// 当前洞天宝钱
    /// </summary>
    [JsonPropertyName("current_home_coin")]
    public int CurrentHomeCoin { get; set; }


    /// <summary>
    /// 最大洞天宝钱
    /// </summary>
    [JsonPropertyName("max_home_coin")]
    public int MaxHomeCoin { get; set; }


    /// <summary>
    /// 洞天宝钱恢复时间的秒数
    /// </summary>
    [JsonPropertyName("home_coin_recovery_time")]
    [JsonConverter(typeof(TimeSpanSecondStringJsonConverter))]
    public TimeSpan HomeCoinRecoveryTime { get; set; }


    [JsonPropertyName("calendar_url")]
    public string CalendarUrl { get; set; }


    /// <summary>
    /// 参量质变仪
    /// </summary>
    [JsonPropertyName("transformer")]
    public Transformer Transformer { get; set; }


    /// <summary>
    /// 每日任务
    /// </summary>
    [JsonPropertyName("daily_task")]
    public DailyTask DailyTask { get; set; }


    /// <summary>
    /// 魔神任务进度
    /// </summary>
    [JsonPropertyName("archon_quest_progress")]
    public ArchonQuestProgress ArchonQuestProgress { get; set; }

}


