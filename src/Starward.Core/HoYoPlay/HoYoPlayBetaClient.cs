using Starward.Core.Passport;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Starward.Core.HoYoPlay;

public class HoYoPlayBetaClient : IHoYoPlayClient
{

    private readonly HttpClient _httpClient;

    public required LauncherConfig LauncherConfig { get; set; }

    public string Language { get; set => field = LanguageUtil.FilterLanguage(value); } = "en-us";

    public PassportUserInfo? PassportUserInfo { get; set; }


    public HoYoPlayBetaClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
    }



    private async Task<T> CommonGetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), HoYoPlayJsonContext.Default, cancellationToken).ConfigureAwait(false) as miHoYoApiWrapper<T>;
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


    private async Task<T> CommonGetAsync<T>(string url, string node, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<JsonNode>), HoYoPlayJsonContext.Default, cancellationToken).ConfigureAwait(false) as miHoYoApiWrapper<JsonNode>;
        if (responseData is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        if (responseData.Retcode != 0)
        {
            throw new miHoYoApiException(responseData.Retcode, responseData.Message);
        }
        var data = JsonSerializer.Deserialize<T>(responseData.Data?[node], HoYoPlayJsonContext.Default.Options);
        if (data is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        return data;
    }


    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), HoYoPlayJsonContext.Default, cancellationToken).ConfigureAwait(false) as miHoYoApiWrapper<T>;
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


    private async Task<T> CommonSendAsync<T>(HttpRequestMessage request, string node, CancellationToken cancellationToken = default)
    {
        request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
        var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<JsonNode>), HoYoPlayJsonContext.Default, cancellationToken).ConfigureAwait(false) as miHoYoApiWrapper<JsonNode>;
        if (responseData is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        if (responseData.Retcode != 0)
        {
            throw new miHoYoApiException(responseData.Retcode, responseData.Message);
        }
        var data = JsonSerializer.Deserialize<T>(responseData.Data?[node], HoYoPlayJsonContext.Default.Options);
        if (data is null)
        {
            throw new miHoYoApiException(-1, "Can not parse the response body.");
        }
        return data;
    }



    private string BuildHypUrl(string api, IEnumerable<GameId>? gameIds = null, bool channel = false)
    {
        string url = LauncherConfig.Region switch
        {
            ServerRegion.miHoYo => $"https://hyp-api-beta.mihoyo.com/hyp/hyp-connect/api/{api}?launcher_id={LauncherConfig.Id}&language={Language}",
            ServerRegion.HoYoverse => $"https://sg-hyp-api-beta.hoyoverse.com/hyp/hyp-connect/api/{api}?launcher_id={LauncherConfig.Id}&language={Language}",
            _ => throw new NotSupportedException($"Server region '{LauncherConfig.Region}' is not supported."),
        };
        if (gameIds is not null)
        {
            foreach (var gameId in gameIds)
            {
                url += $"&game_ids[]={gameId.Id}";
            }
        }
        if (channel)
        {
            url += $"&channel={LauncherConfig.Channel}&sub_channel={LauncherConfig.SubChannel}";
        }
        return url;
    }


    private string BuildSophonUrl(string api)
    {
        return LauncherConfig.Region switch
        {
            ServerRegion.miHoYo => $"https://api-beta.mihoyo.com/downloader/sophon_chunk/api/{api}?",
            ServerRegion.HoYoverse => $"https://sg-beta-api.hoyoverse.com/downloader/sophon_chunk/api/{api}?",
            _ => throw new NotSupportedException($"Server region '{LauncherConfig.Region}' is not supported."),
        };
    }



    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameInfo>> GetGameInfoAsync(CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGames");
        return await CommonGetAsync<List<GameInfo>>(url, "games", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 版本背景图和版本亮点
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameBackgroundInfo>> GetGameBackgroundAsync(CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getAllGameBasicInfo");
        return await CommonGetAsync<List<GameBackgroundInfo>>(url, "game_info_list", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 轮播图、资讯、媒体标签
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<GameContent> GetGameContentAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameContent") + $"&game_id={gameId.Id}";
        return await CommonGetAsync<GameContent>(url, "content", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 游戏安装包
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GamePackage>> GetGamePackageAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGamePackagesBySToken", gameIds);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", $"mid={PassportUserInfo?.Mid}; stoken={PassportUserInfo?.Stoken}; stuid={PassportUserInfo?.Aid}");
        return await CommonSendAsync<List<GamePackage>>(request, "game_packages", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameChannelSDK>> GetGameChannelSDKAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameChannelSDKs", gameIds, true);
        return await CommonGetAsync<List<GameChannelSDK>>(url, "game_channel_sdks", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 需要删除的文件
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameDeprecatedFileConfig>> GetGameDeprecatedFileConfigAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameDeprecatedFileConfigs", gameIds, true);
        return await CommonGetAsync<List<GameDeprecatedFileConfig>>(url, "deprecated_file_configs", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 游戏配置
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameConfig>> GetGameConfigAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameConfigs", gameIds);
        return await CommonGetAsync<List<GameConfig>>(url, "launch_configs", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 获取游戏扫描信息，不同版本exe的md5
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameScanInfo>> GetGameScanInfosAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameScanInfo", gameIds);
        return await CommonGetAsync<List<GameScanInfo>>(url, "game_scan_info", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameBranch>> GetGameBranchAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameBranches", gameIds);
        return await CommonGetAsync<List<GameBranch>>(url, "game_branches", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Chunk 下载模式文件清单
    /// </summary>
    /// <param name="gameBranchPackage"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<GameSophonChunkBuild> GetGameSophonChunkBuildAsync(GameBranchPackage gameBranchPackage, string? version = null, CancellationToken cancellationToken = default)
    {
        string url = BuildSophonUrl("getBuildWithStokenLogin") + $"branch={gameBranchPackage.Branch}&package_id={gameBranchPackage.PackageId}&password={gameBranchPackage.Password}";
        if (version is not null)
        {
            url += $"&tag={version}";
        }
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", $"mid={PassportUserInfo?.Mid}; stoken={PassportUserInfo?.Stoken}; stuid={PassportUserInfo?.Aid}");
        return await CommonSendAsync<GameSophonChunkBuild>(request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// Chunk 下载模式的增量更新补丁文件清单
    /// </summary>
    /// <param name="gameBranchPackage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<GameSophonPatchBuild> GetGameSophonPatchBuildAsync(GameBranchPackage gameBranchPackage, CancellationToken cancellationToken = default)
    {
        string url = BuildSophonUrl("getPatchBuild") + $"branch={gameBranchPackage.Branch}&package_id={gameBranchPackage.PackageId}&password={gameBranchPackage.Password}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Add("Cookie", $"mid={PassportUserInfo?.Mid}; stoken={PassportUserInfo?.Stoken}; stuid={PassportUserInfo?.Aid}");
        return await CommonSendAsync<GameSophonPatchBuild>(request, cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// WPF Package
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<WPFPackageInfo>> GetWPFPackagesAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getWPFPackagesBySToken", gameIds);
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("Cookie", $"mid={PassportUserInfo?.Mid}; stoken={PassportUserInfo?.Stoken}; stuid={PassportUserInfo?.Aid}");
        return await CommonSendAsync<List<WPFPackageInfo>>(request, "wpf_packages", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 获取 DirectX 配置
    /// </summary>
    /// <param name="gpuInfos"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameDXConfig>> GetDXConfigsAsync(IEnumerable<GPUInfo> gpuInfos, IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        Version os = Environment.OSVersion.Version;
        string url = BuildHypUrl("getDXConfigs");
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new GetDXConfigsRequest
            {
                LauncherId = LauncherConfig.Id,
                GameIds = gameIds?.Select(x => x.Id).ToList(),
                Language = LanguageUtil.FilterLanguage(Language),
                GPUInfo = gpuInfos.ToList(),
                OSMajorVersion = os.Major.ToString(),
                OSMinorVersion = os.Minor.ToString(),
                OSBuildNumber = os.Build.ToString(),
            }, HoYoPlayJsonContext.Default.GetDXConfigsRequest)
        };
        GetDXConfigsResponse response = await CommonSendAsync<GetDXConfigsResponse>(request, cancellationToken).ConfigureAwait(false);
        return response.DXConfigs;
    }


    /// <summary>
    /// 游戏插件
    /// </summary>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GamePluginRelease>> GetGamePluginsAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGamePlugins", gameIds);
        return await CommonGetAsync<List<GamePluginRelease>>(url, "plugin_releases", cancellationToken).ConfigureAwait(false);
    }


    /// <summary>
    /// 游戏预约页面
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<GameReservationContent> GetGameReservationContentAsync(GameId gameId, CancellationToken cancellation = default)
    {
        string url = BuildHypUrl("getGameReservationContent") + $"&game_id={gameId.Id}";
        return await CommonGetAsync<GameReservationContent>(url, cancellation).ConfigureAwait(false);
    }


}
