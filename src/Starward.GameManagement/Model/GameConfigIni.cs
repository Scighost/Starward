using Microsoft.Extensions.Configuration;

namespace Starward.GameManagement.Model;

public class GameConfigIni
{
    [ConfigurationKeyName("channel")]
    public int Channel { get; set; }

    [ConfigurationKeyName("sub_channel")]
    public int SubChannel { get; set; }

    [ConfigurationKeyName("cps")]
    public string Cps { get; set; }

    [ConfigurationKeyName("game_biz")]
    public string GameBiz { get; set; }

    [ConfigurationKeyName("game_version")]
    public Version? GameVersion { get; set; }

    [ConfigurationKeyName("sdk_version")]
    public string SdkVersion { get; set; }

    [ConfigurationKeyName("wpf_version")]
    public string WpfVersion { get; set; }

    [ConfigurationKeyName("plugin_version")]
    public Version? PluginVersion { get; set; }

    [ConfigurationKeyName("uapc")]
    public string Uapc { get; set; }

    [ConfigurationKeyName("downloading_mode")]
    public string DownloadingMode { get; set; }

    public Dictionary<string, string> Settings { get; set; } = new();
}