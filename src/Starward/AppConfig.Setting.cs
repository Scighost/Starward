using Dapper;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.Database;
using Starward.Features.GameLauncher;
using Starward.Features.ViewHost;
using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Starward;

public static partial class AppConfig
{


    #region Static Setting


    public static string? LastAppVersion
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static bool EnablePreviewRelease
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static string? IgnoreVersion
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 更新完成后自动重启
    /// </summary>
    public static bool AutoRestartWhenUpdateFinished
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    /// <summary>
    /// 更新完成后显示更新内容
    /// </summary>
    public static bool ShowUpdateContentAfterUpdateRestart
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    /// <summary>
    /// 主窗口关闭选项，隐藏/退出
    /// </summary>
    public static MainWindowCloseOption CloseWindowOption
    {
        get => GetValue<MainWindowCloseOption>();
        set => SetValue(value);
    }

    public static string? EnabledLauncherIds
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static string? ExtraLauncherConfigModel
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? CurrentGameId
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static bool ShownFindGamesTip
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool EnableBannerAndPost
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static string? GachaLanguage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? AccentColor
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static int VideoBgVolume
    {
        get => Math.Clamp(GetValue(0), 0, 100);
        set => SetValue(value);
    }

    public static bool AcceptHoyolabToolboxAgreement
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool HoyolabToolboxPaneOpen
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static bool DisableGameNoticeRedHot
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static StartGameAction StartGameAction
    {
        get => GetValue<StartGameAction>();
        set => SetValue(value);
    }

    public static int SpeedLimitKBPerSecond
    {
        get => GetValue(0);
        set => SetValue(value);
    }

    /// <summary>
    /// 崩坏3国际服多区服选项
    /// </summary>
    public static string? LastGameIdOfBH3Global
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 启用硬链接
    /// </summary>
    public static bool EnableHardLink
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    /// <summary>
    /// 原神HDR
    /// </summary>
    public static bool EnableGenshinHDR
    {
        get => GetValue(false);
        set => SetValue(value);
    }

    /// <summary>
    /// 截图文件夹
    /// </summary>
    public static string? ScreenshotFolder
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 显示主窗口快捷键
    /// </summary>
    public static string? ShowMainWindowHotkey
    {
        // Alt + S
        get => GetValue("1+83");
        set => SetValue(value);
    }

    /// <summary>
    /// 截图快捷键
    /// </summary>
    public static string? ScreenshotCaptureHotkey
    {
        // Alt + D
        get => GetValue("1+68");
        set => SetValue(value);
    }

    /// <summary>
    /// 手柄控制
    /// </summary>
    public static bool EnableGamepadSimulateInput
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static int GamepadGuideButtonMode
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public static string? GamepadShareButtonMapKeys
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? GamepadGuideButtonMapKeys
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static int GamepadShareButtonMode
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public static bool AutoConvertScreenshotToSDR
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static bool AutoCopyScreenshotToClipboard
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    /// <summary>
    /// 截图链路色彩管理（HDR 模式始终启用）
    /// </summary>
    public static bool EnableScreenshotColorManagement
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    /// <summary>
    /// 0: PNG, 1: AVIF, 2: JPEG XL
    /// </summary>
    public static int ScreenCaptureSavedFormat
    {
        get => GetValue(0);
        set => SetValue(value);
    }

    /// <summary>
    /// 0: Middle, 1: High, 2: Lossless
    /// </summary>
    public static int ScreenCaptureEncodeQuality
    {
        get => GetValue(1);
        set => SetValue(value);
    }

    public static bool EnableGamepadController
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 使用 CMD 启动游戏 <see href="https://github.com/Scighost/Starward/issues/1634"/>
    /// </summary>
    public static bool StartGameWithCMD
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool AuthLoginGameByPassportAccount
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static int PlayTimeDayStartHour
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public static long LastUgcGachaUid
    {
        get => GetValue<long>();
        set => SetValue(value);
    }

    public static string? AndroidDeviceInfo
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    #endregion



    #region Dynamic Setting


    public static long GetLastPassportUserAid(ServerRegion region)
    {
        return GetValue<long>(default, "LastPassportUserAid", region.ToString());
    }

    public static void SetLastPassportUserAid(ServerRegion region, long value)
    {
        SetValue(value, "LastPassportUserAid", region.ToString());
    }


    public static string? GetGameBackground(GameId gameId)
    {
        return GetValue<string>(default, "GameBackground", gameId.ToString());
    }

    public static void SetGameBackground(GameId gameId, string? value)
    {
        SetValue(value, "GameBackground", gameId.ToString());
    }


    public static string? GetBg(GameId gameId)
    {
        return GetValue<string>(default, "Bg", gameId.ToString());
    }

    public static void SetBg(GameId gameId, string? value)
    {
        SetValue(value, "Bg", gameId.ToString());
    }


    public static string? GetCustomBg(GameId gameId)
    {
        return GetValue<string>(default, "CustomBg", gameId.ToString());
    }

    public static void SetCustomBg(GameId gameId, string? value)
    {
        SetValue(value, "CustomBg", gameId.ToString());
    }


