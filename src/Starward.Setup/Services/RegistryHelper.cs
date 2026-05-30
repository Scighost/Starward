using Microsoft.Win32;

namespace Starward.Setup.Services;

public static class RegistryHelper
{

    public static void WriteUninstallInfo(string folder, string version, long size)
    {
        string exe = Path.Combine(folder, "Starward.exe");
        string setupExe = Path.Combine(folder, "Starward.Setup.exe");
        using var subkey = Registry.LocalMachine.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Starward");
        subkey.SetValue("Publisher", "Scighost", RegistryValueKind.String);
        subkey.SetValue("DisplayName", "Starward", RegistryValueKind.String);
        subkey.SetValue("DisplayIcon", exe, RegistryValueKind.String);
        subkey.SetValue("DisplayVersion", version, RegistryValueKind.String);
        subkey.SetValue("InstallLocation", folder, RegistryValueKind.String);
        subkey.SetValue("EstimatedSize", (int)(size / 1024), RegistryValueKind.DWord);
        subkey.SetValue("InstallDate", $"{DateTime.Now:yyyyMMdd}", RegistryValueKind.String);
        subkey.SetValue("UninstallString", $"\"{setupExe}\" uninstall", RegistryValueKind.String);
        subkey.SetValue("QuietUninstallString", $"\"{setupExe}\" uninstall /S", RegistryValueKind.String);
    }


    public static void DeleteUninstallInfo()
    {
        Registry.LocalMachine.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Starward", false);
    }


    public static void WriteUrlProtocol(string folder)
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Starward", false);
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Starward", "", "URL:Starward Protocol");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Starward", "URL Protocol", "");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Starward\DefaultIcon", "", "Starward.exe,1");
        Registry.SetValue(@"HKEY_LOCAL_MACHINE\Software\Classes\Starward\Shell\Open\Command", "", $"""
            "{Path.Combine(folder, "Starward.exe")}" "%1"
            """);
    }


    public static void DeleteUrlProtocol()
    {
        Registry.LocalMachine.DeleteSubKeyTree(@"Software\Classes\Starward", false);
    }


    public static string? GetInstallLocation()
    {
        using var subkey = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Starward");
        return subkey?.GetValue("InstallLocation") as string;
    }


    public static void DeleteRegistrySetting()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Starward", false);
    }

}





