using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DailyNote;

/// <summary>
/// 零号空洞悬赏委托
/// </summary>
public class BountyCommission
{
    /// <summary>
    /// 已完成
    /// </summary>
    [JsonPropertyName("num")]
    public int Num { get; set; }

    /// <summary>
    /// 总数
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }

    /// <summary>
    /// 剩余刷新时间，秒
    /// </summary>
    [JsonPropertyName("refresh_time")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan RefreshTime { get; set; }
}
