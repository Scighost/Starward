using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.ZZZ.ShiyuDefense;

public class ShiyuDefenseWrapper
{

    [JsonPropertyName("hadal_ver")]
    public string HadalVer { get; set; }

    [JsonPropertyName("hadal_info_v1")]
    public ShiyuDefenseInfo? InfoV1 { get; set; }

    [JsonPropertyName("hadal_info_v2")]
    public ShiyuDefenseInfoV2? InfoV2 { get; set; }

    [JsonPropertyName("nick_name")]
    public string NickName { get; set; }

    [JsonPropertyName("icon")]
    public string Icon { get; set; }

}