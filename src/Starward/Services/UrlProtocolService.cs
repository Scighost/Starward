using Microsoft.Win32;
using System;
using System.IO;

namespace Starward.Services;

internal class UrlProtocolService
{



    public static void RegisterProtocol()
    {
        UnregisterProtocol();
        string exe = Path.Join(Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('\\', '/')), "Starward.exe");
        if (!File.Exists(exe))
        {
            exe = Path.Join(AppContext.BaseDirectory, "Starward.exe");
        }
        string command = $"""
            "{exe}" "%1"
            """;
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward", "", "URL:Starward Protocol");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward", "URL Protocol", "");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward\DefaultIcon", "", "Starward.exe,1");
        Registry.SetValue(@"HKEY_CURRENT_USER\Software\Classes\Starward\Shell\Open\Command", "", command);
    }



    public static void UnregisterProtocol()
    {
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Classes\Starward", false);
    }









}
