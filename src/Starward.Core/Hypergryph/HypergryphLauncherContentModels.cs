using System.Text.Json.Serialization;

namespace Starward.Core.Hypergryph;

public sealed class HypergryphLauncherContent
{
    public HypergryphMainBackground? MainBackground { get; set; }

    public List<HypergryphContentBanner> Banners { get; set; } = [];

    public List<HypergryphAnnouncementTab> AnnouncementTabs { get; set; } = [];
}

public sealed class HypergryphMainBackground
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("md5")]
    public string MD5 { get; set; } = "";

    [JsonPropertyName("video_url")]
    public string VideoUrl { get; set; } = "";
}

public sealed class HypergryphContentBanner
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("jump_url")]
    public string JumpUrl { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("need_token")]
    public bool NeedToken { get; set; }
}

public sealed class HypergryphAnnouncementTab
{
    [JsonPropertyName("tabName")]
    public string TabName { get; set; } = "";

    [JsonPropertyName("announcements")]
    public List<HypergryphAnnouncement> Announcements { get; set; } = [];

    [JsonPropertyName("tab_id")]
    public string TabId { get; set; } = "";
}

public sealed class HypergryphAnnouncement
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = "";

    [JsonPropertyName("jump_url")]
    public string JumpUrl { get; set; } = "";

    [JsonPropertyName("start_ts")]
    public string StartTimestamp { get; set; } = "";

    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    [JsonPropertyName("need_token")]
    public bool NeedToken { get; set; }

    [JsonPropertyName("pin")]
    public int Pin { get; set; }
}
