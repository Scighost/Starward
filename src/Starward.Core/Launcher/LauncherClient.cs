using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;

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
        lang = LanguageUtil.FilterLanguage(lang);
        var url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud or GameBiz.hk4e_bilibili => "https://sdk-static.mihoyo.com/hk4e_cn/mdk/launcher/api/content?filter_adv=false&key=eYd89JmJ&language=zh-cn&launcher_id=18",
            GameBiz.hk4e_global => $"https://sdk-os-static.mihoyo.com/hk4e_global/mdk/launcher/api/content?filter_adv=false&key=gcStgarh&language={lang}&launcher_id=10",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/content?filter_adv=false&key=6KcVuOkbcqjJomjZ&language=zh-cn&launcher_id=33",
            GameBiz.hkrpg_global => $"https://hkrpg-launcher-static.hoyoverse.com/hkrpg_global/mdk/launcher/api/content?filter_adv=false&key=vplOVX8Vn7cwG8yb&language={lang}&launcher_id=35",
            GameBiz.bh3_cn => $"https://bh3-launcher-static.mihoyo.com/bh3_cn/mdk/launcher/api/content?key=SyvuPnqL&filter_adv=false&language=zh-cn&launcher_id=4",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=tEGNtVhN&language={lang}&launcher_id=9",
            GameBiz.bh3_global => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=dpz65xJ3&language={lang}&launcher_id=10",
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=demhUTcW&language=zh-tw&launcher_id=8",
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=PRg571Xh&language=ko-kr&launcher_id=11",
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/content?filter_adv=false&key=ojevZ0EyIyZNCy4n&language=ja-jp&launcher_id=19",
            GameBiz.nap_cn => "https://nap-launcher-static.mihoyo.com/nap_cn/mdk/launcher/api/content?filter_adv=false&key=9HEb62Pw0qKYX4Mw&language=zh-cn&launcher_id=15",
            //GameBiz.nap_global => "",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        if (biz is GameBiz.nap_cn)
        {
            return await GetZZZCBT3LauncherContentAsync(cancellationToken);
        }
        else
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            return await CommonSendAsync<LauncherContent>(request, cancellationToken);
        }
    }


    public async Task<LauncherContent> GetZZZCBT3LauncherContentAsync(CancellationToken cancellationToken = default)
    {
        const string url = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=PFKmM45gSW&game_id=ol93169Cmh&language=zh-cn";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var node = await CommonSendAsync<JsonNode>(request, cancellationToken);
        var content = new LauncherContent { Banner = [], Post = [] };
        foreach (JsonNode? item in node?["content"]?["banners"] as JsonArray ?? [])
        {
            string? img = item?["image"]?["url"]?.ToString()!;
            if (string.IsNullOrWhiteSpace(img))
            {
                continue;
            }
            var banner = new LauncherBanner
            {
                Img = img,
                BannerId = item?["id"]?.ToString()!,
                Url = item?["image"]?["link"]?.ToString()!
            };
            content.Banner.Add(banner);
        }
        foreach (var item in node?["content"]?["posts"] as JsonArray ?? [])
        {
            string? title = item?["title"]?.ToString()!;
            if (string.IsNullOrWhiteSpace(title))
            {
                continue;
            }
            var post = new LauncherPost
            {
                PostId = item?["id"]?.ToString()!,
                Type = item?["type"]?.ToString() switch
                {
                    "POST_TYPE_ACTIVITY" => PostType.POST_TYPE_ACTIVITY,
                    "POST_TYPE_ANNOUNCE" => PostType.POST_TYPE_ANNOUNCE,
                    "POST_TYPE_INFO" => PostType.POST_TYPE_INFO,
                    _ => PostType.POST_TYPE_ACTIVITY,
                },
                Title = title,
                Url = item?["link"]?.ToString()!,
                ShowTime = item?["date"]?.ToString()!
            };
            content.Post.Add(post);
        }
        return content;
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


    public async Task<string> GetZZZCBT3BackgroundAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.nap_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=PFKmM45gSW&language=zh-cn&game_id=ol93169Cmh",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var node = await CommonSendAsync<JsonNode>(request, cancellationToken);
        string? img = node?["game_info_list"]?[0]?["backgrounds"]?[0]?["background"]?["url"]?.ToString();
        return img ?? throw new miHoYoApiException(-1, "ZZZ CBT3 background image is null.");
    }



    public async Task<LauncherGameResource> GetLauncherGameResourceAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=1Z8W5NHUQb&launcher_id=jGHBHlcOq1",
            GameBiz.hk4e_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=gopR6Cufr3&launcher_id=VYTpXlbWo8",
            GameBiz.hk4e_bilibili => "https://hk4e-launcher-static.mihoyo.com/hk4e_cn/mdk/launcher/api/resource?channel_id=14&key=KAtdSsoQ&launcher_id=17&sub_channel_id=0",
            GameBiz.hkrpg_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=64kMb5iAWu&launcher_id=jGHBHlcOq1",
            GameBiz.hkrpg_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=4ziysqXOQ8&launcher_id=VYTpXlbWo8",
            GameBiz.hkrpg_bilibili => "https://api-launcher-static.mihoyo.com/hkrpg_cn/mdk/launcher/api/resource?channel_id=14&key=fSPJNRwFHRipkprW&launcher_id=28&sub_channel_id=0",
            GameBiz.bh3_cn => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=osvnlOc0S8&launcher_id=jGHBHlcOq1",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=tEGNtVhN&launcher_id=9&sub_channel_id=1",
            GameBiz.bh3_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=5TIVvvcwtM&launcher_id=VYTpXlbWo8",
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=demhUTcW&launcher_id=8&sub_channel_id=1",
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=PRg571Xh&launcher_id=11&sub_channel_id=1",
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=ojevZ0EyIyZNCy4n&launcher_id=19&sub_channel_id=6",
            GameBiz.nap_cn => "https://nap-launcher-static.mihoyo.com/nap_cn/mdk/launcher/api/resource?channel_id=1&key=9HEb62Pw0qKYX4Mw&launcher_id=15&sub_channel_id=1",
            //GameBiz.nap_global => "https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=VYTpXlbWo8&launcher_id=VYTpXlbWo8",
            //nap_global is what I guessed based on data package capture result
            //nap_global 是我抓包抓出来的 不保真
            //https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?game_ids[]=64kMb5iAWu&launcher_id=jGHBHlcOq1
            //https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?game_ids[]= &launcher_id=VYTpXlbWo8
            //以下是game_ids对应的游戏
            //GAME              game_ids
            //HI3_CN            osvnlOc0S8
            //HK4E_CN           1Z8W5NHUQb
            //HSR_CN            64kMb5iAWu
            //ZZZ_CN            x6znKlJ0xK
            //          GLOBAL
            //HI3_global        5TIVvvcwtM
            //HK4e_global       gopR6Cufr3
            //HSR_global        4ziysqXOQ8
            //ZZZ_global        VYTpXlbWo8
            //启动器ID           launcher_id
            //国服启动器ID       jGHBHlcOq1 2024/6/18 暂无更改
            //Launcher global   VYTpXlbWo8 2024/6/18 no changes
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var resource = await CommonSendAsync<LauncherGameResource>(request, cancellationToken);
        if (biz is GameBiz.hkrpg_global)
        {
            if (string.IsNullOrWhiteSpace(resource.Game.Latest.DecompressedPath) && !string.IsNullOrWhiteSpace(resource.Game.Latest.Path))
            {
                string path = resource.Game.Latest.Path;
                resource.Game.Latest.DecompressedPath = path[..path.LastIndexOf('/')] + "/unzip";
            }
        }
        return resource;
    }




    public static string GetGameNoticesUrl(GameBiz biz, long uid, string? lang = null)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        return biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud or GameBiz.hk4e_bilibili => $"https://webstatic.mihoyo.com/hk4e/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hk4e_global => $"https://sdk.hoyoverse.com/hk4e/announcement/index.html?announcement_version=1.37&auth_appid=announcement&bundle_id=hk4e_global&channel_id=1&game=hk4e&game_biz=hk4e_global&lang={lang}&level=60&platform=pc&region=os_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&uid={uid}",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => $"https://webstatic.mihoyo.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.hkrpg_global => $"https://sdk.hoyoverse.com/hkrpg/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_cn => $"https://webstatic.mihoyo.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_cn&channel_id=1&game=bh3&game_biz=bh3_cn&lang=zh-cn&level=88&platform=pc&region=android01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.bh3_global or GameBiz.bh3_jp or GameBiz.bh3_kr or GameBiz.bh3_overseas or GameBiz.bh3_tw => $"https://sdk.hoyoverse.com/bh3/announcement/index.html?auth_appid=announcement&authkey_ver=1&bundle_id=bh3_os&channel_id=1&game=bh3&game_biz=bh3_os&lang={lang}&level=88&platform=pc&region=overseas01&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&sign_type=2&uid={uid}",
            GameBiz.nap_cn => $"https://webstatic.mihoyo.com/nap/announcement/index.html?game=nap&game_biz=nap_cn&lang=zh-cn&bundle_id=nap_cn&channel_id=1&level=40&platform=pc&region=prod_cb01_cn&sdk_presentation_style=fullscreen&sdk_screen_transparent=true&uid={uid}",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
    }



    public async Task<bool> IsNoticesAlertAsync(GameBiz biz, long uid, string? lang = null, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        uid = uid == 0 ? 100000000 : uid;
        string url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud or GameBiz.hk4e_bilibili => $"https://hk4e-api.mihoyo.com/common/hk4e_cn/announcement/api/getAlertAnn?bundle_id=hk4e_cn&channel_id=1&game=hk4e&game_biz=hk4e_cn&lang={lang}&level=60&platform=pc&region=cn_gf01&uid={uid}",
            GameBiz.hk4e_global => $"https://sg-hk4e-api.hoyoverse.com/common/hk4e_global/announcement/api/getAlertAnn?game=hk4e&game_biz=hk4e_global&lang={lang}&bundle_id=hk4e_global&channel_id=1&level=60&platform=pc&region=os_asia&uid={uid}",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => $"https://hkrpg-api.mihoyo.com/common/hkrpg_cn/announcement/api/getAlertAnn?bundle_id=hkrpg_cn&channel_id=1&game=hkrpg&game_biz=hkrpg_cn&lang={lang}&level=70&platform=pc&region=prod_gf_cn&uid={uid}",
            GameBiz.hkrpg_global => $"https://sg-hkrpg-api.hoyoverse.com/common/hkrpg_global/announcement/api/getAlertAnn?bundle_id=hkrpg_global&channel_id=1&game=hkrpg&game_biz=hkrpg_global&lang={lang}&level=1&platform=pc&region=prod_official_asia&uid={uid}",
            GameBiz.bh3_cn => $"https://api-takumi.mihoyo.com/common/bh3_cn/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_cn&lang={lang}&bundle_id=bh3_cn&platform=pc&region=android01&level=88&channel_id=1&uid={uid}",
            GameBiz.bh3_global or GameBiz.bh3_jp or GameBiz.bh3_kr or GameBiz.bh3_overseas or GameBiz.bh3_tw => $"https://sg-public-api.hoyoverse.com/common/bh3_global/announcement/api/getAlertAnn?game=bh3&game_biz=bh3_global&lang={lang}&bundle_id=bh3_os&platform=pc&region=overseas01&level=88&channel_id=1&uid={uid}",
            GameBiz.nap_cn => $"https://announcement-api.mihoyo.com/common/nap_cn/announcement/api/getAlertAnn?game=nap&game_biz=nap_cn&lang=zh-cn&bundle_id=nap_cn&channel_id=1&level=40&platform=pc&region=prod_cb01_cn&uid={uid}",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var alertAnn = await CommonSendAsync<AlertAnn>(request, cancellationToken);
        return alertAnn.Remind || alertAnn.ExtraRemind;
    }




}
