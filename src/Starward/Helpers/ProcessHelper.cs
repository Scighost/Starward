using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using Windows.ApplicationModel;

namespace Starward.Helpers;

internal static class ProcessHelper
{


    private static bool? IsAdmin;


    private static void EnsureIsAdmin()
    {
        if (IsAdmin is null)
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                IsAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }
    }




    public static void StartAsAdmin(string? exe = null, string? argument = null)
    {
        EnsureIsAdmin();
        if (AppConfig.MsixPackaged && !IsAdmin!.Value)
        {
            if (string.IsNullOrWhiteSpace(exe))
            {
                exe = $@"shell:AppsFolder\{Package.Current.Id.FamilyName}!App";
            }
            else
            {
                exe = $@"shell:AppsFolder\{Package.Current.Id.FamilyName}!{exe}";
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(exe))
            {
                exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
            }
            else
            {
                exe = Path.Combine(AppContext.BaseDirectory, exe);
            }
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = exe,
            UseShellExecute = true,
            Arguments = argument,
            Verb = "runas",
        });
    }



}
