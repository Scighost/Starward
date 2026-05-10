using System;
using System.IO;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

namespace Starward.Helpers;

internal abstract class DriveHelper
{


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



    private static string? GetPathRoot(string path)
    {
        string? root = Path.GetPathRoot(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(root) && !root.EndsWith('\\'))
        {
            root += '\\';
        }
        return root;
    }


    public static DriveType GetDriveType(string path)
    {
        string? root = GetPathRoot(path);
        if (string.IsNullOrWhiteSpace(root))
        {
            return DriveType.Unknown;
        }
        return (DriveType)Kernel32.GetDriveType(root);
    }



    public static long GetDriveAvailableSpace(string path)
    {
        string? root = GetPathRoot(path);
        if (string.IsNullOrWhiteSpace(root))
        {
            throw new DriveNotFoundException($"The path '{path}' does not have a valid root directory.");
        }
        else
        {
            if (!Kernel32.GetDiskFreeSpaceEx(root, out ulong freeBytesAvailable, out ulong totalNumberOfBytes, out ulong totalNumberOfFreeBytes))
            {
                Kernel32.GetLastError().ThrowIfFailed($"Failed to get disk free space information for path '{path}'.");
            }
            return (long)freeBytesAvailable;
        }
    }


    public static string? GetDriveFormat(string path)
    {
        string? root = GetPathRoot(path);
        if (string.IsNullOrWhiteSpace(root))
        {
            return null;
        }
        else
        {
            if (!Kernel32.GetVolumeInformation(root, out string? volumeName, out uint volumeSerialNumber, out uint maximumComponentLength, out Kernel32.FileSystemFlags fileSystemFlags, out string? fileSystemName))
            {
                Kernel32.GetLastError().ThrowIfFailed($"Failed to get volume information for path '{path}'.");
            }
            return fileSystemName;
        }
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