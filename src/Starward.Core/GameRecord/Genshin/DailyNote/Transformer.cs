using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.DailyNote;

/// <summary>
/// 参量质变仪
/// </summary>
public class Transformer
{

    /// <summary>
    /// 是否获得
    /// </summary>
    [JsonPropertyName("obtained")]
    public bool Obtained { get; set; }

    /// <summary>
    /// 剩余时间
    /// </summary>
    [JsonPropertyName("recovery_time")]
    public TransformerRecoveryTime RecoveryTime { get; set; }

    /// <summary>
    /// Wiki url
    /// </summary>
    [JsonPropertyName("wiki")]
    public string Wiki { get; set; }

    [JsonPropertyName("noticed")]
    public bool Noticed { get; set; }

    [JsonPropertyName("latest_job_id")]
    public string LatestJobId { get; set; }

}


/// <summary>
/// 参量质变仪恢复时间
/// <para>仅有 <see cref="Day"/> 或 <see cref="Hour"/> 有值</para>
/// </summary>
public class TransformerRecoveryTime
{
    [JsonPropertyName("Day")]
    public int Day { get; set; }

    [JsonPropertyName("Hour")]
    public int Hour { get; set; }

    [JsonPropertyName("Minute")]
    public int Minute { get; set; }

    [JsonPropertyName("Second")]
    public int Second { get; set; }

    /// <summary>
    /// 是否可再次使用
    /// </summary>
    [JsonPropertyName("reached")]
    public bool Reached { get; set; }
}

