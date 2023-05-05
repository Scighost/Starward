using System.Net;
using System.Net.Http.Json;

namespace Starward.Core.Launcher;

public class LauncherClient
{

    private readonly HttpClient _httpClient;



    /// <summary>
    /// 传入参数为空时会自动构造新的 <see cref="HttpClient"/>
    /// </summary>
    /// <param name="httpClient"></param>
    public LauncherClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
    }



    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken? cancellationToken = null) where T : class
    {
        var response = await _httpClient.SendAsync(request, cancellationToken ?? CancellationToken.None);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(MihoyoApiWrapper<T>), LauncherJsonContext.Default) as MihoyoApiWrapper<T>;
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



    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverIndex">0 CN, 1 OS</param>
    /// <param name="lang">default is en-us</param>
    /// <returns></returns>
    public async Task<LauncherContent> GetLauncherContentAsync(int serverIndex, string? lang = null)
    {
        var url = serverIndex switch
        {
            0 => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33",
            1 => $"https://hkrpg-launcher-static.hoyoverse.com/hkrpg_global/mdk/launcher/api/content?filter_adv=false&key=vplOVX8Vn7cwG8yb&language={lang ?? "en-us"}&launcher_id=35",
            _ => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33",
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await CommonSendAsync<LauncherContent>(request);
    }



}
