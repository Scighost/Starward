using Starward.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Starward;

internal abstract class AppConfig
{


    public static string? AppVersion { get; private set; }


    public static string ConfigDirectory { get; private set; }


    public static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };


    static AppConfig()
    {
        try
        {
            AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            var module = Process.GetCurrentProcess().MainModule?.FileName;
            if (File.Exists(module) && Path.GetFileNameWithoutExtension(module) != "dotnet")
            {
                ConfigDirectory = Path.GetDirectoryName(module)!;
            }
            else
            {
                ConfigDirectory = AppContext.BaseDirectory;
            }
        }
        catch { }
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


    public static ulong MainWindowRect
    {
        get => GetValue<ulong>();
        set => SetValue(value);
    }


    public static bool IsMainWindowMaximum
    {
        get => GetValue<bool>();
        set => SetValue(value);
    }


    public static string? BackgroundImage
    {
        get => GetValue<string>();
        set => SetValue(value);
    }


    public static int LastSelectAccountServerIndex
    {
        get => GetValue<int>();
        set => SetValue(value);
    }

    public static int LastSelectGameServerIndex
    {
        get => GetValue<int>();
        set => SetValue(value);
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


    public static T? GetValue<T>(T? defaultValue = default, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return defaultValue;
        }
        return DatabaseService.Instance.GetValue<T>(key, out _);
    }



    public static void SetValue<T>(T? value, [CallerMemberName] string? key = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }
        try
        {
            if (value?.ToString() is string str)
            {
                DatabaseService.Instance.SetValue(key, str);
            }
        }
        catch { }
    }


}
