using Microsoft.Win32;
using Starward.Features.Database;
using Starward.GameInstall;
using Starward.Helpers;
using Starward.Setup.Core;
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Vanara.PInvoke;

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




    public static void CheckEnviroment()
    {
        try
        {
            AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
            IsAppInRemovableStorage = DriveHelper.IsDeviceRemovableOrOnUSB(AppContext.BaseDirectory);

            string? parentFolder = new DirectoryInfo(AppContext.BaseDirectory).Parent?.FullName;
            string portableExe = Path.Join(parentFolder, "Starward.exe");
            string portableVersion = Path.Join(parentFolder, "version.ini");

            if (Directory.Exists(parentFolder) && (File.Exists(portableExe) || File.Exists(portableVersion)))
            {
                InstallType = InstallType.Portable;
                StarwardPortableLauncherExecutePath = portableExe;
                if (!HaveWritePermission(parentFolder))
                {
                    ShowNoPermissionMessageBox(parentFolder);
                    Environment.Exit(0);
                }
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
            GameInstallEnv.CacheFolder = CacheFolder;

            string? userDataFolder = null;
            if (string.IsNullOrWhiteSpace(ConfigPath))
            {
#if DEBUG
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Starward.Debug");
#else
                using RegistryKey key = Registry.CurrentUser.CreateSubKey(@"Software\Starward");
#endif
                userDataFolder = (key.GetValue("UserDataFolder") as string)?.Trim();
            }
            else if (File.Exists(ConfigPath))
            {
                string text = File.ReadAllText(ConfigPath);
                userDataFolder = Regex.Match(text, @"UserDataFolder=(.+)").Groups[1].Value.Trim();
                if (!string.IsNullOrWhiteSpace(userDataFolder) && !Path.IsPathFullyQualified(userDataFolder))
                {
                    userDataFolder = Path.GetFullPath(userDataFolder, Path.GetDirectoryName(ConfigPath)!);
                }
            }

            if (Directory.Exists(userDataFolder))
            {
                if (HaveWritePermission(userDataFolder))
                {
                    UserDataFolder = userDataFolder;
                    DatabaseService.SetDatabase(userDataFolder);
                    GameFileDatabase.SetDatabase(userDataFolder);
                    LoadConfiguration();
                }
                else
                {
                    ShowNoPermissionMessageBox(userDataFolder);
                    Environment.Exit(0);
                }
            }
        }
        catch (Exception ex)
        {
            User32.MessageBox(HWND.NULL, $"{Lang.AppConfig_AnUnknownIssueOccurredDuringInitialization}\n{ex.Message}", "Starward", User32.MB_FLAGS.MB_OK);
            ClipboardHelper.SetText(ex.ToString());
            Environment.Exit(0);
        }
    }


    private static bool HaveWritePermission(string folder)
    {
        try
        {
            string random = Path.Combine(folder, Guid.CreateVersion7().ToString());
            File.WriteAllBytes(random, "Write permission test."u8);
            File.Delete(random);
            return true;
        }
        catch
        {
            return false;
        }
    }


    private static void ShowNoPermissionMessageBox(string folder)
    {
        User32.MessageBox(HWND.NULL, $"{Lang.NoPermissionWindow_NoPermissionToFolderRestartOrClose}\n{folder}", "Starward", User32.MB_FLAGS.MB_OK);
    }


    public static void SetLanguage(string? lang)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(lang))
            {
                var info = new CultureInfo(lang);
                CultureInfo.CurrentUICulture = info;
                CultureInfo.DefaultThreadCurrentUICulture = info;
                Language = lang;
            }
            else
            {
                CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InstalledUICulture;
                Language = null;
            }
        }
        catch { }
    }


    public static void LoadConfiguration()
    {
        try
        {
            Directory.CreateDirectory(CacheFolder);
            FileCache.Initialize(Path.Combine(CacheFolder, "cache"));
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


    private static void LoadConfigurationFromConfigFile(string path)
    {
        if (File.Exists(path))
        {
            string text = File.ReadAllText(path);
            string lang = Regex.Match(text, @"Language=(.+)").Groups[1].Value.Trim();
            SetLanguage(lang);
        }
    }


    private static void LoadConfigurationFromRegistry()
    {
#if DEBUG
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Starward.Debug");
#else
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Starward");
#endif
        string? lang = (key.GetValue("Language") as string)?.Trim();
        SetLanguage(lang);
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


    private static void SaveConfigurationToRegistry()
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
            else
            {
                key.DeleteValue("Language", false);
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
            else
            {
                key.DeleteValue("UserDataFolder", false);
            }
        }
        catch { }
    }


    private static void SaveConfigurationToConfigFile()
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
            Directory.CreateDirectory(Path.GetDirectoryName(ConfigPath)!);
            File.WriteAllText(ConfigPath, sb.ToString());
        }
        catch { }
    }



}
