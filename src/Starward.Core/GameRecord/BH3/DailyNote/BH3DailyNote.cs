using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.BH3.DailyNote;

/// <summary>
/// 崩坏3实时便笺
/// </summary>
public class BH3DailyNote
{

    /// <summary>
    /// 当前体力
    /// </summary>
    [JsonPropertyName("current_stamina")]
    public int CurrentStamina { get; set; }

    /// <summary>
    /// 最大体力
    /// </summary>
    [JsonPropertyName("max_stamina")]
    public int MaxStamina { get; set; }

    /// <summary>
    /// 体力恢复时间，秒
    /// </summary>
    [JsonPropertyName("stamina_recover_time")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan StaminaRecoverTime { get; set; }

    /// <summary>
    /// 当前每日历练值
    /// </summary>
    [JsonPropertyName("current_train_score")]
    public int CurrentTrainScore { get; set; }

    /// <summary>
    /// 最大每日历练值
    /// </summary>
    [JsonPropertyName("max_train_score")]
    public int MaxTrainScore { get; set; }

    /// <summary>
    /// 量子流型
    /// </summary>
    [JsonPropertyName("greedy_endless")]
    public GreedyEndless GreedyEndless { get; set; }

    /// <summary>
    /// 超弦空间
    /// </summary>
    [JsonPropertyName("ultra_endless")]
    public UltraEndless UltraEndless { get; set; }

    /// <summary>
    /// 记忆战场
    /// </summary>
    [JsonPropertyName("battle_field")]
    public BattleField BattleField { get; set; }

    /// <summary>
    /// 往事乐土
    /// </summary>
    [JsonPropertyName("god_war")]
    public GodWar GodWar { get; set; }


}