    public static bool GetEnableThirdPartyTool(GameId gameId)
    {
        return GetValue<bool>(default, "EnableThirdPartyTool", gameId.ToString());
    }

    public static void SetEnableThirdPartyTool(GameId gameId, bool value)
    {
        SetValue(value, "EnableThirdPartyTool", gameId.ToString());
    }


    public static string? GetThirdPartyToolPath(GameId gameId)
    {
        return GetValue<string>(default, "ThirdPartyToolPath", gameId.ToString());
    }

    public static void SetThirdPartyToolPath(GameId gameId, string? value)
    {
        SetValue(value, "ThirdPartyToolPath", gameId.ToString());
    }


    public static string? GetStartArgument(GameId gameId)
    {
        return GetValue<string>(default, "StartArgument", gameId.ToString());
    }

    public static void SetStartArgument(GameId gameId, string? value)
    {
        SetValue(value, "StartArgument", gameId.ToString());
    }


    public static long GetLastUidInGachaLogPage(GameBiz game)
    {
        return GetValue<long>(default, "LastGachaUid", game.Game);
    }

    public static void SetLastUidInGachaLogPage(GameBiz game, long value)
    {
        SetValue(value, "LastGachaUid", game.Game);
    }


    public static string? GetDisplayGachaBanners(GameBiz game)
    {
        return GetValue<string>(default, "DisplayGachaBanners", game.Game);
    }

    public static void SetDisplayGachaBanners(GameBiz game, string value)
    {
        SetValue(value, "DisplayGachaBanners", game.Game);
    }


    /// <summary>
    /// 外部截图文件夹
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static string? GetExternalScreenshotFolder(GameId gameId)
    {
        return GetValue<string>(default, "ExternalScreenshotFolder", gameId.ToString());
    }

    /// <summary>
    /// 外部截图文件夹
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="value"></param>
    public static void SetExternalScreenshotFolder(GameId gameId, string? value)
    {
        SetValue(value, "ExternalScreenshotFolder", gameId.ToString());
    }


    /// <summary>
    /// 启用 DX12
    /// </summary>
    public static bool? GetEnableDX12(GameId gameId)
    {
        return GetValue<bool>(default, "EnableDX12", gameId.ToString());
    }

    /// <summary>
    /// 启用 DX12
    /// </summary>
    public static void SetEnableDX12(GameId gameId, bool? value)
    {
        SetValue(value, "EnableDX12", gameId.ToString());
    }


    public static bool GetEnableCutsceneManage(GameId gameId)
    {
        return GetValue<bool>(default, "EnableCutsceneManage", gameId.ToString());
    }

    public static void SetEnableCutsceneManage(GameId gameId, bool value)
    {
        SetValue(value, "EnableCutsceneManage", gameId.ToString());
    }


    public static long GetLastSelectGameRecordRole(GameBiz gameBiz)
    {
        return GetValue<long>(default, "LastSelectGameRecordRole", gameBiz);
    }

    public static void SetLastSelectGameRecordRole(GameBiz gameBiz, long value)
    {
        SetValue(value, "LastSelectGameRecordRole", gameBiz);
    }


    #endregion



    #region Obsolete Setting


    [Obsolete("Obsolete Setting", true)]
    public static bool IgnoreRunningGame
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool ShowNoviceGacha
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool ShowChronicledWish
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool UseOneBg
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool EnableSystemTrayIcon
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool ExitWhenClosing
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    [Obsolete("Obsolete Setting", true)]
    public static bool UseSystemThemeColor
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool EnableNavigationViewLeftCompact
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 游戏账号切换
    /// </summary>
    [Obsolete("Obsolete Setting", true)]
    public static bool EnableGameAccountSwitcher
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 当前选择的游戏区服
    /// </summary>
    [Obsolete("Obsolete Setting", true)]
    public static GameBiz CurrentGameBiz
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static string? SelectedGameBizs
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 固定待选择的游戏区服图标
    /// </summary>
    [Obsolete("Obsolete Setting", true)]
    public static bool IsGameBizSelectorPinned
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 缓存的游戏信息 <see cref="GameInfo"/>
    /// </summary>
    [Obsolete("Obsolete Setting", true)]
    public static string? CachedGameInfo
    {
        get => DatabaseService.GetValue<string>(nameof(CachedGameInfo), out _, default);
        set => DatabaseService.SetValue(nameof(CachedGameInfo), value);
    }

