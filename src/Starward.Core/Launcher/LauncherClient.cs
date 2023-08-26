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



    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default) where T : class
    {
        request.Version = HttpVersion.Version20;
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), LauncherJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
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



    public async Task<LauncherContent> GetLauncherContentAsync(GameBiz biz, string? lang = null, CancellationToken cancellationToken = default)
    {
        lang = Util.FilterLanguage(lang);
        var url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud => "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/content?filter_adv=false&key=eYd89JmJ&language=zh-cn&launcher_id=18",
            GameBiz.hk4e_global => $"https://sdk-os-static.mihoyo.com/hk4e_global/mdk/launcher/api/content?filter_adv=false&key=gcStgarh&language={lang}&launcher_id=10",
            GameBiz.hkrpg_cn => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33",
            GameBiz.hkrpg_global => $"https://hkrpg-launcher-static.hoyoverse.com/hkrpg_global/mdk/launcher/api/content?filter_adv=false&key=vplOVX8Vn7cwG8yb&language={lang}&launcher_id=35",
            GameBiz.bh3_cn => $"https://bh3-launcher-static.mihoyo.com/bh3_cn/mdk/launcher/api/content?key=SyvuPnqL&filter_adv=false&language=zh-cn&launcher_id=4",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=tEGNtVhN&language={lang}&launcher_id=9",
            GameBiz.bh3_global => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=dpz65xJ3&language={lang}&launcher_id=10",
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=demhUTcW&language=zh-tw&launcher_id=8",
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=PRg571Xh&language=ko-kr&launcher_id=11",
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=ojevZ0EyIyZNCy4n&language=ja-jp&launcher_id=19",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await CommonSendAsync<LauncherContent>(request, cancellationToken);
    }



    public async Task<CloudGameBackground> GetCloudGameBackgroundAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cloud => "https://api-cloudgame.mihoyo.com/hk4e_cg_cn/gamer/api/getUIConfig",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var wrapper = await CommonSendAsync<CloudGameBackgroundWrapper>(request, cancellationToken);
        return wrapper.BgImage;
    }




    public async Task<LauncherResource> GetLauncherResourceAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cn => "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/resource?channel_id=1&key=eYd89JmJ&launcher_id=18&sub_channel_id=1",
            GameBiz.hk4e_global => $"https://sdk-os-static.mihoyo.com/hk4e_global/mdk/launcher/api/resource?channel_id=1&key=gcStgarh&launcher_id=10&sub_channel_id=0",
            GameBiz.hkrpg_cn => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/resource?channel_id=1&key=6KcVuOkbcqjJomjZ&launcher_id=33&sub_channel_id=1",
            GameBiz.hkrpg_global => $"https://hkrpg-launcher-static.hoyoverse.com/hkrpg_global/mdk/launcher/api/resource?channel_id=1&key=vplOVX8Vn7cwG8yb&launcher_id=35&sub_channel_id=1",
            GameBiz.bh3_cn => $"https://bh3-launcher-static.mihoyo.com/bh3_cn/mdk/launcher/api/resource?channel_id=1&key=SyvuPnqL&launcher_id=4&sub_channel_id=1",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=tEGNtVhN&launcher_id=9&sub_channel_id=1",
            GameBiz.bh3_global => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?key=dpz65xJ3&channel_id=1&launcher_id=10&sub_channel_id=1",
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=demhUTcW&launcher_id=8&sub_channel_id=1",
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=PRg571Xh&launcher_id=11&sub_channel_id=1",
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=ojevZ0EyIyZNCy4n&launcher_id=19&sub_channel_id=6",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return await CommonSendAsync<LauncherResource>(request, cancellationToken);
    }




    public static string GetGameNoticesUrl(GameBiz biz, long uid, string? lang = null)
    {
        lang = Util.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        return biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud => $"https://webstatic.mihoyo.com/hk4e/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hk4e_global => $"https://sdk.hoyoverse.com/hk4e/announcement/index.html?announcement_version=1.37&auth_appid=announcement&bundle_id=hk4e_global&channel_id=1&game=hk4e&game_biz=hk4e_global&lang={lang}&level=60&platform=pc&region=os_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&uid={uid}",
            GameBiz.hkrpg_cn => $"https://webstatic.mihoyo.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hkrpg_global => $"https://sdk.hoyoverse.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_cn => $"https://webstatic.mihoyo.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_cn&channel_id=1&game=bh3&game_biz=bh3_cn&lang=zh-cn&level=88&platform=pc&region=android01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_global or GameBiz.bh3_jp or GameBiz.bh3_kr or GameBiz.bh3_overseas or GameBiz.bh3_tw => $"https://sdk.hoyoverse.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_os&channel_id=1&game=bh3&game_biz=bh3_os&lang={lang}&level=88&platform=pc&region=overseas01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
    }



    public async Task<bool> IsNoticesAlertAsync(GameBiz biz, long uid, string? lang = null, CancellationToken cancellationToken = default)
    {
        lang = Util.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        string url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud => $"https://hk4e-api.mihoyo.com/common/hk4e_cn/announcement/api/getAlertAnn?bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&uid={uid}",
            GameBiz.hk4e_global => $"https://sg-hk4e-api.hoyoverse.com/common/hk4e_global/announcement/api/getAlertAnn?game=hk4e&game_biz=hk4e_global&lang={lang}&bundle_id=hk4e_global&channel_id=1&level=60&platform=pc&region=os_asia&uid={uid}",
            GameBiz.hkrpg_cn => $"https://hkrpg-api.mihoyo.com/common/hkrpg_cn/announcement/api/getAlertAnn?bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&uid={uid}",
            GameBiz.hkrpg_global => $"https://sg-hkrpg-api.hoyoverse.com/common/hkrpg_global/announcement/api/getAlertAnn?bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&uid={uid}",
            GameBiz.bh3_cn => $"https://api-takumi.mihoyo.com/common/bh3_cn/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_cn&lang={lang}&bundle_id=bh3_cn&platform=pc&region=android01&level=88&channel_id=1&uid={uid}",
            GameBiz.bh3_global or GameBiz.bh3_jp or GameBiz.bh3_kr or GameBiz.bh3_overseas or GameBiz.bh3_tw => $"https://sg-public-api.hoyoverse.com/common/bh3_global/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_global&lang={lang}&bundle_id=bh3_os&platform=pc&region=overseas01&level=88&channel_id=1&uid={uid}",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var alertAnn = await CommonSendAsync<AlertAnn>(request, cancellationToken);
        return alertAnn.Remind || alertAnn.ExtraRemind;
    }




}
