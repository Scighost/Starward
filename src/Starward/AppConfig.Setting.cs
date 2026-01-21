using Dapper;
using Starward.Core;
using Starward.Features.Database;
using Starward.Features.GameLauncher;
using Starward.Features.ViewHost;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Starward;

public static partial class AppConfig
{


    #region Static Setting


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

    public static bool EnableBannerAndPost
    {
        get => GetValue(true);
        set => SetValue(value);
    }

    public static bool IgnoreRunningGame
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool ShowNoviceGacha
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool ShowChronicledWish
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

    [Obsolete("已不用", true)]
    public static bool UseOneBg
    {
        get => GetValue<bool>();
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

    public static bool EnableSystemTrayIcon
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool ExitWhenClosing
    {
        get => GetValue<bool>();
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

    public static bool UseSystemThemeColor
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static bool EnableNavigationViewLeftCompact
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 游戏账号切换
    /// </summary>
    public static bool EnableGameAccountSwitcher
    {
        get => GetValue<bool>();
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

    public static string? HyperionDeviceId
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? HyperionDeviceFp
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static DateTimeOffset HyperionDeviceFpLastUpdateTime
    {
        get => GetValue<DateTimeOffset>();
        set => SetValue(value);
    }

    public static string? LastAppVersion
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 当前选择的游戏区服
    /// </summary>
    public static GameBiz CurrentGameBiz
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static string? SelectedGameBizs
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    /// <summary>
    /// 固定待选择的游戏区服图标
    /// </summary>
    public static bool IsGameBizSelectorPinned
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    public static string? DefaultGameInstallationPath
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

    public static int SpeedLimitKBPerSecond
    {
        get => GetValue(0);
        set => SetValue(value);
    }

    /// <summary>
    /// 缓存的游戏信息 <see cref="Starward.Core.HoYoPlay.GameInfo"/>
    /// </summary>
    public static string? CachedGameInfo
    {
        get => DatabaseService.GetValue<string>(nameof(CachedGameInfo), out _, default);
        set => DatabaseService.SetValue(nameof(CachedGameInfo), value);
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
    /// 保持 RPC 服务在后台运行
    /// </summary>
    public static bool KeepRpcServerRunningInBackground
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 安装游戏时自动创建子文件夹
    /// </summary>
    public static bool AutomaticallyCreateSubfolderForInstall
    {
        get => GetValue(true);
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

    /// <summary>
    /// 绳网月报自动刷新
    /// </summary>
    public static bool AutoRefreshInterKnotMonthlyReport
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 式舆防卫战自动刷新
    /// </summary>
    public static bool AutoRefreshShiyuDefense
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 危局强袭战自动刷新
    /// </summary>
    public static bool AutoRefreshDeadlyAssault
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }

    /// <summary>
    /// 抽卡/调频记录自动刷新
    /// </summary>
    public static bool AutoRefreshGachaLog
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    #endregion



    #region Dynamic Setting


    public static string? GetBg(GameBiz biz)
    {
        return GetValue<string>(default, $"bg_{biz}");
    }

    public static void SetBg(GameBiz biz, string? value)
    {
        SetValue(value, $"bg_{biz}");
    }


    public static bool GetUseVersionPoster(GameBiz biz)
    {
        return GetValue<bool>(default, $"use_version_poster_{biz}");
    }

    public static void SetUseVersionPoster(GameBiz biz, bool value)
    {
        SetValue(value, $"use_version_poster_{biz}");
    }


    public static string? GetVersionPoster(GameBiz biz)
    {
        return GetValue<string>(default, $"version_poster_{biz}");
    }

    public static void SetVersionPoster(GameBiz biz, string? value)
    {
        SetValue(value, $"version_poster_{biz}");
    }


    public static string? GetCustomBg(GameBiz biz)
    {
        return GetValue<string>(default, $"custom_bg_{biz}");
    }

    public static void SetCustomBg(GameBiz biz, string? value)
    {
        SetValue(value, $"custom_bg_{biz}");
    }


    public static bool GetEnableCustomBg(GameBiz biz)
    {
        return GetValue<bool>(default, $"enable_custom_bg_{biz}");
    }

    public static void SetEnableCustomBg(GameBiz biz, bool value)
    {
        SetValue(value, $"enable_custom_bg_{biz}");
    }


    public static string? GetGameInstallPath(GameBiz biz)
    {
        return GetValue<string>(default, $"install_path_{biz}");
    }

    public static void SetGameInstallPath(GameBiz biz, string? value)
    {
        SetValue(value, $"install_path_{biz}");
    }


    public static bool GetGameInstallPathRemovable(GameBiz biz)
    {
        return GetValue<bool>(default, $"install_path_removable_{biz}");
    }

    public static void SetGameInstallPathRemovable(GameBiz biz, bool value)
    {
        SetValue(value, $"install_path_removable_{biz}");
    }


    public static bool GetEnableThirdPartyTool(GameBiz biz)
    {
        return GetValue<bool>(default, $"enable_third_party_tool_{biz}");
    }

    public static void SetEnableThirdPartyTool(GameBiz biz, bool value)
    {
        SetValue(value, $"enable_third_party_tool_{biz}");
    }


    public static string? GetThirdPartyToolPath(GameBiz biz)
    {
        return GetValue<string>(default, $"third_party_tool_path_{biz}");
    }

    public static void SetThirdPartyToolPath(GameBiz biz, string? value)
    {
        SetValue(value, $"third_party_tool_path_{biz}");
    }


    public static string? GetStartArgument(GameBiz biz)
    {
        return GetValue<string>(default, $"start_argument_{biz}");
    }

    public static void SetStartArgument(GameBiz biz, string? value)
    {
        SetValue(value, $"start_argument_{biz}");
    }


    /// <summary>
    /// 无边框窗口
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public static bool GetUsePopupWindow(GameBiz biz)
    {
        return GetValue<bool>(false, $"use_popup_window_{biz}");
    }

    /// <summary>
    /// 无边框窗口
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="value"></param>
    public static void SetUsePopupWindow(GameBiz biz, bool value)
    {
        SetValue(value, $"use_popup_window_{biz}");
    }


    public static long GetLastUidInGachaLogPage(GameBiz biz)
    {
        return GetValue<long>(default, $"last_gacha_uid_{biz}");
    }

    public static void SetLastUidInGachaLogPage(GameBiz biz, long value)
    {
        SetValue(value, $"last_gacha_uid_{biz}");
    }


    [Obsolete("已不用")]
    public static GameBiz GetLastRegionOfGame(GameBiz game)
    {
        return GetValue<GameBiz>(default, $"last_region_of_{game}");
    }

    [Obsolete("已不用")]
    public static void SetLastRegionOfGame(GameBiz game, GameBiz value)
    {
        SetValue(value, $"last_region_of_{game}");
    }


    public static string? GetDisplayGachaBanners(GameBiz biz)
    {
        return GetValue<string>(default, $"display_gacha_banners_{biz}");
    }

    public static void SetDisplayGachaBanners(GameBiz biz, string value)
    {
        SetValue(value, $"display_gacha_banners_{biz}");
    }


    /// <summary>
    /// 外部截图文件夹
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public static string? GetExternalScreenshotFolder(GameBiz biz)
    {
        return GetValue<string>(default, $"external_screenshot_folder_{biz}");
    }

    /// <summary>
    /// 外部截图文件夹
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="value"></param>
    public static void SetExternalScreenshotFolder(GameBiz biz, string? value)
    {
        SetValue(value, $"external_screenshot_folder_{biz}");
    }


    public static string? GetGameBackgroundIds(GameBiz biz)
    {
        return GetValue<string>(default, $"game_background_ids_{biz}");
    }

    public static void SetGameBackgroundIds(GameBiz biz, string? value)
    {
        SetValue(value, $"game_background_ids_{biz}");
    }


    #endregion



    #region Setting Method


    private static Dictionary<string, string?> _settingCache;


    private static void InitializeSettingProvider()
    {
        try
        {
            if (_settingCache is null)
            {
                using var dapper = DatabaseService.CreateConnection();
                _settingCache = dapper.Query<(string Key, string? Value)>("SELECT Key, Value FROM Setting;").ToDictionary(x => x.Key, x => x.Value);
            }
        }
        catch { }
    }


    public static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return defaultValue;
        }
        InitializeSettingProvider();
        if (_settingCache is null)
        {
            return defaultValue;
        }
        try
        {
            if (_settingCache.TryGetValue(key, out string? value))
            {
                return ConvertFromString(value, defaultValue);
            }
            using var dapper = DatabaseService.CreateConnection();
            value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM Setting WHERE Key=@key LIMIT 1;", new { key });
            _settingCache[key] = value;
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
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }
        if (string.IsNullOrWhiteSpace(UserDataFolder))
        {
            return;
        }
        InitializeSettingProvider();
        if (_settingCache is null)
        {
            return;
        }
        try
        {
            string? val = value?.ToString();
            if (_settingCache.TryGetValue(key, out string? cacheValue) && cacheValue == val)
            {
                return;
            }
            _settingCache[key] = val;
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO Setting (Key, Value) VALUES (@key, @val);", new { key, val });
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