    /// <summary>
    /// 保持 RPC 服务在后台运行
    /// </summary>
    [Obsolete("Obsolete Setting", true)]
    public static bool KeepRpcServerRunningInBackground
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 安装游戏时自动创建子文件夹
    /// </summary>
    [Obsolete("Obsolete Setting", true)]
    public static bool AutomaticallyCreateSubfolderForInstall
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static string? DefaultGameInstallationPath
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static string? HyperionDeviceId
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static string? HyperionDeviceFp
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static DateTimeOffset HyperionDeviceFpLastUpdateTime
    {
        get => GetValue<DateTimeOffset>();
        set => SetValue(value);
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool GetUseVersionPoster(GameId gameId)
    {
        return GetValue<bool>(default, "UseVersionPoster", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static void SetUseVersionPoster(GameId gameId, bool value)
    {
        SetValue(value, "UseVersionPoster", gameId.ToString());
    }


    [Obsolete("Obsolete Setting", true)]
    public static string? GetVersionPoster(GameId gameId)
    {
        return GetValue<string>(default, "VersionPoster", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static void SetVersionPoster(GameId gameId, string? value)
    {
        SetValue(value, "VersionPoster", gameId.ToString());
    }


    [Obsolete("Obsolete Setting", true)]
    public static bool GetEnableCustomBg(GameId gameId)
    {
        return GetValue<bool>(default, "EnableCustomBg", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static void SetEnableCustomBg(GameId gameId, bool value)
    {
        SetValue(value, "EnableCustomBg", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static string? GetGameInstallPath(GameId gameId)
    {
        return GetValue<string>(default, "InstallPath", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static void SetGameInstallPath(GameId gameId, string? value)
    {
        SetValue(value, "InstallPath", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static bool GetGameInstallPathRemovable(GameId gameId)
    {
        return GetValue<bool>(default, "InstallPathRemovable", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static void SetGameInstallPathRemovable(GameId gameId, bool value)
    {
        SetValue(value, "InstallPathRemovable", gameId.ToString());
    }


    /// <summary>
    /// 无边框窗口
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public static bool GetUsePopupWindow(GameId gameId)
    {
        return GetValue<bool>(false, "UsePopupWindow", gameId.ToString());
    }

    /// <summary>
    /// 无边框窗口
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="value"></param>
    public static void SetUsePopupWindow(GameId gameId, bool value)
    {
        SetValue(value, "UsePopupWindow", gameId.ToString());
    }


    [Obsolete("Obsolete Setting", true)]
    public static GameBiz GetLastRegionOfGame(GameBiz game)
    {
        return GetValue<GameBiz>(default, "LastRegionOf", game.Game);
    }

    [Obsolete("Obsolete Setting", true)]
    public static void SetLastRegionOfGame(GameBiz game, GameBiz value)
    {
        SetValue(value, "LastRegionOf", game.Game);
    }


    [Obsolete("Obsolete Setting", true)]
    public static string? GetGameBackgroundIds(GameId gameId)
    {
        return GetValue<string>(default, "GameBackgroundIds", gameId.ToString());
    }

    [Obsolete("Obsolete Setting", true)]
    public static void SetGameBackgroundIds(GameId gameId, string? value)
    {
        SetValue(value, "GameBackgroundIds", gameId.ToString());
    }

    /// <summary>
    /// 使用 DX11 启动
    /// </summary>
    public static bool GetEnableDX11(GameId gameId)
    {
        return GetValue<bool>(default, "EnableDX11", gameId.ToString());
    }

    /// <summary>
    /// 使用 DX11 启动
    /// </summary>
    public static void SetEnableDX11(GameId gameId, bool value)
    {
        SetValue(value, "EnableDX11", gameId.ToString());
    }


    #endregion



    #region Setting Method


    private static ConcurrentDictionary<string, string?> _settingCache = new();


    public static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
    {
        return GetValue<T>(defaultValue, key, "");
    }


    public static T? GetValue<T>(T? defaultValue, string? key, string gameId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return defaultValue;
        }
        try
        {
            string cacheKey = $"{key}{gameId}";
            if (_settingCache.TryGetValue(cacheKey, out string? value))
            {
                return ConvertFromString(value, defaultValue);
            }
            using var dapper = DatabaseService.CreateConnection();
            value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM Setting WHERE Key=@key AND GameId=@gameId LIMIT 1;", new { key, gameId });
            _settingCache[cacheKey] = value;
            return ConvertFromString(value, defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }


    private static T? ConvertFromString<T>(string? value, T? defaultValue = default)
    {
        if (value is null)
        {
            return defaultValue;
        }
        var converter = TypeDescriptor.GetConverter(typeof(T));
        if (converter == null)
        {
            return defaultValue;
        }
        return (T?)converter.ConvertFromString(value);
    }


    public static void SetValue<T>(T? value, [CallerMemberName] string? key = null)
    {
        SetValue<T>(value, key, "");
    }


    public static void SetValue<T>(T? value, string? key, string gameId)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return;
        }
        try
        {
            string cacheKey = $"{key}{gameId}";
            string? val = value?.ToString();
            if (_settingCache.TryGetValue(cacheKey, out string? cacheValue) && cacheValue == val)
            {
                return;
            }
            _settingCache[cacheKey] = val;
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO Setting (Key, GameId, Value) VALUES (@key, @gameId, @val);", new { key, gameId, val });
        }
        catch { }
    }


    public static void DeleteAllSettings()
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("DELETE FROM Setting WHERE TRUE;");
        }
        catch { }
    }


    public static void ClearCache()
    {
        _settingCache.Clear();
    }


    #endregion


}
