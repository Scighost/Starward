using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.TravelersDiary;

/// <summary>
/// 旅行札记每月统计
/// </summary>
public class TravelersDiaryMonthData
{

    public long Uid { get; set; }

    public int Year { get; set; }

    public int Month { get; set; }


    /// <summary>
    /// 查询月原石数
    /// </summary>
    [JsonPropertyName("current_primogems")]
    public int CurrentPrimogems { get; set; }

    /// <summary>
    /// 查询月摩拉数
    /// </summary>
    [JsonPropertyName("current_mora")]
    public int CurrentMora { get; set; }

    /// <summary>
    /// 查询月上一月原石数
    /// </summary>
    [JsonPropertyName("last_primogems")]
    public int LastPrimogems { get; set; }

    /// <summary>
    /// 查询月上一月摩拉数
    /// </summary>
    [JsonPropertyName("last_mora")]
    public int LastMora { get; set; }

    /// <summary>
    /// 不知道什么意思
    /// </summary>
    [JsonPropertyName("current_primogems_level")]
    public int CurrentPrimogemsLevel { get; set; }

    /// <summary>
    /// 相比于上一月原石的增长率，单位为百分数
    /// </summary>
    [JsonPropertyName("primogems_rate")]
    public int PrimogemsChangeRate { get; set; }

    /// <summary>
    /// 相比于上一月摩拉的增长率，单位为百分数
    /// </summary>
    [JsonPropertyName("mora_rate")]
    public int MoraChangeRate { get; set; }

    /// <summary>
    /// 原石获取来源分组统计
    /// </summary>
    [JsonPropertyName("group_by")]
    public List<TravelersDiaryPrimogemsMonthGroupStats> PrimogemsGroupBy { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


}
