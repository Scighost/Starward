using Microsoft.Win32;
using Starward.Core;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Starward;

internal abstract class AppConfig
{


    private const string REG_KEY_NAME = @"HKEY_CURRENT_USER\Software\Starward";


    public static string? AppVersion { get; private set; }


    public static string ConfigDirectory { get; private set; }



    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };


    private static readonly Dictionary<string, object?> cache = new();


    static AppConfig()
    {
        try
        {
            AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var cd = Registry.GetValue(REG_KEY_NAME, nameof(ConfigDirectory), null) as string;
            if (Directory.Exists(cd))
            {
                ConfigDirectory = cd;
            }
        }
        catch { }
    }



    public static void SetConfigDirectory(string value)
    {
        if (Directory.Exists(value))
        {
            ConfigDirectory = value;
            Registry.SetValue(@"HKEY_CURRENT_USER\Software\Starward", nameof(ConfigDirectory), value);
        }
        else
        {
            throw new DirectoryNotFoundException(value);
        }
    }







    #region Static Setting




    public static bool EnableAutoBackupDatabase
    {
        get => GetValue(true);
        set => SetValue(value);
    }


    public static int BackupIntervalInDays
    {
        get => GetValue(21);
        set => SetValue(value);
    }


    public static bool AutoCheckUpdate
    {
        get => GetValue(true);
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


    public static GameBiz SelectGameBiz
    {
        get => GetValue<GameBiz>();
        set => SetValue((int)value);
    }


    public static int SelectUidInGachaLogPage
    {
        get => GetValue<int>();
        set => SetValue(value);
    }


    public static bool ShowNoviceGacha
    {
        get => GetValue<int>() == 0 ? false : true;
        set => SetValue(value ? 1 : 0);
    }


    public static string? GachaLanguage
    {
        get => GetValue<string>();
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



    public static string? GetStartArgument(GameBiz biz)
    {
        return GetValue<string>(default, $"start_argument_{biz}");
    }

    public static void SetStartArgument(GameBiz biz, string? value)
    {
        SetValue(value, $"start_argument_{biz}");
    }






    #endregion






    private static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }
        try
        {
            if (cache.TryGetValue(key, out var value))
            {
                if (value is T)
                {
                    return (T)value;
                }
            }
            value = Registry.GetValue(REG_KEY_NAME, key, defaultValue);
            cache[key] = value;
            return (T?)(value ?? defaultValue);
        }
        catch
        {
            return defaultValue;
        }
    }



    private static void SetValue<T>(T? value, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }
        try
        {
            cache[key] = value;
            if (value is null)
            {
                Registry.CurrentUser.OpenSubKey(@"Software\Starward", true)?.DeleteValue(key, false);
            }
            else
            {
                Registry.SetValue(REG_KEY_NAME, key, value);
            }
        }
        catch { }
    }


}
