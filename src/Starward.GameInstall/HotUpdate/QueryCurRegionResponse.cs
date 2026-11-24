using System.Text.Json.Serialization;

namespace Starward.GameInstall.HotUpdate;

internal class QueryCurRegionResponse
{
    [JsonPropertyName("content")]
    public string Content { get; set; }

    [JsonPropertyName("sign")]
    public string Sign { get; set; }
}
