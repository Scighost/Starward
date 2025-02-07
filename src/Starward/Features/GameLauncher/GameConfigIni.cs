using Microsoft.Extensions.Configuration;
using Starward.Core;
using System;

namespace Starward.Features.GameLauncher;

public class GameConfigIni
{

    [ConfigurationKeyName("channel")]
    public int Channel { get; set; }


    [ConfigurationKeyName("sub_channel")]
    public int SubChannel { get; set; }


    [ConfigurationKeyName("cps")]
    public string Cps { get; set; }


    [ConfigurationKeyName("game_version")]
    public Version? GameVersion { get; set; }


    [ConfigurationKeyName("sdk_version")]
    public Version? SdkVersion { get; set; }


    [ConfigurationKeyName("game_biz")]
    public GameBiz GameBiz { get; set; }


    [ConfigurationKeyName("uapc")]
    public int Uapc { get; set; }


    [ConfigurationKeyName("downloading_mode")]
    public string DownloadingMode { get; set; }

}