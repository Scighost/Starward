using System.Text.Json.Serialization;

namespace Starward.Core.Hyperion.Genshin.TravelersDiary;

/// <summary>
/// 旅行札记总览
/// </summary>
public class TravelersDiarySummary : TravelersDiaryBase, IJsonOnDeserialized
{
    /// <summary>
    /// 查询月份的上个月
    /// </summary>
    [JsonPropertyName("data_last_month")]
    public int DataLastMonth { get; set; }

    /// <summary>
    /// 今日数据，查询月份不是当前月时，数据内容均为0
    /// </summary>
    [JsonPropertyName("day_data")]
    public TravelersDiaryDayData DayData { get; set; }

    /// <summary>
    /// 查询月数据
    /// </summary>
    [JsonPropertyName("month_data")]
    public TravelersDiaryMonthData MonthData { get; set; }

    /// <summary>
    /// 不知道是什么
    /// </summary>
    [JsonPropertyName("lantern")]
    public bool Lantern { get; set; }

    public void OnDeserialized()
    {
        var year = DataMonth > Date.Month ? Date.Year - 1 : Date.Year;
        MonthData.Uid = Uid;
        MonthData.Year = year;
        MonthData.Month = DataMonth;
        foreach (var item in MonthData.PrimogemsGroupBy)
        {
            item.Uid = Uid;
            item.Year = year;
            item.Month = DataMonth;
        }
    }

}
