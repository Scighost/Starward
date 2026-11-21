namespace Starward.Core.Hypergryph;

public sealed record HypergryphGameProfile(
    GameBiz GameBiz,
    string LauncherAppCode,
    string GameAppCode,
    string Channel,
    string SubChannel,
    string BatchProxyUrl,
    string WebBatchProxyUrl,
    string ExeName,
    string InstallationDirectory,
    string PublisherDirectory,
    string Icon,
    string Background,
    IReadOnlyList<string> RelatedProcesses);

public static class HypergryphGameConstants
{
    public const string ChinaBatchProxyUrl = "https://launcher.hypergryph.com/api/proxy/batch_proxy";

    public const string ChinaWebBatchProxyUrl = "https://launcher.hypergryph.com/api/proxy/web/batch_proxy";

    public const string GlobalBatchProxyUrl = "https://launcher.gryphline.com/api/proxy/batch_proxy";

    public const string GlobalWebBatchProxyUrl = "https://launcher.gryphline.com/api/proxy/web/batch_proxy";

    public const string LauncherAppCode = "abYeZZ16BPluCFyT";

    public const string EndfieldAppCode = "6LL0KJuqHBVz33WK";

    public const string ArknightsAppCode = "GzD1CpaWgmSq1wew";

    public const string GlobalLauncherAppCode = "TiaytKBUIEdoEwRT";

    public const string GlobalEndfieldAppCode = "YDUTE5gscDZ229CW";

    public const string EndfieldTargetApp = "EndField";

    public const string Channel = "1";

    public const string SubChannel = "1";

    public const string EndfieldExeName = "Endfield.exe";

    public const string ArknightsExeName = "Arknights.exe";

    public const string EndfieldInstallationDirectory = "Endfield Game";

    public const string ArknightsInstallationDirectory = "Arknights";

    public const string GlobalEndfieldInstallationDirectory = "Arknights Endfield";

    public const string EndfieldBackground = "ms-appx:///Assets/Image/background_endfield.jpg";

    public const string EndfieldIcon = "ms-appx:///Assets/Image/icon_endfield.png";

    public const string ArknightsBackground = "ms-appx:///Assets/Image/background_arknights.jpg";

    public const string ArknightsIcon = "ms-appx:///Assets/Image/icon_arknights.png";

    public static HypergryphGameProfile ChinaArknightsProfile { get; } = new(
        GameBiz.arknights_cn,
        LauncherAppCode,
        ArknightsAppCode,
        Channel,
        SubChannel,
        ChinaBatchProxyUrl,
        ChinaWebBatchProxyUrl,
        ArknightsExeName,
        ArknightsInstallationDirectory,
        "Hypergryph",
        ArknightsIcon,
        ArknightsBackground,
        [ArknightsExeName, "PlatformProcess.exe"]);

    public static HypergryphGameProfile ChinaEndfieldProfile { get; } = new(
        GameBiz.endfield_cn,
        LauncherAppCode,
        EndfieldAppCode,
        Channel,
        SubChannel,
        ChinaBatchProxyUrl,
        ChinaWebBatchProxyUrl,
        EndfieldExeName,
        EndfieldInstallationDirectory,
        "Hypergryph",
        EndfieldIcon,
        EndfieldBackground,
        [EndfieldExeName, "PlatformProcess.exe"]);

    public static HypergryphGameProfile GlobalEndfieldProfile { get; } = new(
        GameBiz.endfield_global,
        GlobalLauncherAppCode,
        GlobalEndfieldAppCode,
        "6",
        "6",
        GlobalBatchProxyUrl,
        GlobalWebBatchProxyUrl,
        EndfieldExeName,
        GlobalEndfieldInstallationDirectory,
        "GRYPHLINK",
        EndfieldIcon,
        EndfieldBackground,
        [EndfieldExeName, "PlatformProcess.exe"]);

    public static IReadOnlyList<HypergryphGameProfile> GameProfiles { get; } =
    [
        ChinaArknightsProfile,
        ChinaEndfieldProfile,
        GlobalEndfieldProfile,
    ];

    public static bool IsHypergryphGame(GameBiz gameBiz) =>
        GameProfiles.Any(x => x.GameBiz == gameBiz);

    public static bool IsEndfield(GameBiz gameBiz) =>
        gameBiz.Value is GameBiz.endfield_cn or GameBiz.endfield_global;

    public static bool IsArknights(GameBiz gameBiz) => gameBiz.Value is GameBiz.arknights_cn;

    public static HypergryphGameProfile GetGameProfile(GameBiz gameBiz) => gameBiz.Value switch
    {
        GameBiz.arknights_cn => ChinaArknightsProfile,
        GameBiz.endfield_cn => ChinaEndfieldProfile,
        GameBiz.endfield_global => GlobalEndfieldProfile,
        _ => throw new ArgumentOutOfRangeException(nameof(gameBiz), gameBiz.Value, "Unsupported Hypergryph game."),
    };
}
