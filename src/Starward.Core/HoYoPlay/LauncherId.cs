namespace Starward.Core.HoYoPlay;


/// <summary>
/// 启动器 ID
/// </summary>
public abstract class LauncherId
{

    public const string ChinaOfficial = "jGHBHlcOq1";

    public const string GlobalOfficial = "VYTpXlbWo8";

    public const string BilibiliGenshin = "umfgRO5gh5";

    public const string BilibiliStarRail = "6P5gHMNyK3";


    public static bool IsChinaOfficial(string launcherId)
    {
        return launcherId is ChinaOfficial;
    }


    public static bool IsGlobalOfficial(string launcherId)
    {
        return launcherId is GlobalOfficial;
    }


    public static bool IsBilibili(string launcherId)
    {
        return launcherId is BilibiliGenshin or BilibiliStarRail;
    }

}
