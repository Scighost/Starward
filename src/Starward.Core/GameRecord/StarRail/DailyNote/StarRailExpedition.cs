using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.DailyNote;

/// <summary>
/// 星穹铁道委托
/// </summary>
public class StarRailExpedition
{

    /// <summary>
    /// 委托角色头像 url
    /// </summary>
    [JsonPropertyName("avatars")]
    public List<string> Avatars { get; set; }

    /// <summary>
    /// 状态 Ongoing:派遣中 Finished:已完成
    /// </summary>
    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// 剩余时间，秒
    /// </summary>
    [JsonPropertyName("remaining_time")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan RemainingTime { get; set; }

    /// <summary>
    /// 委托名称
    /// </summary>
    [JsonPropertyName("name")]
    public string Name { get; set; }

    /// <summary>
    /// 委托奖励图标 url
    /// </summary>
    [JsonPropertyName("item_url")]
    public string ItemUrl { get; set; }

    /// <summary>
    /// 委托完成时间戳，秒
    /// </summary>
    [JsonPropertyName("finish_ts")]
    [JsonConverter(typeof(TimestampNumberJsonConverter))]
    public DateTimeOffset FinishTime { get; set; }

}