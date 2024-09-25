using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.InterKnotReport;

public class InterKnotReportDetailItem
{

    [JsonIgnore]
    public long Uid { get; set; }

    [JsonPropertyName("id")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Id { get; set; }

    [JsonIgnore]
    public string DataMonth { get; set; }

    [JsonIgnore]
    public string DataType { get; set; }


    [JsonPropertyName("action")]
    public string Action { get; set; }


    [JsonPropertyName("time")]
    [JsonConverter(typeof(SpiralAbyssTimeJsonConverter))]
    public DateTimeOffset Time { get; set; }


    [JsonPropertyName("num")]
    public int Number { get; set; }

}
