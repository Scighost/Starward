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
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version30 };
    }



    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken? cancellationToken = null) where T : class
    {
        request.Version = HttpVersion.Version30;
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




    private static string FilterLanguage(string? lang)
    {
        // zh-cn,zh-tw,en-us,de-de,es-es,fr-fr,id-id,it-it,ja-jp,ko-kr,pt-pt,ru-ru,th-th,tr-tr,vi-vn
        var low = lang?.ToLower() ?? "";
        if (low.Length < 2)
        {
            low = "..";
        }
        return low switch
        {
            "zh-hk" or "zh-mo" or "zh-tw" => "zh-tw",
            "zh-cn" or "zh-sg" => "zh-cn",
            _ => low[..2] switch
            {
                "de" => "de-de",
                "es" => "es-es",
                "fr" => "fr-fr",
                "id" => "id-id",
                "it" => "it-it",
                "ja" => "ja-jp",
                "ko" => "ko-kr",
                "pt" => "pt-pt",
                "ru" => "ru-ru",
                "th" => "th-th",
                "tr" => "tr-tr",
                "vi" => "vi-vn",
                _ => "en-us",
            }
        };
    }




    public async Task<LauncherContent> GetLauncherContentAsync(GameBiz biz, string? lang = null)
    {
        lang = FilterLanguage(lang);
        var url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud => "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/content?filter_adv=false&key=eYd89JmJ&language=zh-cn&launcher_id=18",
            GameBiz.hk4e_global => $"https://sdk-os-static.mihoyo.com/hk4e_global/mdk/launcher/api/content?filter_adv=false&key=gcStgarh&language={lang}&launcher_id=10",
            GameBiz.hkrpg_cn => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33",
            GameBiz.hkrpg_global => $"https://hkrpg-launcher-static.hoyoverse.com/hkrpg_global/mdk/launcher/api/content?filter_adv=false&key=vplOVX8Vn7cwG8yb&language={lang}&launcher_id=35",
            GameBiz.bh3_cn => $"https://sdk-static.mihoyo.com/bh3_cn/mdk/launcher/api/content?key=SyvuPnqL&filter_adv=false&language=zh-cn&launcher_id=4",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=tEGNtVhN&language={lang}&launcher_id=9",
            GameBiz.bh3_global => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=dpz65xJ3&language={lang}&launcher_id=10",
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=demhUTcW&language=zh-tw&launcher_id=8",
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=PRg571Xh&language=ko-kr&launcher_id=11",
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=ojevZ0EyIyZNCy4n&language=ja-jp&launcher_id=19",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await CommonSendAsync<LauncherContent>(request);
    }



    public async Task<CloudGameBackground> GetCloudGameBackgroundAsync(GameBiz biz)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cloud => "https://api-cloudgame.mihoyo.com/hk4e_cg_cn/gamer/api/getUIConfig",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var wrapper = await CommonSendAsync<CloudGameBackgroundWrapper>(request);
        return wrapper.BgImage;
    }




    public async Task<LauncherResource> GetLauncherResourceAsync(GameBiz biz)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cn => "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/resource?channel_id=1&key=eYd89JmJ&launcher_id=18&sub_channel_id=1",
            GameBiz.hk4e_global => $"https://sdk-os-static.mihoyo.com/hk4e_global/mdk/launcher/api/resource?channel_id=1&key=gcStgarh&launcher_id=10&sub_channel_id=0",
            GameBiz.hkrpg_cn => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/resource?channel_id=1&key=6KcVuOkbcqjJomjZ&launcher_id=33&sub_channel_id=1",
            GameBiz.hkrpg_global => $"https://hkrpg-launcher-static.hoyoverse.com/hkrpg_global/mdk/launcher/api/resource?channel_id=1&key=vplOVX8Vn7cwG8yb&launcher_id=35&sub_channel_id=1",
            GameBiz.bh3_cn => $"https://sdk-static.mihoyo.com/bh3_cn/mdk/launcher/api/resource?channel_id=1&key=SyvuPnqL&launcher_id=4&sub_channel_id=1",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=tEGNtVhN&launcher_id=9&sub_channel_id=1",
            GameBiz.bh3_global => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?key=dpz65xJ3&channel_id=1&launcher_id=10&sub_channel_id=1",
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=demhUTcW&launcher_id=8&sub_channel_id=1",
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=PRg571Xh&launcher_id=11&sub_channel_id=1",
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=ojevZ0EyIyZNCy4n&launcher_id=19&sub_channel_id=6",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await CommonSendAsync<LauncherResource>(request);
    }



    // https://webstatic.mihoyo.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang=zh-cn&level=20&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid=101566062#/
    // https://hkrpg-api.mihoyo.com/common/hkrpg_cn/announcement/api/getAnnList?game=hkrpg&game_biz=hkrpg_cn&lang=zh-cn&auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&level=20&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid=101566062
    // https://hkrpg-api-static.mihoyo.com/common/hkrpg_cn/announcement/api/getAnnContent?game=hkrpg&game_biz=hkrpg_cn&lang=zh-cn&bundle_id=hkrpg_cn&platform=pc&region=prod_gf_cn&t=1683271998134&level=20&channel_id=1


}
