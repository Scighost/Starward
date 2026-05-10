using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Vanara.PInvoke;

namespace Starward.RPC;

internal static class AppConfig
{

    public static string? AppVersion { get; private set; }


    public static bool IsAdmin { get; private set; }


    public static string MutexAndPipeName { get; private set; }


    public const string StartupMagic = "zb8L3ShgFjeyDxeA";


    public static string? StarwardLauncherExecutePath { get; private set; }


    public static bool IsPortable { get; private set; }


    public static bool IsAppInRemovableStorage { get; private set; }


    public static string CacheFolder { get; private set; }




    static AppConfig()
    {
        AppVersion = typeof(AppConfig).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        MutexAndPipeName = $"Starward.RPC/{Process.GetCurrentProcess().SessionId}/{AppVersion}";
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




    public static unsafe bool IsDeviceRemovableOrOnUSB(string path)
    {
        try
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
            STORAGE_PROPERTY_QUERY query = new()
            {
                PropertyId = Kernel32.STORAGE_PROPERTY_ID.StorageDeviceProperty,
                QueryType = Kernel32.STORAGE_QUERY_TYPE.PropertyStandardQuery,
            };
            Span<byte> buffer = stackalloc byte[512];
            fixed (byte* pBuffer = buffer)
            {
                bool result = Kernel32.DeviceIoControl(hDevice,
                                                       Kernel32.IOControlCode.IOCTL_STORAGE_QUERY_PROPERTY,
                                                       (nint)(&query),
                                                       (uint)sizeof(STORAGE_PROPERTY_QUERY),
                                                       (nint)pBuffer,
                                                       (uint)buffer.Length,
                                                       out uint bytesReturned,
                                                       IntPtr.Zero);
                if (!result || bytesReturned < sizeof(STORAGE_DEVICE_DESCRIPTOR))
                {
                    return false;
                }
                STORAGE_DEVICE_DESCRIPTOR* desc = (STORAGE_DEVICE_DESCRIPTOR*)pBuffer;
                return desc->BusType == Kernel32.STORAGE_BUS_TYPE.BusTypeUsb;
            }
        }
        catch { }
        return false;
    }



    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_PROPERTY_QUERY
    {
        public Kernel32.STORAGE_PROPERTY_ID PropertyId;
        public Kernel32.STORAGE_QUERY_TYPE QueryType;
        public byte AdditionalParameters;
    }



    [StructLayout(LayoutKind.Sequential)]
    internal struct STORAGE_DEVICE_DESCRIPTOR
    {
        public uint Version;
        public uint Size;
        public byte DeviceType;
        public byte DeviceTypeModifier;
        public byte RemovableMedia;
        public byte CommandQueueing;
        public uint VendorIdOffset;
        public uint ProductIdOffset;
        public uint ProductRevisionOffset;
        public uint SerialNumberOffset;
        public Kernel32.STORAGE_BUS_TYPE BusType;
        public uint RawPropertiesLength;
    }


}
