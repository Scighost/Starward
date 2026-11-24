using System.Text.Json.Serialization;

namespace Starward.Core.HoYoPlay;

public record LauncherConfig
{

    [JsonPropertyName("id")]
    public string Id { get; set; }


    [JsonPropertyName("channel")]
    public int Channel { get; set; }


    [JsonPropertyName("sub_channel")]
    public int SubChannel { get; set; }


    [JsonPropertyName("region")]
    public ServerRegion Region { get; set; }


    [JsonPropertyName("is_beta")]
    public bool IsBeta { get; set; }


    public LauncherConfig(string id, int channel, int subChannel, ServerRegion region, bool isBeta = false)
    {
        Id = id;
        Channel = channel;
        SubChannel = subChannel;
        Region = region;
        IsBeta = isBeta;
    }

}
