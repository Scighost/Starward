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
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version20 };
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




    public async Task<LauncherContent> GetLauncherContentAsync(GameBiz biz, string? lang = null)
    {
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = "en-us";
        }
        var url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud => "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/content?filter_adv=false&key=eYd89JmJ&language=zh-cn&launcher_id=18",
            GameBiz.hk4e_global => $"https://sdk-os-static.mihoyo.com/hk4e_global/mdk/launcher/api/content?filter_adv=false&key=gcStgarh&language={lang}&launcher_id=10",
            GameBiz.hkrpg_cn => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33",
            GameBiz.hkrpg_global => $"https://hkrpg-launcher-static.hoyoverse.com/hkrpg_global/mdk/launcher/api/content?filter_adv=false&key=vplOVX8Vn7cwG8yb&language={lang}&launcher_id=35",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await CommonSendAsync<LauncherContent>(request);
    }



    // https://webstatic.mihoyo.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang=zh-cn&level=20&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid=101566062#/
    // https://hkrpg-api.mihoyo.com/common/hkrpg_cn/announcement/api/getAnnList?game=hkrpg&game_biz=hkrpg_cn&lang=zh-cn&auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&level=20&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid=101566062
    // https://hkrpg-api-static.mihoyo.com/common/hkrpg_cn/announcement/api/getAnnContent?game=hkrpg&game_biz=hkrpg_cn&lang=zh-cn&bundle_id=hkrpg_cn&platform=pc&region=prod_gf_cn&t=1683271998134&level=20&channel_id=1


}
