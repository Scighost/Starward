using Microsoft.Win32;
using Starward.Features.Database;
using Starward.Helpers;
using Starward.Setup.Core;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward;

public static partial class AppConfig
{

    public static string? StarwardPortableLauncherExecutePath { get; private set; }

    public static string AppVersion { get; private set; }

    public static InstallType InstallType { get; set; }

    public static bool IsPortable => InstallType is InstallType.Portable;

    public static bool IsAppInRemovableStorage { get; private set; }

    public static string CacheFolder { get; private set; }

    public static string ConfigPath { get; private set; }

    public static string? Language { get; set; }

    public static string? UserDataFolder { get; set; }

    public static bool IsAdmin { get; private set; }

    public static string LogFile { get; private set; }


    public static bool? EnableLoginAuthTicket { get; set; }

    public static string? stoken { get; set; }

    public static string? mid { get; set; }



    public static void LoadConfiguration()
    {
        try
        {
            AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            IsAppInRemovableStorage = DriveHelper.IsDeviceRemovableOrOnUSB(AppContext.BaseDirectory);

            string? parentFolder = new DirectoryInfo(AppContext.BaseDirectory).Parent?.FullName;
            string portableExe = Path.Join(parentFolder, "Starward.exe");
            if (Directory.Exists(parentFolder) && File.Exists(portableExe))
            {
                InstallType = InstallType.Portable;
                StarwardPortableLauncherExecutePath = portableExe;
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
            }
            Directory.CreateDirectory(CacheFolder);
            var webviewFolder = Path.Combine(CacheFolder, "webview");
            Environment.SetEnvironmentVariable("WEBVIEW2_USER_DATA_FOLDER", webviewFolder, EnvironmentVariableTarget.Process);

            using WindowsIdentity identity = WindowsIdentity.GetCurrent();
            WindowsPrincipal principal = new WindowsPrincipal(identity);
            IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);

            if (string.IsNullOrWhiteSpace(ConfigPath))
            {
                LoadConfigurationFromRegistry();
            }
            else
            {
                LoadConfigurationFromConfigFile(ConfigPath);
            }
        }
        catch { }
    }


    public static void LoadConfigurationFromConfigFile(string path)
    {
        if (File.Exists(path))
        {
            string text = File.ReadAllText(path);
            string lang = Regex.Match(text, @"Language=(.+)").Groups[1].Value.Trim();
            string folder = Regex.Match(text, @"UserDataFolder=(.+)").Groups[1].Value.Trim();
            bool.TryParse(Regex.Match(text, @"EnableLoginAuthTicket=(.+)").Groups[1].Value.Trim(), out bool enabled);
            EnableLoginAuthTicket = enabled;
            stoken = Regex.Match(text, @"stoken=(.+)").Groups[1].Value.Trim();
            mid = Regex.Match(text, @"mid=(.+)").Groups[1].Value.Trim();
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
                    userDataFolder = Path.GetFullPath(folder, Path.GetDirectoryName(path)!);
                }
                if (Directory.Exists(userDataFolder))
                {
                    UserDataFolder = Path.GetFullPath(userDataFolder);
                    DatabaseService.SetDatabase(userDataFolder);
                }
            }
        }
    }


    public static void LoadConfigurationFromRegistry()
    {
#if DEBUG
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Starward.Debug");
#else
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Starward");
#endif
        string? lang = (key.GetValue("Language") as string)?.Trim();
        string? folder = (key.GetValue("UserDataFolder") as string)?.Trim();
        EnableLoginAuthTicket = key.GetValue("EnableLoginAuthTicket") is 1;
        stoken = (key.GetValue("stoken") as string)?.Trim();
        mid = (key.GetValue("mid") as string)?.Trim();
        if (!string.IsNullOrWhiteSpace(lang))
        {
            try
            {
                CultureInfo.CurrentUICulture = new CultureInfo(lang);
                Language = lang;
            }
            catch { }
        }
        if (Directory.Exists(folder))
        {
            UserDataFolder = Path.GetFullPath(folder);
            DatabaseService.SetDatabase(folder);
        }
    }


    public static void SaveConfiguration()
    {
        if (string.IsNullOrWhiteSpace(ConfigPath))
        {
            SaveConfigurationToRegistry();
        }
        else
        {
            SaveConfigurationToConfigFile();
        }
    }


    public static void SaveConfigurationToRegistry()
    {
        try
        {
#if DEBUG
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Starward.Debug");
#else
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\Starward");
#endif
            if (!string.IsNullOrWhiteSpace(Language))
            {
                key.SetValue("Language", Language);
            }
            if (!string.IsNullOrWhiteSpace(UserDataFolder))
            {
                string dataFolder = UserDataFolder;
                string? parentFolder = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrWhiteSpace(parentFolder) && UserDataFolder.StartsWith(parentFolder))
                {
                    dataFolder = Path.GetRelativePath(parentFolder, UserDataFolder);
                }
                key.SetValue("UserDataFolder", dataFolder);
            }
            if (EnableLoginAuthTicket.HasValue)
            {
                key.SetValue("EnableLoginAuthTicket", EnableLoginAuthTicket.Value ? 1 : 0);
            }
            if (!string.IsNullOrWhiteSpace(stoken))
            {
                key.SetValue("stoken", stoken);
            }
            if (!string.IsNullOrWhiteSpace(mid))
            {
                key.SetValue("mid", mid);
            }
        }
        catch { }
    }


    public static void SaveConfigurationToConfigFile()
    {
        try
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrWhiteSpace(UserDataFolder))
            {
                string dataFolder = UserDataFolder;
                string? parentFolder = Path.GetDirectoryName(ConfigPath);
                if (!string.IsNullOrWhiteSpace(parentFolder) && UserDataFolder.StartsWith(parentFolder))
                {
                    dataFolder = Path.GetRelativePath(parentFolder, UserDataFolder);
                }
                sb.AppendLine($"Language={Language}");
                sb.AppendLine($"UserDataFolder={dataFolder}");
            }
            else
            {
                sb.AppendLine($"Language={Language}");
                sb.AppendLine($"UserDataFolder=");
            }
            if (EnableLoginAuthTicket.HasValue)
            {
                sb.AppendLine($"{nameof(EnableLoginAuthTicket)}={EnableLoginAuthTicket}");
            }
            if (!string.IsNullOrWhiteSpace(stoken))
            {
                sb.AppendLine($"{nameof(stoken)}={stoken}");
            }
            if (!string.IsNullOrWhiteSpace(mid))
            {
                sb.AppendLine($"{nameof(mid)}={mid}");
            }
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, sb.ToString());
        }
        catch { }
    }









}
