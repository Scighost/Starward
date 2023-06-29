using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.TravelersDiary;

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
        var year = DataMonth > CurrentMonth ? Date.Year - 1 : Date.Year;
        if (year < 2000)
        {
            var now = DateTime.Now;
            if (now.Month == CurrentMonth)
            {
                year = DataMonth > CurrentMonth ? now.Year - 1 : now.Year;
            }
            if (now.Month > CurrentMonth)
            {
                year = now.Year - 1;
            }
            if (now.Month < CurrentMonth)
            {
                year = DataMonth > CurrentMonth ? now.Year : now.Year + 1;
            }
        }
        MonthData.Uid = Uid;
        MonthData.Year = year;
        MonthData.Month = DataMonth;
    }

}
