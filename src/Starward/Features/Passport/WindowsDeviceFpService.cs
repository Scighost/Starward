using Microsoft.UI.Windowing;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.DeviceFp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics;

namespace Starward.Features.Passport;

internal partial class WindowsDeviceFpService
{

    /// <summary>
    /// SDK 注册表键
    /// </summary>
    private const string SdkKey = @"HKEY_CURRENT_USER\Software\miHoYoSDK";

    private const string CpuKey = @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\CentralProcessor\0";

    private const string BiosKey = @"HKEY_LOCAL_MACHINE\HARDWARE\DESCRIPTION\System\BIOS";

    private const string GpuClassKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}";

    private const string MachineGuidKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Cryptography";

    private const string WindowsVersionKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion";


    private readonly DeviceFpClient _client;


    public WindowsDeviceFpService(DeviceFpClient deviceFpClient)
    {
        _client = deviceFpClient;
    }



    /// <summary>
    /// 初始化设备信息
    /// </summary>
    public async Task InitializeDeviceFpAsync(ServerRegion region, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(GetSdkValue("MIHOYOSDK_DEVICE_FP")))
        {
            var extList = await _client.GetExtListAsync(region, DevicePlatform.PC, cancellationToken).ConfigureAwait(false);
            var request = CreateRequest(extList);
            request.AppName = region switch
            {
                ServerRegion.miHoYo => "HYP_cn",
                ServerRegion.HoYoverse => "HYP_global",
                _ => throw new NotSupportedException($"Region '{region}' is not supported.")
            };
            var result = await _client.GetDeviceFpAsync(region, request, cancellationToken).ConfigureAwait(false);
            SaveSdkValueIfMissing("MIHOYOSDK_DEVICE_FP", result.DeviceFp);
        }
    }



    /// <summary>
    /// 登录窗口的 deviceId。与 <see cref="CreateRequest"/> 的取值一致，取注册表缓存，没有就按 HYP 的算法生成。
    /// </summary>
    public string GetDeviceId()
    {
        return GetSdkValue("MIHOYOSDK_DEVICE_ID") ?? GenerateDeviceId();
    }



    /// <summary>
    /// 登录窗口的 deviceFp。取注册表缓存，没有就用占位值。
    /// </summary>
    public string GetDeviceFp()
    {
        return GetSdkValue("MIHOYOSDK_DEVICE_FP") ?? RandomNumberGenerator.GetString("0123456789", 10);
    }


    /// <summary>
    /// 构造 getFp 请求体：device_id / seed / device_fp 取注册表缓存或默认值，
    /// </summary>
    public DeviceFpRequest CreateRequest(DeviceExtListResult extList)
    {
        var request = new DeviceFpRequest
        {
            DeviceId = GetSdkValue("MIHOYOSDK_DEVICE_ID") ?? GenerateDeviceId(),
            SeedId = GetSdkValue("MIHOYOSDK_SEED_ID") ?? RandomNumberGenerator.GetHexString(16, true),
            SeedTime = GetSdkValue("MIHOYOSDK_SEED_TIME") ?? DateTimeOffset.Now.ToUnixTimeMilliseconds().ToString(),
            Platform = "3",
            DeviceFp = GetSdkValue("MIHOYOSDK_DEVICE_FP") ?? RandomNumberGenerator.GetString("0123456789", 10),
            ExtFields = JsonSerializer.Serialize(CollectExtFields(extList.ExtList ?? [])),
        };

        // 和 SDK 一样，只在值不存在时回写，避免覆盖启动器/游戏已经写好的数据
        //SaveSdkValueIfMissing("MIHOYOSDK_DEVICE_FP", request.DeviceFp);
        SaveSdkValueIfMissing("MIHOYOSDK_SEED_ID", request.SeedId);
        SaveSdkValueIfMissing("MIHOYOSDK_SEED_TIME", request.SeedTime);
        SaveSdkValueIfMissing("MIHOYOSDK_DEVICE_UID", GetExtFieldDeviceUid());

        return request;
    }


    private Dictionary<string, string> CollectExtFields(IEnumerable<string> names)
    {
        var gpu = GetGpuInfo();
        var memory = GetMemoryInfo();
        var rom = GetRomInfo();
        var fields = new Dictionary<string, string>();
        foreach (string name in names)
        {
            fields[name] = name switch
            {
                "osVersion" => GetOsVersion(),
                "cpuName" => Registry.GetValue(CpuKey, "ProcessorNameString", null)?.ToString() ?? "unknown",
                "cpuCores" => Environment.ProcessorCount.ToString(),
                "cpuFrequency" => Registry.GetValue(CpuKey, "~MHz", null)?.ToString() ?? "unknown",
                "cpuType" => "unknown",
                "deviceModel" => Registry.GetValue(BiosKey, "BaseBoardProduct", null)?.ToString() ?? "unknown",
                "deviceName" or "systemName" => Environment.MachineName,
                "deviceType" => "Desktop",
                "deviceUID" => GetExtFieldDeviceUid(),
                "gpuName" => gpu.Name,
                "gpuVendor" => gpu.Vendor,
                "gpuVendorID" => gpu.VendorId,
                "gpuID" => gpu.DeviceId,
                "gpuMemory" => gpu.Memory,
                "gpuVersion" => gpu.Version,
                "gpuAPI" => gpu.Api,
                "isGpuMultiTread" => "false",
                "memorySize" or "ramCapacity" => memory.Total,
                "ramRemain" => memory.Available,
                "romCapacity" => rom.Total,
                "romRemain" => rom.Available,
                "screenSize" => GetScreenSize(),
                "addressMAC" => GetMacAddress(),
                "packageVersion" => AppConfig.AppVersion,
                // 其余是移动端/浏览器字段
                _ => "unknown",
            };
        }
        return fields;
    }


    private string GetExtFieldDeviceUid()
    {
        return GetSdkValue("MIHOYOSDK_DEVICE_UID") ?? GetMachineGuid();
    }


    private static string? GetSdkValue(string name)
    {
        string? value = Registry.GetValue(SdkKey, name, null) as string;
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return value;
    }


    private static void SaveSdkValueIfMissing(string name, string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || GetSdkValue(name) is not null)
        {
            return;
        }
        try
        {
            Registry.SetValue(SdkKey, name, value, RegistryValueKind.String);
        }
        catch { }
    }


    private static string GetMachineGuid()
    {
        return Registry.GetValue(MachineGuidKey, "MachineGuid", null) as string ?? "";
    }


    /// <summary>
    /// 生成设备号：<c>SHA1(主板序列号 + BIOS 序列号 + 系统 ProductId)</c> 的 40 位小写十六进制，再拼接当前毫秒。
    /// <para>
    /// 主板序列号取 SMBIOS Type 2，BIOS 序列号取 SMBIOS Type 1，与 Win32_BaseBoard / Win32_BIOS 的 SerialNumber 一致。
    /// 任一序列号缺失时退化成 MachineGuid，保证仍能得到一个稳定值。
    /// </para>
    /// </summary>
    private static string GenerateDeviceId()
    {
        string boardSerial = ReadSmbiosSerial(2);
        string biosSerial = ReadSmbiosSerial(1);
        string productId = Registry.GetValue(WindowsVersionKey, "ProductId", null)?.ToString() ?? "";

        string raw;
        if (string.IsNullOrEmpty(boardSerial) || string.IsNullOrEmpty(biosSerial))
        {
            raw = GetMachineGuid();
        }
        else
        {
            raw = boardSerial + biosSerial + productId;
        }

        byte[] hash = SHA1.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant() + DateTimeOffset.Now.ToUnixTimeMilliseconds();
    }


    /// <summary>
    /// 读取 SMBIOS 表中的字符串字段（Type 1 = 系统，Type 2 = 主板）。
    /// </summary>
    private static string ReadSmbiosSerial(byte type)
    {
        try
        {
            uint size = Kernel32.GetSystemFirmwareTable(Kernel32.FirmwareTableProviderId.RSMB, 0, out byte[]? buffer);
            if (buffer is null || size > buffer.Length)
            {
                return "";
            }

            int offset = 8; // 跳过 SMBIOS 数据头
            while (offset + 4 <= buffer.Length)
            {
                byte structureType = buffer[offset];
                int length = buffer[offset + 1];
                if (length < 4)
                {
                    break;
                }
                if (structureType == type)
                {
                    // Type 1 / Type 2 的 Serial Number 都在结构体偏移 +7 处
                    return ReadSmbiosString(buffer, offset, length, buffer[offset + 7]);
                }
                if (structureType == 127)
                {
                    break;
                }
                int position = offset + length;
                while (position + 1 < buffer.Length && !(buffer[position] == 0 && buffer[position + 1] == 0))
                {
                    position++;
                }
                offset = position + 2;
            }
        }
        catch { }
        return "";
    }


    /// <summary>
    /// 取 SMBIOS 结构体后面第 <paramref name="index"/> 个字符串（从 1 开始，0 表示没有）。
    /// </summary>
    private static string ReadSmbiosString(byte[] buffer, int offset, int length, byte index)
    {
        if (index == 0)
        {
            return "";
        }
        int position = offset + length;
        for (int i = 1; i <= index; i++)
        {
            if (position >= buffer.Length)
            {
                return "";
            }
            int end = position;
            while (end < buffer.Length && buffer[end] != 0)
            {
                end++;
            }
            if (i == index)
            {
                return Encoding.UTF8.GetString(buffer, position, end - position).Trim();
            }
            position = end + 1;
        }
        return "";
    }


    private static string GetOsVersion()
    {
        Version version = Environment.OSVersion.Version;
        return $"Windows {version.Major}.{version.Minor}.{version.Build}";
    }


    /// <summary>
    /// GPU 信息，取值方式和 SDK 一致：DXGI 拿适配器信息，D3D11 拿特性等级。
    /// </summary>
    private readonly record struct GpuInfo(string Name, string Vendor, string VendorId, string DeviceId, string Memory, string Version, string Api);


    private static GpuInfo GetGpuInfo()
    {
        if (TryGetGpuInfoFromDxgi(out GpuInfo info))
        {
            return info;
        }
        // DXGI 不可用时退回注册表，能取多少算多少
        var (name, vendor, memory) = GetGpuInfoFromRegistry();
        return new GpuInfo(name, vendor, "unknown", "unknown", memory, "Direct3D 11.0", "Direct3D 11");
    }


    private static bool TryGetGpuInfoFromDxgi(out GpuInfo info)
    {
        info = default;
        try
        {
            // 不同系统支持的 factory 接口版本不同，从新到旧依次尝试
            DXGI.IDXGIFactory? factory = CreateDxgiFactory();
            if (factory is null)
            {
                return false;
            }

            // 枚举主显卡
            if (factory.EnumAdapters(0, out DXGI.IDXGIAdapter? adapter).Failed || adapter is null)
            {
                return false;
            }

            DXGI.DXGI_ADAPTER_DESC desc = adapter.GetDesc();

            info = new GpuInfo(
                string.IsNullOrWhiteSpace(desc.Description) ? "unknown" : desc.Description,
                GetGpuVendorName(desc.VendorId),
                desc.VendorId.ToString(),
                desc.DeviceId.ToString(),
                (desc.DedicatedVideoMemory >> 20).ToString(),
                "Direct3D 11.0",
                "Direct3D 11");
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }


    /// <summary>
    /// 创建 DXGI factory，从 IDXGIFactory2 往旧版本回退。
    /// </summary>
    private static DXGI.IDXGIFactory? CreateDxgiFactory()
    {
        if (DXGI.CreateDXGIFactory(typeof(DXGI.IDXGIFactory2).GUID, out object? factory2).Succeeded && factory2 is DXGI.IDXGIFactory2 f2)
        {
            return f2;
        }
        if (DXGI.CreateDXGIFactory(typeof(DXGI.IDXGIFactory1).GUID, out object? factory1).Succeeded && factory1 is DXGI.IDXGIFactory1 f1)
        {
            return f1;
        }
        if (DXGI.CreateDXGIFactory(typeof(DXGI.IDXGIFactory).GUID, out object? factory0).Succeeded && factory0 is DXGI.IDXGIFactory f0)
        {
            return f0;
        }
        return null;
    }


    /// <summary>
    /// DXGI VendorId 到厂商名的映射，和 SDK 一致。
    /// </summary>
    private static string GetGpuVendorName(uint vendorId) => vendorId switch
    {
        0x10DE or 0x12D2 => "NVIDIA",
        0x1002 => "AMD",
        0x163C or 0x8086 => "Intel",
        _ => "unknown",
    };


    private static (string Name, string Vendor, string Memory) GetGpuInfoFromRegistry()
    {
        try
        {
            // 显示适配器类的子键是 0000、0001…
            for (int i = 0; i < 16; i++)
            {
                string subKey = $@"{GpuClassKey}\{i:0000}";
                if (Registry.GetValue(subKey, "DriverDesc", null) is string name && !string.IsNullOrWhiteSpace(name))
                {
                    string vendor = Registry.GetValue(subKey, "ProviderName", null) as string ?? "unknown";
                    string memory = "unknown";
                    if (Registry.GetValue(subKey, "HardwareInformation.qwMemorySize", null) is { } size)
                    {
                        long bytes = Convert.ToInt64(size);
                        if (bytes > 0)
                        {
                            memory = (bytes >> 20).ToString();
                        }
                    }
                    return (name, vendor, memory);
                }
            }
        }
        catch { }
        return ("unknown", "unknown", "unknown");
    }


    private static (string Total, string Available) GetMemoryInfo()
    {
        try
        {
            Kernel32.MEMORYSTATUSEX status = new();
            if (Kernel32.GlobalMemoryStatusEx(ref status))
            {
                return ((status.ullTotalPhys >> 20).ToString(), (status.ullAvailPhys >> 20).ToString());
            }
        }
        catch { }
        return ("unknown", "unknown");
    }


    private static (string Total, string Available) GetRomInfo()
    {
        try
        {
            string? root = Path.GetPathRoot(Environment.SystemDirectory);
            if (!string.IsNullOrEmpty(root))
            {
                DriveInfo drive = new(root);
                return ((drive.TotalSize >> 20).ToString(), (drive.AvailableFreeSpace >> 20).ToString());
            }
        }
        catch (Exception)
        {

        }
        return ("unknown", "unknown");
    }


    private static string GetScreenSize()
    {
        try
        {
            DisplayArea area = DisplayArea.GetFromPoint(new PointInt32(0, 0), DisplayAreaFallback.Primary);
            return $"{area.OuterBounds.Width}x{area.OuterBounds.Height}";
        }
        catch (Exception)
        {
            return "unknown";
        }
    }


    private static string GetMacAddress()
    {
        try
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (networkInterface.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel)
                {
                    continue;
                }
                byte[] bytes = networkInterface.GetPhysicalAddress().GetAddressBytes();
                if (bytes.Length == 6)
                {
                    return Convert.ToHexString(bytes);
                }
            }
        }
        catch { }
        return "FFFFFFFFFFFF";
    }


}
