using System.Reflection;
using System.Security.Principal;

namespace Starward.RPC;

internal static class AppConfig
{

    public static string? AppVersion { get; private set; }


    public static bool IsAdmin { get; private set; }


    public static string MutexAndPipeName => $"Starward.RPC/{AppVersion}";


    public const string StartupMagic = "zb8L3ShgFjeyDxeA";



    static AppConfig()
    {
        AppVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);
    }




}
