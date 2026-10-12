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


    [JsonPropertyName("host")]
    public string Host { get; set; }


    public LauncherConfig(string id, int channel, int subChannel, string host)
    {
        Id = id;
        Channel = channel;
        SubChannel = subChannel;
        Host = host;
    }



    public static LauncherConfig ChinaOfficial { get; } = new("jGHBHlcOq1", 1, 1, "mihoyo");

    public static LauncherConfig GlobalOfficial { get; } = new("VYTpXlbWo8", 1, 0, "hoyoverse");

    public static LauncherConfig BilibiliGenshin { get; } = new("umfgRO5gh5", 14, 0, "mihoyo");

    public static LauncherConfig BilibiliStarRail { get; } = new("6P5gHMNyK3", 14, 0, "mihoyo");

    public static LauncherConfig BilibiliZZZ { get; } = new("xV0f4r1GT0", 14, 0, "mihoyo");


}
