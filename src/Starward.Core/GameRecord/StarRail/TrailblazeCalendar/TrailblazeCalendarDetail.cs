using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.StarRail.TrailblazeCalendar;

/// <summary>
/// 开拓月历明细
/// </summary>
public class TrailblazeCalendarDetail : IJsonOnDeserialized
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }

    [JsonPropertyName("region")]
    public string Region { get; set; }

    /// <summary>
    /// 202304
    /// </summary>
    [JsonPropertyName("data_month")]
    public string DataMonth { get; set; }


    [JsonPropertyName("current_page")]
    public int CurrentPage { get; set; }


    [JsonPropertyName("list")]
    public List<TrailblazeCalendarDetailItem> List { get; set; }


    [JsonPropertyName("total")]
    public int Total { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


    public void OnDeserialized()
    {
        foreach (var item in List)
        {
            item.Uid = Uid;
            item.Month = DataMonth;
        }
    }
}

