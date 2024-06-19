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



    public async Task<ContentWrapper> GetLauncherContentAsync(GameBiz biz, string? lang = null, CancellationToken cancellationToken = default)
    {
        lang = LanguageUtil.FilterLanguage(lang);
        var url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud or GameBiz.hk4e_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=jGHBHlcOq1&game_id=1Z8W5NHUQb",
            GameBiz.hk4e_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=gopR6Cufr3&language=en-us",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=jGHBHlcOq1&game_id=64kMb5iAWu",
            GameBiz.hkrpg_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=4ziysqXOQ8&language=en-us",
            GameBiz.bh3_cn => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=jGHBHlcOq1&game_id=osvnlOc0S8",
            GameBiz.bh3_overseas => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language={lang}", // TODO: Waiting to have data and then testing
            GameBiz.bh3_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=en-us", // TODO: Waiting to have data and then testing
            GameBiz.bh3_tw => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=zh-tw", // TODO: Waiting to have data and then testing
            GameBiz.bh3_kr => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=ko-kr", // TODO: Waiting to have data and then testing
            GameBiz.bh3_jp => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=ja-jp", // TODO: Waiting to have data and then testing
            GameBiz.nap_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameContent?launcher_id=jGHBHlcOq1&game_id=x6znKlJ0xK",
            //GameBiz.nap_global => "https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameContent?launcher_id=VYTpXlbWo8&game_id=U5hbdsT9W7",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        /*if (biz is GameBiz.nap_cn)
        {
            return await GetZZZCBT3LauncherContentAsync(cancellationToken);
        }
        else
        {*/
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var wrapper = await CommonSendAsync<LauncherContent>(request, cancellationToken);
            return wrapper.Content;
        //}
    }


    /*public async Task<LauncherContent> GetZZZCBT3LauncherContentAsync(CancellationToken cancellationToken = default)
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
    }*/



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


    public async Task<string> GetBackgroundAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=jGHBHlcOq1&game_id=1Z8W5NHUQb",
            GameBiz.hk4e_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=gopR6Cufr3&language=en-us",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=jGHBHlcOq1&game_id=64kMb5iAWu",
            GameBiz.hkrpg_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=4ziysqXOQ8&language=en-us",
            GameBiz.bh3_cn => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=jGHBHlcOq1&game_id=osvnlOc0S8",
            GameBiz.bh3_overseas => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=", // TODO: Fill in the new API
            GameBiz.bh3_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=en-us",
            GameBiz.bh3_tw => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=zh-tw",
            GameBiz.bh3_kr => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=ko-kr",
            GameBiz.bh3_jp => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=5TIVvvcwtM&language=ja-jp",
            GameBiz.nap_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=jGHBHlcOq1&game_id=x6znKlJ0xK",
            //GameBiz.nap_global => "https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getAllGameBasicInfo?launcher_id=VYTpXlbWo8&game_id=U5hbdsT9W7",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var node = await CommonSendAsync<LauncherBasicInfo>(request, cancellationToken);
        return node.BasicInfo.First().Backgrounds.First().BackgroundImage.Url;
    }


    /*public async Task<string> GetZZZCBT3BackgroundAsync(GameBiz biz, CancellationToken cancellationToken = default)
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
    }*/



    public async Task<GamePackagesWrapper> GetLauncherGameResourceAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?launcher_id=jGHBHlcOq1&game_ids[]=1Z8W5NHUQb",
            GameBiz.hk4e_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?launcher_id=VYTpXlbWo8&game_ids[]=gopR6Cufr3",
            GameBiz.hk4e_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?launcher_id=umfgRO5gh5",
            GameBiz.hkrpg_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?launcher_id=jGHBHlcOq1&game_ids[]=64kMb5iAWu",
            GameBiz.hkrpg_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?launcher_id=VYTpXlbWo8&game_ids[]=4ziysqXOQ8",
            GameBiz.hkrpg_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?launcher_id=6P5gHMNyK3",
            GameBiz.bh3_cn => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?launcher_id=jGHBHlcOq1&game_ids[]=osvnlOc0S8",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=tEGNtVhN&launcher_id=9&sub_channel_id=1",// TODO: Fill in the new API
            GameBiz.bh3_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?launcher_id=VYTpXlbWo8&game_ids[]=5TIVvvcwtM", // TODO: Waiting to have data and then testing
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=demhUTcW&launcher_id=8&sub_channel_id=1",// TODO: Fill in the new API
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=PRg571Xh&launcher_id=11&sub_channel_id=1",// TODO: Fill in the new API
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=ojevZ0EyIyZNCy4n&launcher_id=19&sub_channel_id=6",// TODO: Fill in the new API
            GameBiz.nap_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?launcher_id=jGHBHlcOq1&game_ids[]=x6znKlJ0xK", // TODO: Waiting to have data and then testing
            //GameBiz.nap_global => "https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGamePackages?launcher_id=VYTpXlbWo8&game_ids[]=U5hbdsT9W7", // TODO: Waiting to have data and then testing
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var resource = (await CommonSendAsync<LauncherGameResource>(request, cancellationToken)).Resources.First();
        // 这个if块好像没啥用了
        if (biz is GameBiz.hkrpg_global)
        {
            if (string.IsNullOrWhiteSpace(resource.Main.Major.ResListUrl) && !string.IsNullOrWhiteSpace(resource.Main.Major.GamePkgs.First().Url))
            {
                string path = resource.Main.Major.GamePkgs.First().Url;
                resource.Main.Major.ResListUrl = path[..path.LastIndexOf('/')] + "/unzip";
            }
        }
        return resource;
    }


    public async Task<GameDeprecatedFilesWrapper?> GetLauncherGameDeprecatedFilesAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.hk4e_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=jGHBHlcOq1&game_ids[]=1Z8W5NHUQb",
            GameBiz.hk4e_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=VYTpXlbWo8&game_ids[]=gopR6Cufr3",
            GameBiz.hk4e_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=umfgRO5gh5",
            GameBiz.hkrpg_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=jGHBHlcOq1&game_ids[]=64kMb5iAWu",
            GameBiz.hkrpg_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=VYTpXlbWo8&game_ids[]=4ziysqXOQ8",
            GameBiz.hkrpg_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=6P5gHMNyK3",
            GameBiz.bh3_cn => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=jGHBHlcOq1&game_ids[]=osvnlOc0S8",
            GameBiz.bh3_overseas => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=tEGNtVhN&launcher_id=9&sub_channel_id=1",// TODO: Fill in the new API
            GameBiz.bh3_global => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=VYTpXlbWo8&game_ids[]=5TIVvvcwtM", // TODO: Waiting to have data and then testing
            GameBiz.bh3_tw => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=demhUTcW&launcher_id=8&sub_channel_id=1",// TODO: Fill in the new API
            GameBiz.bh3_kr => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=PRg571Xh&launcher_id=11&sub_channel_id=1",// TODO: Fill in the new API
            GameBiz.bh3_jp => $"https://sdk-os-static.mihoyo.com/bh3_global/mdk/launcher/api/resource?channel_id=1&key=ojevZ0EyIyZNCy4n&launcher_id=19&sub_channel_id=6",// TODO: Fill in the new API
            GameBiz.nap_cn => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=jGHBHlcOq1&game_ids[]=x6znKlJ0xK", // TODO: Waiting to have data and then testing
            //GameBiz.nap_global => "https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/getGameDeprecatedFileConfigs?launcher_id=VYTpXlbWo8&game_ids[]=U5hbdsT9W7", // TODO: Waiting to have data and then testing
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        return (await CommonSendAsync<LauncherGameDeprecatedFiles>(request, cancellationToken)).Resources.FirstOrDefault();
    }


    public async Task<GameSDK?> GetLauncherGameSdkAsync(GameBiz biz, CancellationToken cancellationToken = default)
    {
        var url = biz switch
        {
            GameBiz.hk4e_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameChannelSDKs?channel=14&launcher_id=umfgRO5gh5&sub_channel=0",
            GameBiz.hkrpg_bilibili => "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGameChannelSDKs?channel=14&launcher_id=6P5gHMNyK3&sub_channel=0",
            _ => null,
        };
        GameSDK? resource = null;
        if (!string.IsNullOrWhiteSpace(url))
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            resource = (await CommonSendAsync<LauncherGameSdk>(request, cancellationToken)).Sdk.First();
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
            //GameBiz.nap_global => $"",
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
            //GameBiz.nap_global => $"",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        var alertAnn = await CommonSendAsync<AlertAnn>(request, cancellationToken);
        return alertAnn.Remind || alertAnn.ExtraRemind;
    }




}
