using Vanara.PInvoke;

namespace Starward.Setup.Services;

public static class DriveHelper
{



    public static string GetSizeText(long size)
    {
        const double KB = 1 << 10;
        const double MB = 1 << 20;
        const double GB = 1 << 30;
        return size switch
        {
            >= (1 << 30) => $"{size / GB:F2} GB",
            >= (1 << 20) => $"{size / MB:F2} MB",
            _ => $"{size / KB:F2} KB",
        };
    }




    public static string GetDriveAvailableSpaceText(string path)
    {
        var drive = new DriveInfo(path);
        if (drive.IsReady)
        {
            return GetSizeText(drive.AvailableFreeSpace);
        }
        return "-";
    }



    public static bool IsDeviceRemovableOrOnUSB(string path)
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
        catch { }
        return false;
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


    private static string? GetPathRoot(string path)
    {
        string? root = Path.GetPathRoot(Path.GetFullPath(path));
        if (!string.IsNullOrWhiteSpace(root) && !root.EndsWith('\\'))
        {
            root += '\\';
        }
        return root;
    }



}