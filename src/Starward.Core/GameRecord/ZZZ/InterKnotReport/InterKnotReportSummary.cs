using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.InterKnotReport;

public class InterKnotReportSummaryAward
{
    [JsonPropertyName("data_type")]
    public string DataType { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }

    [JsonPropertyName("data_name")]
    public string DataName { get; set; }
}

public class InterKnotReportSummary
{

    [JsonPropertyName("uid")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Uid { get; set; }


    [JsonPropertyName("region")]
    public string Region { get; set; }


    [JsonPropertyName("current_month")]
    public string CurrentMonth { get; set; }


    [JsonPropertyName("data_month")]
    public string DataMonth { get; set; }


    [JsonPropertyName("month_data")]
    public InterKnotReportMonthData MonthData { get; set; }


    [JsonPropertyName("optional_month")]
    public List<string> OptionalMonth { get; set; }


    [JsonPropertyName("role_info")]
    public InterKnotReportRoleInfo RoleInfo { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }

}


