using System.Reflection;
using System.Security.Principal;
using Vanara.PInvoke;

namespace Starward.RPC;

internal static class AppConfig
{

    public static string? AppVersion { get; private set; }


    public static bool IsAdmin { get; private set; }


    public static string MutexAndPipeName => $"Starward.RPC/{AppVersion}";


    public const string StartupMagic = "zb8L3ShgFjeyDxeA";


    public static string? StarwardLauncherExecutePath { get; private set; }


    public static bool IsPortable { get; private set; }


    public static bool IsAppInRemovableStorage { get; private set; }


    public static string CacheFolder { get; private set; }




    static AppConfig()
    {
        AppVersion = typeof(Program).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        IsAdmin = new WindowsPrincipal(WindowsIdentity.GetCurrent()).IsInRole(WindowsBuiltInRole.Administrator);

        IsAppInRemovableStorage = IsDeviceRemovableOrOnUSB(AppContext.BaseDirectory);
        string? parentFolder = new DirectoryInfo(AppContext.BaseDirectory).Parent?.FullName;
        string launcherExe = Path.Join(parentFolder, "Starward.exe");
        if (Directory.Exists(parentFolder) && File.Exists(launcherExe))
        {
            IsPortable = true;
            StarwardLauncherExecutePath = launcherExe;
        }

        if (IsAppInRemovableStorage && IsPortable)
        {
            CacheFolder = Path.Combine(parentFolder!, ".cache");
        }
        else if (IsAppInRemovableStorage)
        {
            CacheFolder = Path.Combine(Path.GetPathRoot(AppContext.BaseDirectory)!, ".StarwardCache");
        }
        else if (IsPortable)
        {
            CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
        }
        else
        {
            CacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
        }
        Directory.CreateDirectory(CacheFolder);
    }




    public static bool IsDeviceRemovableOrOnUSB(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                DriveInfo drive = new DriveInfo(path);
                if (drive.DriveType is DriveType.Removable)
                {
                    return true;
                }
                string fileName = $@"\\.\{drive.Name.Trim('\\')}";
                using Kernel32.SafeHFILE hDevice = Kernel32.CreateFile(fileName, 0, FileShare.ReadWrite | FileShare.Delete, null, FileMode.Open, 0, HFILE.NULL);
                if (hDevice.IsInvalid)
                {
                    return false;
                }
                Kernel32.STORAGE_PROPERTY_QUERY query = new()
                {
                    PropertyId = Kernel32.STORAGE_PROPERTY_ID.StorageDeviceProperty,
                    QueryType = Kernel32.STORAGE_QUERY_TYPE.PropertyStandardQuery,
                };
                bool result = Kernel32.DeviceIoControl(hDevice, Kernel32.IOControlCode.IOCTL_STORAGE_QUERY_PROPERTY, query, out Kernel32.STORAGE_DEVICE_DESCRIPTOR_MGD desc);
                if (result)
                {
                    if (desc.BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeUsb)
                    {
                        return true;
                    }
                }
            }
        }
        catch { }
        return false;
    }



}
