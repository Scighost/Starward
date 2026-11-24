using Starward.Core.GameRecord;
using System.Net;

namespace Starward.Core.DeviceFp;


public class DeviceFpClient
{

    private readonly HttpClient _httpClient;


    public DeviceFpClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
    }



    private async Task<T> SendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken) where T : class
    {
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var wrapper = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), DeviceFpJsonContext.Default, cancellationToken).ConfigureAwait(false) as miHoYoApiWrapper<T>;
        if (wrapper is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        return wrapper.Data;
    }



    /// <summary>
    /// 
    /// </summary>
    /// <param name="region"></param>
    /// <param name="platform">1 = iOS，2 = Android，3 = PC，4 = Web</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DeviceExtListResult> GetExtListAsync(ServerRegion region, DevicePlatform platform, CancellationToken cancellationToken = default)
    {
        string url = region switch
        {
            ServerRegion.miHoYo => $"https://public-data-api.mihoyo.com/device-fp/api/getExtList?platform={(int)platform}",
            ServerRegion.HoYoverse => $"https://sg-public-data-api.hoyoverse.com/device-fp/api/getExtList?platform={(int)platform}",
            _ => throw new NotSupportedException($"Region '{region}' is not supported."),
        };
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await SendAsync<DeviceExtListResult>(request, cancellationToken).ConfigureAwait(false);
    }



    /// <summary>
    /// 设备指纹
    /// </summary>
    /// <param name="region"></param>
    /// <param name="requestBody"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DeviceFpResult> GetDeviceFpAsync(ServerRegion region, DeviceFpRequest request, CancellationToken cancellationToken = default)
    {
        string url = region switch
        {
            ServerRegion.miHoYo => $"https://public-data-api.mihoyo.com/device-fp/api/getFp",
            ServerRegion.HoYoverse => $"https://sg-public-data-api.hoyoverse.com/device-fp/api/getFp",
            _ => throw new NotSupportedException($"Region '{region}' is not supported."),
        };
        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(request, DeviceFpJsonContext.Default.DeviceFpRequest),
        };
        return await SendAsync<DeviceFpResult>(message, cancellationToken).ConfigureAwait(false);
    }



    /// <summary>
    /// 设备指纹
    /// </summary>
    /// <param name="region"></param>
    /// <param name="requestBody"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<DeviceFpResult> GetDeviceFpAsync(ServerRegion region, AndroidDeviceInfo deviceInfo, CancellationToken cancellationToken = default)
    {
        deviceInfo = deviceInfo.Clone();
        if (region is ServerRegion.HoYoverse)
        {
            deviceInfo.AppName = "bbs_oversea";
            deviceInfo.ExtFields.PackageName = "com.mihoyo.hyperion";
            deviceInfo.ExtFields.PackageVersion = GameRecordClientBase.HoyolabSdkVersion;
        }
        if (region is ServerRegion.miHoYo)
        {
            deviceInfo.AppName = "bbs_cn";
            deviceInfo.ExtFields.PackageName = "com.mihoyo.hyperion";
            deviceInfo.ExtFields.PackageVersion = GameRecordClientBase.HyperionSdkVersion;
        }
        return await GetDeviceFpAsync(region, deviceInfo.ToRequest(), cancellationToken).ConfigureAwait(false);
    }


}
