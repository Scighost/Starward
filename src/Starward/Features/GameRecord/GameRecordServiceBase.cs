using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Starward.Core;
using Starward.Core.DeviceFp;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord;

internal abstract class GameRecordServiceBase
{

    protected readonly IMemoryCache _cache;

    private readonly DeviceFpClient _deviceFpClient;


    public GameRecordServiceBase(IServiceProvider serviceProvider)
    {
        _cache = serviceProvider.GetRequiredService<IMemoryCache>();
        _deviceFpClient = serviceProvider.GetRequiredService<DeviceFpClient>();
    }



    private static AndroidDeviceInfo? _hyperionDeviceInfo;

    private static AndroidDeviceInfo? _hoyolabDeviceInfo;


    public static void ResetDeviceInfo()
    {
        AppConfig.AndroidDeviceInfo = null;
        _hyperionDeviceInfo = null;
        _hoyolabDeviceInfo = null;
    }



    public async Task<AndroidDeviceInfo> GetDeviceInfoAsync(ServerRegion region, CancellationToken cancellation = default)
    {
        return region switch
        {
            ServerRegion.miHoYo => await GetHyperionDeviceInfoAsync(cancellation),
            ServerRegion.HoYoverse => await GetHoyolabDeviceInfoAsync(),
            _ => throw new NotSupportedException($"ServerRegion '{region}' is not supported."),
        };
    }


    private async Task<AndroidDeviceInfo> GetHyperionDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        if (_hyperionDeviceInfo is not null)
        {
            return _hyperionDeviceInfo;
        }
        AndroidDeviceInfo deviceInfo = LoadDeviceInfo();
        try
        {
            DeviceFpResult fpResult = await _deviceFpClient.GetDeviceFpAsync(ServerRegion.miHoYo, deviceInfo, cancellationToken);
            deviceInfo.DeviceFp = fpResult.DeviceFp;
            deviceInfo.BbsDeviceFp = fpResult.DeviceFp;
            AppConfig.AndroidDeviceInfo = JsonSerializer.Serialize(deviceInfo, DeviceFpJsonContext.Default.AndroidDeviceInfo);
            _hyperionDeviceInfo = deviceInfo;
        }
        catch { }
        return deviceInfo;
    }


    private async Task<AndroidDeviceInfo> GetHoyolabDeviceInfoAsync(CancellationToken cancellationToken = default)
    {
        if (_hoyolabDeviceInfo is not null)
        {
            return _hoyolabDeviceInfo;
        }
        AndroidDeviceInfo deviceInfo = LoadDeviceInfo();
        try
        {
            DeviceFpResult fpResult = await _deviceFpClient.GetDeviceFpAsync(ServerRegion.HoYoverse, deviceInfo, cancellationToken);
            deviceInfo.DeviceFp = fpResult.DeviceFp;
            deviceInfo.HoyolabDeviceFp = fpResult.DeviceFp;
            AppConfig.AndroidDeviceInfo = JsonSerializer.Serialize(deviceInfo, DeviceFpJsonContext.Default.AndroidDeviceInfo);
            _hoyolabDeviceInfo = deviceInfo;
        }
        catch { }
        return deviceInfo;
    }


    private static AndroidDeviceInfo LoadDeviceInfo()
    {
        AndroidDeviceInfo? deviceInfo = null;
        try
        {
            string? json = AppConfig.AndroidDeviceInfo;
            if (!string.IsNullOrWhiteSpace(json))
            {
                deviceInfo = JsonSerializer.Deserialize<AndroidDeviceInfo>(json, DeviceFpJsonContext.Default.AndroidDeviceInfo);
            }
        }
        catch { }
        return deviceInfo ??= AndroidDeviceInfo.CreateRandom();
    }


    protected static ServerRegion GameBizToServerRegion(GameBiz gameBiz)
    {
        if (gameBiz.Server.Equals("cn", StringComparison.OrdinalIgnoreCase))
        {
            return ServerRegion.miHoYo;
        }
        else if (gameBiz.Server.Equals("global", StringComparison.OrdinalIgnoreCase))
        {
            return ServerRegion.HoYoverse;
        }
        else
        {
            throw new NotSupportedException($"GameBiz '{gameBiz}' is not supported.");
        }
    }


}
