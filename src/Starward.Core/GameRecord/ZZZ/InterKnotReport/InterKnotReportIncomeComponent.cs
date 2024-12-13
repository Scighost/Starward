using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.InterKnotReport;

public class InterKnotReportIncomeComponent
{

    [JsonPropertyName("action")]
    public string Action { get; set; }


    [JsonPropertyName("num")]
    public int Num { get; set; }

    /// <summary>
    /// 百分比，单位 %
    /// </summary>
    [JsonPropertyName("percent")]
    public int Percent { get; set; }

}


