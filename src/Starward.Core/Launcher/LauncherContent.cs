using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class LauncherContent
{

    [JsonPropertyName("content")]
    public ContentWrapper Content { get; set; }

}

public class ContentWrapper
{

    [JsonPropertyName("banners")]
    public List<LauncherBanner> Banner { get; set; }

    [JsonPropertyName("posts")]
    public List<LauncherPost> Post { get; set; }

}

public class LauncherBasicInfo
{

    [JsonPropertyName("game_info_list")]
    public List<BasicInfoWrapper> BasicInfo { get; set; }

}

public class BasicInfoWrapper
{

    [JsonPropertyName("backgrounds")]
    public List<BackgroundImageWrapper> Backgrounds { get; set; }

}
