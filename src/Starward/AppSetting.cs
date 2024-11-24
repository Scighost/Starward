using Starward.Core;
using Starward.Features.ViewHost;
using Starward.Models;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;

namespace Starward;

public static class AppSetting
{




    static AppSetting()
    {
        try
        {
            AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            var webviewFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\webview");
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", webviewFolder, EnvironmentVariableTarget.Process);

            string? baseDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\'));
            string exe = Path.Join(baseDir, "Starward.exe");
            if (File.Exists(exe))
            {
                IsPortable = true;
            }
            else
            {
                baseDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
                Directory.CreateDirectory(baseDir);
            }
            string? iniPath = Path.Join(baseDir, "config.ini");
            if (File.Exists(iniPath))
            {
                string text = File.ReadAllText(iniPath);
                string lang = Regex.Match(text, @"Language=(.+)").Groups[1].Value;
                string folder = Regex.Match(text, @"UserDataFolder=(.+)").Groups[1].Value;
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
                        userDataFolder = Path.Join(baseDir, folder);
                    }
                    if (Directory.Exists(userDataFolder))
                    {
                        UserDataFolder = Path.GetFullPath(userDataFolder);
                    }
                }
            }
        }
        catch { }
    }




    public static string AppVersion { get; private set; }


    public static bool IsPortable { get; private set; }


    public static string? Language { get; private set; }


    public static string? UserDataFolder { get; private set; }






    #region Static Setting


    /// <summary>
    /// 主窗口关闭选项，隐藏/退出
    /// </summary>
    public static MainWindowCloseOption CloseWindowOption
    {
        get => GetValue<MainWindowCloseOption>();
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


    public static AfterStartGameAction AfterStartGameAction
    {
        get => GetValue<AfterStartGameAction>();
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



    public static string? GetCustomBg(GameBiz biz)
    {
        return GetValue<string>(default, UseOneBg ? $"custom_bg_All" : $"custom_bg_{biz}");
    }

    public static void SetCustomBg(GameBiz biz, string? value)
    {
        SetValue(value, UseOneBg ? $"custom_bg_All" : $"custom_bg_{biz}");
    }



    public static bool GetEnableCustomBg(GameBiz biz)
    {
        return GetValue<bool>(default, UseOneBg ? $"enable_custom_bg_All" : $"enable_custom_bg_{biz}");
    }

    public static void SetEnableCustomBg(GameBiz biz, bool value)
    {
        SetValue(value, UseOneBg ? $"enable_custom_bg_All" : $"enable_custom_bg_{biz}");
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



    private static DatabaseService DatabaseService;

    private static Dictionary<string, string?> cache;


    private static void InitializeSettingProvider()
    {
        try
        {
            //DatabaseService ??= GetService<DatabaseService>();
            //if (cache is null)
            //{
            //    using var dapper = DatabaseService.CreateConnection();
            //    cache = dapper.Query<(string Key, string? Value)>("SELECT Key, Value FROM Setting;").ToDictionary(x => x.Key, x => x.Value);
            //}
        }
        catch { }
    }



    private static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
    {
        //if (string.IsNullOrWhiteSpace(key))
        //{
        //    return defaultValue;
        //}
        //if (string.IsNullOrWhiteSpace(UserDataFolder))
        //{
        //    return defaultValue;
        //}
        //InitializeSettingProvider();
        //try
        //{
        //    if (cache.TryGetValue(key, out string? value))
        //    {
        //        return ConvertFromString(value, defaultValue);
        //    }
        //    using var dapper = DatabaseService.CreateConnection();
        //    value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM Setting WHERE Key=@key LIMIT 1;", new { key });
        //    cache[key] = value;
        //    return ConvertFromString(value, defaultValue);
        //}
        //catch
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
        //if (string.IsNullOrWhiteSpace(key))
        //{
        //    return;
        //}
        //if (string.IsNullOrWhiteSpace(UserDataFolder))
        //{
        //    return;
        //}
        //InitializeSettingProvider();
        //try
        //{
        //    string? val = value?.ToString();
        //    if (cache.TryGetValue(key, out string? cacheValue) && cacheValue == val)
        //    {
        //        return;
        //    }
        //    cache[key] = val;
        //    using var dapper = DatabaseService.CreateConnection();
        //    dapper.Execute("INSERT OR REPLACE INTO Setting (Key, Value) VALUES (@key, @val);", new { key, val });
        //}
        //catch { }
    }



    public static void DeleteAllSettings()
    {
        try
        {
            //using var dapper = DatabaseService.CreateConnection();
            //dapper.Execute("DELETE FROM Setting WHERE TRUE;");
        }
        catch { }
    }


    #endregion



}
