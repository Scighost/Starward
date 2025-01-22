using System.Net;
using System.Net.Http.Json;

namespace Starward.Core.GameNotice;

public class GameNoticeClient
{

    private readonly HttpClient _httpClient;



    public GameNoticeClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version20 };
    }




    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default) where T : class
    {
        request.Version = HttpVersion.Version20;
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), GameNoticeJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
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




    public static string GetGameNoticeUrl(GameBiz biz, long uid, string? lang = null)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        return biz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.clgm_cn or GameBiz.hk4e_bilibili => $"https://sdk.mihoyo.com/hk4e/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hk4e_global => $"https://sdk.hoyoverse.com/hk4e/announcement/index.html?announcement_version=1.37&auth_appid=announcement&bundle_id=hk4e_global&channel_id=1&game=hk4e&game_biz=hk4e_global&lang={lang}&level=60&platform=pc&region=os_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&uid={uid}",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => $"https://sdk.mihoyo.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hkrpg_global => $"https://sdk.hoyoverse.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_cn => $"https://sdk.mihoyo.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_cn&channel_id=1&game=bh3&game_biz=bh3_cn&lang=zh-cn&level=88&platform=pc&region=android01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_global or GameBiz.bh3_jp or GameBiz.bh3_kr or GameBiz.bh3_os or GameBiz.bh3_asia => $"https://sdk.hoyoverse.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_os&channel_id=1&game=bh3&game_biz=bh3_os&lang={lang}&level=88&platform=pc&region=overseas01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.nap_cn or GameBiz.nap_bilibili => $"https://sdk.mihoyo.com/nap/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=nap_cn&channel_id=1&game=nap&game_biz=nap_cn&lang=zh-cn&level=60&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}&version=2.27",
            GameBiz.nap_global => $"https://sdk.hoyoverse.com/nap/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=nap_global&channel_id=1&game=nap&game_biz=nap_global&lang={lang}&level=60&platform=pc&region=prod_gf_jp&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}&version=2.27",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
    }



    public async Task<bool> IsNoticeAlertAsync(GameBiz biz, long uid, string? lang = null, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        string url = biz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.clgm_cn or GameBiz.hk4e_bilibili => $"https://hk4e-ann-api.mihoyo.com/common/hk4e_cn/announcement/api/getAlertAnn?bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&uid={uid}",
            GameBiz.hk4e_global => $"https://sg-hk4e-api.hoyoverse.com/common/hk4e_global/announcement/api/getAlertAnn?game=hk4e&game_biz=hk4e_global&lang={lang}&bundle_id=hk4e_global&channel_id=1&level=60&platform=pc&region=os_asia&uid={uid}",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => $"https://hkrpg-ann-api.mihoyo.com/common/hkrpg_cn/announcement/api/getAlertAnn?bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&uid={uid}",
            GameBiz.hkrpg_global => $"https://sg-hkrpg-api.hoyoverse.com/common/hkrpg_global/announcement/api/getAlertAnn?bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&uid={uid}",
            GameBiz.bh3_cn => $"https://ann-api.mihoyo.com/common/bh3_cn/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_cn&lang={lang}&bundle_id=bh3_cn&platform=pc&region=android01&level=88&channel_id=1&uid={uid}",
            GameBiz.bh3_global or GameBiz.bh3_jp or GameBiz.bh3_kr or GameBiz.bh3_os or GameBiz.bh3_asia => $"https://sg-public-api.hoyoverse.com/common/bh3_global/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_global&lang={lang}&bundle_id=bh3_os&platform=pc&region=overseas01&level=88&channel_id=1&uid={uid}",
            GameBiz.nap_cn or GameBiz.hk4e_bilibili => $"https://announcement-api.mihoyo.com/common/nap_cn/announcement/api/getAlertAnn?bundle_id=nap_cn&channel_id=1&game=nap&game_biz=nap_cn&lang=zh-cn&level=60&platform=pc&region=prod_gf_cn&uid={uid}",
            GameBiz.nap_global => $"https://sg-announcement-api.hoyoverse.com/common/nap_global/announcement/api/getAlertAnn?bundle_id=nap_global&channel_id=1&game=nap&game_biz=nap_global&lang={lang}&level=60&platform=pc&region=prod_gf_jp&uid={uid}",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var alertAnn = await CommonSendAsync<AlertAnn>(request, cancellationToken);
        return alertAnn.Remind || alertAnn.ExtraRemind;
    }








}
