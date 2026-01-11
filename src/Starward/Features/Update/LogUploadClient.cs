using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Update;

public class LogUploadClient
{

    public static Uri DefaultBaseAddress { get; set; } = new("https://starward-log.scighost.com");


    private readonly HttpClient _httpClient;


    public string AppName { get; set; }

    public string AppVersion { get; set; }

    public string DeviceId { get; set; }

    public string SessionId { get; set; }



    public LogUploadClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = DefaultBaseAddress;
    }





    public async Task UploadLogAsync(LogUploadRequestBase request, CancellationToken cancellationToken = default)
    {
        FillProperty(request);
        using var response = await _httpClient.PostAsJsonAsync("/event/upload", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }



    private void FillProperty(LogUploadRequestBase request)
    {
        if (request.Time == default)
        {
            request.Time = DateTimeOffset.Now;
        }
        if (string.IsNullOrWhiteSpace(request.AppName))
        {
            request.AppName = AppName;
        }
        if (string.IsNullOrWhiteSpace(request.AppVersion))
        {
            request.AppVersion = AppVersion;
        }
        if (string.IsNullOrWhiteSpace(request.Architecture))
        {
            request.Architecture = RuntimeInformation.OSArchitecture.ToString();
        }
        if (string.IsNullOrWhiteSpace(request.SystemVersion))
        {
            request.SystemVersion = Environment.OSVersion.VersionString;
        }
        if (string.IsNullOrWhiteSpace(request.DeviceId))
        {
            request.DeviceId = DeviceId;
        }
        if (string.IsNullOrWhiteSpace(request.SessionId))
        {
            request.SessionId = SessionId;
        }
    }



}
