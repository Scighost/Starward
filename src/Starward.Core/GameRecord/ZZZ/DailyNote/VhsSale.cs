using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.DailyNote;

/// <summary>
/// 录像店经营
/// </summary>
public class VhsSale
{
    [JsonPropertyName("sale_state")]
    public string SaleState { get; set; }

    /// <summary>
    /// 等待营业
    /// </summary>
    [JsonIgnore]
    public bool IsSaleStateNo => SaleState is SaleStateNo;

    /// <summary>
    /// 正在经营
    /// </summary>
    [JsonIgnore]
    public bool IsSaleStateDoing => SaleState is SaleStateDoing;

    /// <summary>
    /// 待结算
    /// </summary>
    [JsonIgnore]
    public bool IsSaleStateDone => SaleState is SaleStateDone;


    /// <summary>
    /// 等待营业
    /// </summary>
    public const string SaleStateNo = nameof(SaleStateNo);

    /// <summary>
    /// 正在经营
    /// </summary>
    public const string SaleStateDoing = nameof(SaleStateDoing);

    /// <summary>
    /// 待结算
    /// </summary>
    public const string SaleStateDone = nameof(SaleStateDone);

}
