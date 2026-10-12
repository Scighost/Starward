using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Starward.Core.HoYoPlay;

public class HoYoPlayClient
{

    private readonly HttpClient _httpClient;

    public required LauncherConfig LauncherConfig { get; set; }

    public string Language { get; set => field = LanguageUtil.FilterLanguage(value); } = "en-us";


    public HoYoPlayClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
    }



    private async Task<T> CommonGetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), HoYoPlayJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
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
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<JsonNode>), HoYoPlayJsonContext.Default, cancellationToken) as miHoYoApiWrapper<JsonNode>;
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
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<T>), HoYoPlayJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
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
        var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var responseData = await response.Content.ReadFromJsonAsync(typeof(miHoYoApiWrapper<JsonNode>), HoYoPlayJsonContext.Default, cancellationToken) as miHoYoApiWrapper<JsonNode>;
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
        string url = LauncherConfig.Host switch
        {
            "mihoyo" => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/{api}?launcher_id={LauncherConfig.Id}&language={Language}",
            "hoyoverse" => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/{api}?launcher_id={LauncherConfig.Id}&language={Language}",
            _ => throw new ArgumentOutOfRangeException(nameof(LauncherConfig.Host), "Unknown host."),
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
        return LauncherConfig.Host switch
        {
            "mihoyo" => $"https://downloader-api.mihoyo.com/downloader/sophon_chunk/api/{api}?",
            "hoyoverse" => $"https://sg-downloader-api.hoyoverse.com/downloader/sophon_chunk/api/{api}?",
            _ => throw new ArgumentOutOfRangeException(nameof(LauncherConfig.Host), "Unknown host."),
        };
    }



    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameInfo>> GetGameInfoAsync(CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGames");
        return await CommonGetAsync<List<GameInfo>>(url, "games", cancellationToken);
    }


    /// <summary>
    /// 版本背景图和版本亮点
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameBackgroundInfo>> GetGameBackgroundAsync(CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getAllGameBasicInfo");
        return await CommonGetAsync<List<GameBackgroundInfo>>(url, "game_info_list", cancellationToken);
    }


    /// <summary>
    /// 轮播图、资讯、媒体标签
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<GameContent> GetGameContentAsync(GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameContent") + $"&game_id={gameId.Id}";
        return await CommonGetAsync<GameContent>(url, "content", cancellationToken);
    }


    /// <summary>
    /// 游戏安装包
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GamePackage>> GetGamePackageAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGamePackages", gameIds);
        return await CommonGetAsync<List<GamePackage>>(url, "game_packages", cancellationToken);
    }


    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameChannelSDK>> GetGameChannelSDKAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameChannelSDKs", gameIds, true);
        return await CommonGetAsync<List<GameChannelSDK>>(url, "game_channel_sdks", cancellationToken);
    }


    /// <summary>
    /// 需要删除的文件
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameDeprecatedFileConfig>> GetGameDeprecatedFileConfigAsync(IEnumerable<GameId>? gameIds = null, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameDeprecatedFileConfigs", gameIds, true);
        return await CommonGetAsync<List<GameDeprecatedFileConfig>>(url, "deprecated_file_configs", cancellationToken);
    }


    /// <summary>
    /// 游戏配置
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameConfig>> GetGameConfigAsync(IEnumerable<GameId>? gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameConfigs", gameIds);
        return await CommonGetAsync<List<GameConfig>>(url, "launch_configs", cancellationToken);
    }


    /// <summary>
    /// 获取游戏扫描信息，不同版本exe的md5
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameScanInfo>> GetGameScanInfosAsync(IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameScanInfo", gameIds);
        return await CommonGetAsync<List<GameScanInfo>>(url, "game_scan_info", cancellationToken);
    }


    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameBranch>> GetGameBranchAsync(IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGameBranches", gameIds);
        return await CommonGetAsync<List<GameBranch>>(url, "game_branches", cancellationToken);
    }


    /// <summary>
    /// Chunk 下载模式文件清单
    /// </summary>
    /// <param name="gameBranch"></param>
    /// <param name="gameBranchPackage"></param>
    /// <param name="version"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<GameSophonChunkBuild> GetGameSophonChunkBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage, string? version = null, CancellationToken cancellationToken = default)
    {
        string url = BuildSophonUrl("getBuild") + $"branch={gameBranchPackage.Branch}&package_id={gameBranchPackage.PackageId}&password={gameBranchPackage.Password}";
        if (version is not null)
        {
            url += $"&tag={version}";
        }
        return await CommonGetAsync<GameSophonChunkBuild>(url, cancellationToken);
    }


    /// <summary>
    /// Chunk 下载模式的增量更新补丁文件清单
    /// </summary>
    /// <param name="gameBranch"></param>
    /// <param name="gameBranchPackage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<GameSophonPatchBuild> GetGameSophonPatchBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage, CancellationToken cancellationToken = default)
    {
        string url = BuildSophonUrl("getPatchBuild") + $"branch={gameBranchPackage.Branch}&package_id={gameBranchPackage.PackageId}&password={gameBranchPackage.Password}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        return await CommonSendAsync<GameSophonPatchBuild>(request, cancellationToken);
    }


    /// <summary>
    /// WPF Package
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<WPFPackageInfo>> GetWPFPackagesAsync(IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getWPFPackages", gameIds);
        return await CommonGetAsync<List<WPFPackageInfo>>(url, "wpf_packages", cancellationToken);
    }


    /// <summary>
    /// 获取 DirectX 配置
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="gpuInfos"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameDXConfig>> GetDXConfigsAsync(IEnumerable<GameId> gameIds, IEnumerable<GPUInfo> gpuInfos, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getDXConfigs");
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new GetDXConfigsRequest
            {
                LauncherId = LauncherConfig.Id,
                GameIds = gameIds.Select(x => x.Id).ToList(),
                Language = LanguageUtil.FilterLanguage(Language),
                GPUInfo = gpuInfos.ToList(),
            }, HoYoPlayJsonContext.Default.GetDXConfigsRequest)
        };
        GetDXConfigsResponse response = await CommonSendAsync<GetDXConfigsResponse>(request, cancellationToken);
        return response.DXConfigs;
    }


    /// <summary>
    /// 游戏插件
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GamePluginRelease>> GetGamePluginsAsync(IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildHypUrl("getGamePlugins", gameIds);
        return await CommonGetAsync<List<GamePluginRelease>>(url, "plugin_releases", cancellationToken);
    }


    /// <summary>
    /// 游戏预约页面
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    public async Task<GameReservationContent> GetGameReservationContentAsync(GameId gameId, CancellationToken cancellation = default)
    {
        string url = BuildHypUrl("getGameReservationContent") + $"game_id={gameId.Id}";
        return await CommonGetAsync<GameReservationContent>(url, cancellation);
    }


}
