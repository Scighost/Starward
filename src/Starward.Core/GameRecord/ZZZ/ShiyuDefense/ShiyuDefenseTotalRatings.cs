using System.Text.Json.Serialization;
using Starward.Core.GameRecord.ZZZ.Common;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseTotalRatings
{

    [JsonPropertyName("times")]
    public int Times { get; set; }

    [JsonPropertyName("rating")]
    [JsonConverter(typeof(JsonStringEnumConverter<ZZZRating>))]
    public ZZZRating Rating { get; set; }


    [JsonExtensionData]
    public Dictionary<string, object>? ExtensionData { get; set; }


}
