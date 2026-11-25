using Starward.Core.DeviceFp;
using System.Net;

namespace Starward.Core.GameRecord;

public abstract class GameRecordClientBase
{

    protected const string Accept = "Accept";
    protected const string Cookie = "Cookie";
    protected const string UserAgent = "User-Agent";
    protected const string X_Request_With = "X-Requested-With";
    protected const string DS = "DS";
    protected const string Referer = "Referer";
    protected const string application_json = "application/json";
    protected const string com_mihoyo_hyperion = "com.mihoyo.hyperion";
    protected const string com_mihoyo_hoyolab = "com.mihoyo.hoyolab";
    protected const string x_rpc_app_version = "x-rpc-app_version";
    protected const string x_rpc_device_id = "x-rpc-device_id";
    protected const string x_rpc_device_fp = "x-rpc-device_fp";
    protected const string x_rpc_client_type = "x-rpc-client_type";
    protected const string x_rpc_lang = "x-rpc-lang";
    protected const string x_rpc_language = "x-rpc-language";
    protected const string x_rpc_app_id = "x-rpc-app_id";
    protected const string x_rpc_game_biz = "x-rpc-game_biz";
    protected const string x_rpc_sdk_version = "x-rpc-sdk_version";
    protected const string x_rpc_device_model = "x-rpc-device_model";
    protected const string x_rpc_device_name = "x-rpc-device_name";
    protected const string x_rpc_sys_version = "x-rpc-sys_version";
    protected const string x_rpc_channel = "x-rpc-channel";


    public const string HyperionAppVersion = "2.118.1";

    public const string HyperionSdkVersion = "2.44.0";

    public const string HyperionUAContent = $"Mozilla/5.0 (Linux; Android 17) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/157.0.0.0 Mobile Safari/537.36 miHoYoBBS/{HyperionAppVersion}";


    public const string HoyolabAppVersion = "4.23.0";

    public const string HoyolabSdkVersion = "2.3.0";

    public const string HoyolabUAContent = $"Mozilla/5.0 (Linux; Android 17) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/157.0.0.0 Mobile Safari/537.36 miHoYoBBSOversea/{HoyolabAppVersion}";


    public abstract string AppVersion { get; }

    public abstract string DefaultUAContent { get; }

    public abstract ServerRegion ServerRegion { get; }

    public string UAContent { get => field ?? DefaultUAContent; set; }


    public AndroidDeviceInfo? DeviceInfo
    {
        get; set
        {
            if (ReferenceEquals(field, value))
            {
                return;
            }
            field = value;
            if (value?.ExtFields is not null)
            {
                if (!string.IsNullOrWhiteSpace(value.ExtFields.DeviceInfo))
                {
                    var splits = value.ExtFields.DeviceInfo.Split('/');
                    if (splits.Length > 3)
                    {
                        string build = splits[3];
                        UAContent = DefaultUAContent.Replace("Android 17", $"Android {value.ExtFields.OsVersion}; {value.ExtFields.Model} Build/{build}");
                        return;
                    }
                }
            }
            UAContent = null!;
        }
    }


    public string Language
    {
        get => LanguageUtil.FilterLanguage(field ?? (ServerRegion is ServerRegion.miHoYo ? "zh-cn" : "en-us"));
        set => field = LanguageUtil.FilterLanguage(value);
    }


    protected readonly HttpClient _httpClient;


    public GameRecordClientBase(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
    }




    protected virtual void AddDeviceHeaders(HttpRequestMessage request)
    {
        if (DeviceInfo is { } deviceInfo)
        {
            AddHeader(request, x_rpc_device_id, deviceInfo.BbsDeviceId);
            AddHeader(request, x_rpc_device_fp, deviceInfo.DeviceFp);
            AddHeader(request, x_rpc_device_name, GetDeviceName(deviceInfo.ExtFields));
            AddHeader(request, x_rpc_device_model, deviceInfo.ExtFields?.Model);
            AddHeader(request, x_rpc_sys_version, deviceInfo.ExtFields?.OsVersion);
        }
        if (ServerRegion is ServerRegion.miHoYo)
        {
            AddHeader(request, x_rpc_channel, "miyousheluodi");
        }
        if (ServerRegion is ServerRegion.HoYoverse)
        {
            AddHeader(request, x_rpc_channel, "google");
            AddHeader(request, x_rpc_lang, Language);
            AddHeader(request, x_rpc_language, Language);
        }
    }


    private static string? GetDeviceName(AndroidDeviceExtFields? extFields)
    {
        if (extFields is null)
        {
            return null;
        }
        string manufacturer = extFields.Manufacturer ?? "";
        string model = extFields.Model ?? "";
        string result = model;
        if (!model.StartsWith(manufacturer, StringComparison.Ordinal))
        {
            result = $"{manufacturer} {model}";
        }
        if (result.Length > 0)
        {
            result = result.Substring(0, 1).ToUpper() + result.Substring(1);
        }
        return Uri.EscapeDataString(result);
    }



    protected static void AddHeader(HttpRequestMessage request, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            request.Headers.Add(name, value);
        }
    }



    protected virtual async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default) where T : class
    {
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        request.Headers.Add(Accept, application_json);
        request.Headers.Add(UserAgent, UAContent);
        AddDeviceHeaders(request);
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), GameRecordJsonContext.Default, cancellationToken).ConfigureAwait(false) as miHoYoApiWrapper<T>;
        if (responseData is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        if (responseData.Retcode != 0)
        {
            throw new miHoYoApiException(responseData.Retcode, responseData.Message);
        }
        return responseData.Data;
    }



}
