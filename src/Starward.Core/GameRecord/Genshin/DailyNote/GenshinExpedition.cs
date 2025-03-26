using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.DailyNote;

/// <summary>
/// 原神探索派遣
/// </summary>
public class GenshinExpedition
{
    /// <summary>
    /// 角色侧面图
    /// </summary>
    [JsonPropertyName("avatar_side_icon")]
    public string AvatarSideIcon { get; set; }

    /// <summary>
    /// 状态 Ongoing:派遣中 Finished:已完成
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// 剩余时间，秒
    /// </summary>
    [JsonPropertyName("remained_time")]
    [JsonConverter(typeof(TimeSpanSecondStringJsonConverter))]
    public TimeSpan RemainedTime { get; set; }

}