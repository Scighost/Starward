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




    public static string? InstallPath_CN
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static string? InstallPath_OS
    {
        get => GetValue<string>();
        set => SetValue(value);
    }

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



    public static string? BackgroundImage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static bool EnableBannerAndPost
    {
        get => GetValue(true);
        set => SetValue(value);
    }


    public static GameBiz SelectGameBiz
    {
        get => GetValue<GameBiz>();
        set => SetValue((int)value);
    }


    public static string? StartGameArgument
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static int TargetFPS
    {
        get => GetValue(60);
        set => SetValue(value);
    }


    public static int SelectUidInGachaLogPage
    {
        get => GetValue<int>();
        set => SetValue(value);
    }


    public static bool ShowDepatureWarp
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static string? WarpLanguage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
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



    public static void SetValue<T>(T? value, [CallerMemberName] string? key = null)
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
