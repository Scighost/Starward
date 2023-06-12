using Starward.Core.Hyperion.Genshin;
using Starward.Core.Hyperion.StarRail;
using System.Net;
#if !DEBUG
using System.Net.Http.Json;
#endif
using System.Text.Json;

namespace Starward.Core.Hyperion;

public class HyperionClient
{


    #region Constant

    protected const string Accept = "Accept";
    protected const string Cookie = "Cookie";
    protected const string UserAgent = "User-Agent";
    protected const string X_Reuqest_With = "X-Requested-With";
    protected const string DS = "DS";
    protected const string Referer = "Referer";
    protected const string Application_Json = "application/json";
    protected const string com_mihoyo_hyperion = "com.mihoyo.hyperion";
    protected const string x_rpc_app_version = "x-rpc-app_version";
    protected const string x_rpc_device_id = "x-rpc-device_id";
    protected const string x_rpc_client_type = "x-rpc-client_type";
    protected const string UAContent = $"Mozilla/5.0 miHoYoBBS/{AppVersion}";
    protected const string AppVersion = "2.49.1";
    protected static readonly string DeviceId = Guid.NewGuid().ToString("D");

    #endregion


    protected readonly HttpClient _httpClient;


    protected HyperionStarRailClient _starRail;
    public HyperionStarRailClient StarRail => _starRail ?? new HyperionStarRailClient(_httpClient);


    protected HyperionGenshinClient _genshin;
    public HyperionGenshinClient Genshin => _genshin ?? new HyperionGenshinClient(_httpClient);



    public HyperionClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version30 };
    }




    #region Common Method




    protected async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken? cancellationToken = null) where T : class
    {
        request.Version = HttpVersion.Version30;
        request.Headers.Add(Accept, Application_Json);
        request.Headers.Add(UserAgent, UAContent);
        var response = await _httpClient.SendAsync(request, cancellationToken ?? CancellationToken.None);
        response.EnsureSuccessStatusCode();
#if DEBUG
        var content = await response.Content.ReadAsStringAsync();
        var responseData = JsonSerializer.Deserialize(content, typeof(MihoyoApiWrapper<T>), HyperionJsonContext.Default) as MihoyoApiWrapper<T>;
#else
        var responseData = await response.Content.ReadFromJsonAsync(typeof(MihoyoApiWrapper<T>), HyperionJsonContext.Default) as MihoyoApiWrapper<T>;
#endif
        if (responseData is null)
        {
            throw new MihoyoApiException(-1, "Can not parse the response body.");
        }
        if (responseData.Retcode != 0)
        {
            throw new MihoyoApiException(responseData.Retcode, responseData.Message);
        }
        return responseData.Data;
    }



    protected async Task CommonSendAsync(HttpRequestMessage request, CancellationToken? cancellationToken = null)
    {
        await CommonSendAsync<object>(request, cancellationToken ?? CancellationToken.None);
        return;
    }


    #endregion




    /// <summary>
    /// 米游社账号信息
    /// </summary>
    /// <param name="cookie"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException">输入的 <c>cookie</c> 为空</exception>
    public async Task<HyperionUser> GetHyperionUserAsync(string cookie, CancellationToken? cancellationToken = null)
    {
        if (string.IsNullOrWhiteSpace(cookie))
        {
            throw new ArgumentNullException(nameof(cookie));
        }
        var request = new HttpRequestMessage(HttpMethod.Get, "https://bbs-api.mihoyo.com/user/wapi/getUserFullInfo?gids=2");
        request.Headers.Add(Cookie, cookie);
        request.Headers.Add(Referer, "https://bbs.mihoyo.com/");
        request.Headers.Add(DS, DynamicSecret.CreateSecret());
        request.Headers.Add(x_rpc_app_version, AppVersion);
        request.Headers.Add(x_rpc_device_id, DeviceId);
        request.Headers.Add(x_rpc_client_type, "5");
        var data = await CommonSendAsync<HyperionUserWrapper>(request, cancellationToken);
        data.MiyousheUser.Cookie = cookie;
        return data.MiyousheUser;
    }





}
