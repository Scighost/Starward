using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class CloudGameBackground
{

    [JsonPropertyName("url")]
    public string Url { get; set; }

    /// <summary>
    /// always is 0
    /// </summary>
    [JsonPropertyName("size")]
    public string Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

}



internal class CloudGameBackgroundWrapper
{
    [JsonPropertyName("bg_image")]
    public CloudGameBackground BgImage { get; set; }
}
