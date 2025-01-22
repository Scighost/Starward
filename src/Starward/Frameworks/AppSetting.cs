using Dapper;
using Starward.Core;
using Starward.Features.Database;
using Starward.Features.GameLauncher;
using Starward.Features.ViewHost;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Starward.Frameworks;

public static class AppSetting
{



    static AppSetting()
    {
        try
        {
            AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";

            IsAppInRemovableStorage = DriveHelper.IsDeviceRemovableOrOnUSB(AppContext.BaseDirectory);
            string? parentFolder = new DirectoryInfo(AppContext.BaseDirectory).Parent?.FullName;
            string launcherExe = Path.Join(parentFolder, "Starward.exe");
            if (Directory.Exists(parentFolder) && File.Exists(launcherExe))
            {
                IsPortable = true;
                StarwardLauncherExecutePath = launcherExe;
            }

            if (IsAppInRemovableStorage && IsPortable)
            {
                CacheFolder = Path.Combine(parentFolder!, ".cache");
                ConfigPath = Path.Combine(parentFolder!, "config.ini");
            }
            else if (IsAppInRemovableStorage)
            {
                CacheFolder = Path.Combine(Path.GetPathRoot(AppContext.BaseDirectory)!, ".StarwardCache");
                ConfigPath = Path.Combine(CacheFolder, "config.ini");
            }
            else if (IsPortable)
            {
                CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
                ConfigPath = Path.Combine(parentFolder!, "config.ini");
            }
            else
            {
                CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
#if DEBUG || DEV
                ConfigPath = Path.Combine(CacheFolder, "config.ini");
#else
                string roamingFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Starward");
                Directory.CreateDirectory(roamingFolder);
                ConfigPath = Path.Combine(roamingFolder, "config.ini");
#endif
            }
            Directory.CreateDirectory(CacheFolder);
            var webviewFolder = Path.Combine(CacheFolder, "webview");
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", webviewFolder, EnvironmentVariableTarget.Process);


            if (File.Exists(ConfigPath))
            {
                string text = File.ReadAllText(ConfigPath);
                string lang = Regex.Match(text, @"Language=(.+)").Groups[1].Value.Trim();
                string folder = Regex.Match(text, @"UserDataFolder=(.+)").Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    try
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(lang);
                        Language = lang;
                    }
                    catch { }
                }
                if (!string.IsNullOrWhiteSpace(folder))
                {
                    string userDataFolder;
                    if (Path.IsPathFullyQualified(folder))
                    {
                        userDataFolder = folder;
                    }
                    else
                    {
                        userDataFolder = Path.GetFullPath(folder, Path.GetDirectoryName(ConfigPath)!);
                    }
                    if (Directory.Exists(userDataFolder))
                    {
                        UserDataFolder = Path.GetFullPath(userDataFolder);
                        DatabaseService.SetDatabase(userDataFolder);
                    }
                }
            }
        }
        catch { }
    }



    public static string StarwardExecutePath => Environment.ProcessPath ?? Path.Combine(AppContext.BaseDirectory, "Starward.exe");


    public static string? StarwardLauncherExecutePath { get; private set; }


    public static string AppVersion { get; private set; }


    public static bool IsPortable { get; private set; }


    public static bool IsAppInRemovableStorage { get; private set; }


    public static string CacheFolder { get; private set; }


    public static string ConfigPath { get; private set; }


    public static string? Language { get; set; }


    public static string? UserDataFolder { get; set; }




    public static void SaveConfiguration()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(UserDataFolder))
            {
                string dataFolder = UserDataFolder;
                string? parentFolder = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrWhiteSpace(parentFolder) && UserDataFolder.StartsWith(parentFolder))
                {
                    dataFolder = Path.GetRelativePath(parentFolder, UserDataFolder);
                }
                File.WriteAllText(ConfigPath, $"""
                    {nameof(Language)}={Language}
                    {nameof(UserDataFolder)}={dataFolder}
                    """);
            }
        }
        catch { }
    }





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
        get => Math.Clamp(GetValue(100), 0, 100);
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


    public static bool DisableGameAccountSwitcher
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



    private static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
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


    private static void SetValue<T>(T? value, [CallerMemberName] string? key = null)
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
