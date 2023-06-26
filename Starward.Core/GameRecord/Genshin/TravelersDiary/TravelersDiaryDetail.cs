using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.TravelersDiary;

/// <summary>
/// 旅行札记收入记录详细信息
/// </summary>
public class TravelersDiaryDetail : TravelersDiaryBase, IJsonOnDeserialized
{

    [JsonPropertyName("page")]
    public int Page { get; set; }


    [JsonPropertyName("list")]
    public List<TravelersDiaryAwardItem> List { get; set; }


    /// <summary>
    /// 反序列化后<see cref="TravelersDiaryAwardItem.Type"/>没有赋值
    /// </summary>
    public void OnDeserialized()
    {
        if (!(List?.Any() ?? false))
        {
            return;
        }
        var year = List[0].Time.Year;
        var month = List[0].Time.Month;
        foreach (var item in List)
        {
            item.Uid = Uid;
            item.Year = year;
            item.Month = month;
        }
    }
}
