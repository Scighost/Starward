namespace Starward.Features.PlayTime;

/// <summary>
/// 统计卡片数据项
/// </summary>
public sealed class StatCardItem
{
    /// <summary>
    /// 标题
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// 主要数值
    /// </summary>
    public string Value { get; set; }

    /// <summary>
    /// 次要信息（如日期范围），为空时不显示
    /// </summary>
    public string SubText { get; set; }
}
