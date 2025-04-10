using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DeadlyAssault;

/// <summary>
/// 危局强袭战节点
/// </summary>
public class DeadlyAssaultNode
{
    /// <summary>
    /// 分数
    /// </summary>
    [JsonPropertyName("score")]
    public int Score { get; set; }

    /// <summary>
    /// 已获得星级
    /// </summary>
    [JsonPropertyName("star")]
    public int Star { get; set; }

    /// <summary>
    /// 最高星级
    /// </summary>
    [JsonPropertyName("total_star")]
    public int TotalStar { get; set; }

    /// <summary>
    /// 挑战时间
    /// </summary>
    [JsonPropertyName("challenge_time")]
    [JsonConverter(typeof(DateTimeObjectJsonConverter))]
    public DateTime ChallengeTime { get; set; }

    /// <summary>
    /// Boss
    /// </summary>
    [JsonPropertyName("boss")]
    public List<DeadlyAssaultBoss> Boss { get; set; }

    /// <summary>
    /// Buff
    /// </summary>
    [JsonPropertyName("buffer")]
    public List<DeadlyAssaultBuff> Buff { get; set; }

    /// <summary>
    /// 出战角色
    /// </summary>
    [JsonPropertyName("avatar_list")]
    public List<ZZZAvatar> Avatars { get; set; }

    /// <summary>
    /// 出战邦布
    /// </summary>
    [JsonPropertyName("buddy")]
    public ZZZBuddy Buddy { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}



