using System.Globalization;
using Starward.Core.HoYoPlay;

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
    string Logo,
    IReadOnlyList<string> RelatedProcesses);

public static class HypergryphGameConstants
{
    public const string ChinaBatchProxyUrl = "https://launcher.hypergryph.com/api/proxy/batch_proxy";

    public const string ChinaWebBatchProxyUrl = "https://launcher.hypergryph.com/api/proxy/web/batch_proxy";

    public const string GlobalBatchProxyUrl = "https://launcher.gryphline.com/api/proxy/batch_proxy";

    public const string GlobalWebBatchProxyUrl = "https://launcher.gryphline.com/api/proxy/web/batch_proxy";

    public const string LauncherAppCode = "abYeZZ16BPluCFyT";

    public const string EndfieldAppCode = "6LL0KJuqHBVz33WK";

    private const string EndfieldChannelPrefix = "endfield.";

    public const string BilibiliEndfieldId = EndfieldChannelPrefix + EndfieldAppCode + ".2.2";

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

    public const string HypergryphLogo = "ms-appx:///Assets/Image/icon_hypergryph.png";

    public const string GryphlineLogo = "ms-appx:///Assets/Image/icon_gryphline.png";

    public const string BilibiliLogo = "ms-appx:///Assets/Image/gameicon_bilibili.png";

    private const string EndfieldLogo = "ms-appx:///Assets/Image/logo_endfield.svg";

    private const string GlobalEndfieldLogo = "ms-appx:///Assets/Image/logo_endfield_global.svg";

    public const string EndfieldIcon = "ms-appx:///Assets/Image/icon_endfield.png";

    private const string ArknightsLogo = "ms-appx:///Assets/Image/logo_arknights.svg";

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
        ArknightsLogo,
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
        EndfieldLogo,
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
        GlobalEndfieldLogo,
        [EndfieldExeName, "PlatformProcess.exe"]);

    public static bool IsHypergryphGame(GameBiz gameBiz) =>
        IsEndfield(gameBiz) || IsArknights(gameBiz);

    public static bool IsEndfield(GameBiz gameBiz) =>
        gameBiz.Value is GameBiz.endfield_cn or GameBiz.endfield_global or GameBiz.endfield_bilibili or GameBiz.endfield_channel;

    public static bool IsArknights(GameBiz gameBiz) => gameBiz.Value is GameBiz.arknights_cn;

    public static GameId? CreateEndfieldChannelGameId(string? appCode, int channel, int subChannel)
    {
        if (appCode is not { Length: 16 } || !appCode.All(char.IsAsciiLetterOrDigit)
            || channel < 0 || subChannel < 0)
        {
            return null;
        }
        if (appCode == EndfieldAppCode && channel == 1 && subChannel == 1)
        {
            return GameId.FromGameBiz(GameBiz.endfield_cn);
        }
        string id = FormattableString.Invariant($"{EndfieldChannelPrefix}{appCode}.{channel}.{subChannel}");
        return new GameId(id, id == BilibiliEndfieldId ? GameBiz.endfield_bilibili : GameBiz.endfield_channel);
    }

    public static GameId? GetEndfieldChannelGameId(string? id)
    {
        if (!TryGetEndfieldChannel(id, out string appCode, out string channel, out string subChannel))
        {
            return null;
        }
        return CreateEndfieldChannelGameId(appCode, int.Parse(channel, CultureInfo.InvariantCulture),
            int.Parse(subChannel, CultureInfo.InvariantCulture));
    }

    private static bool TryGetEndfieldChannel(string? id, out string appCode, out string channel, out string subChannel)
    {
        appCode = channel = subChannel = "";
        if (id is null || !id.StartsWith(EndfieldChannelPrefix, StringComparison.Ordinal))
        {
            return false;
        }
        string[] parts = id[EndfieldChannelPrefix.Length..].Split('.');
        if (parts is not [string code, string ch, string sub] || code.Length != 16 || !code.All(char.IsAsciiLetterOrDigit)
            || !int.TryParse(ch, NumberStyles.None, CultureInfo.InvariantCulture, out _)
            || !int.TryParse(sub, NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }
        (appCode, channel, subChannel) = (code, ch, sub);
        return true;
    }

    public static HypergryphGameProfile GetGameProfile(GameId gameId) => gameId.GameBiz.Value switch
    {
        GameBiz.arknights_cn => ChinaArknightsProfile,
        GameBiz.endfield_cn => ChinaEndfieldProfile,
        GameBiz.endfield_global => GlobalEndfieldProfile,
        GameBiz.endfield_bilibili or GameBiz.endfield_channel
            when TryGetEndfieldChannel(gameId.Id, out string appCode, out string channel, out string subChannel)
            => ChinaEndfieldProfile with
            {
                GameBiz = gameId.GameBiz,
                GameAppCode = appCode,
                Channel = channel,
                SubChannel = subChannel,
                InstallationDirectory = gameId.GameBiz.Value == GameBiz.endfield_bilibili
                    ? "Arknights Endfield bilibili" : $"{GlobalEndfieldInstallationDirectory} ({appCode}-{channel}-{subChannel})",
            },
        _ => throw new ArgumentOutOfRangeException(nameof(gameId), gameId, "Unsupported Hypergryph game."),
    };
}
