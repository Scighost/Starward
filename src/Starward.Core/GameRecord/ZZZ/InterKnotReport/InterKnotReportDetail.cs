using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.InterKnotReport;

public class InterKnotReportDetail : IJsonOnDeserialized
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


    [JsonPropertyName("data_month")]
    public string DataMonth { get; set; }


    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }


    [JsonPropertyName("list")]
    public List<InterKnotReportDetailItem> List { get; set; }

    /// <summary>
    /// 当月该类别记录总数
    /// </summary>
    [JsonPropertyName("total")]
    public int Total { get; set; }


    [JsonPropertyName("data_name")]
    public string DataName { get; set; }


    [JsonPropertyName("data_type")]
    public string DataType { get; set; }


    public void OnDeserialized()
    {
        foreach (var item in List)
        {
            item.Uid = this.Uid;
            item.DataMonth = this.DataMonth;
            item.DataType = this.DataType;
        }
    }

}
