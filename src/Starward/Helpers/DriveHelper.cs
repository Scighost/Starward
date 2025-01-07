using System.IO;
using Vanara.PInvoke;

namespace Starward.Helpers;

internal abstract class DriveHelper
{


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