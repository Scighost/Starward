using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Starward.Core.HoYoPlay;

public class HoYoPlayClient
{

    private readonly HttpClient _httpClient;




    public HoYoPlayClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version20 };
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





    private static string BuildUrl(string api, string launcherId, string language)
    {
        language = LanguageUtil.FilterLanguage(language);
        return launcherId switch
        {
            LauncherId.ChinaOfficial => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/{api}?launcher_id=jGHBHlcOq1&language={language}",
            LauncherId.GlobalOfficial => $"https://sg-hyp-api.hoyoverse.com/hyp/hyp-connect/api/{api}?launcher_id=VYTpXlbWo8&language={language}",
            LauncherId.BilibiliGenshin => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/{api}?launcher_id=umfgRO5gh5&language={language}",
            LauncherId.BilibiliStarRail => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/{api}?launcher_id=6P5gHMNyK3&language={language}",
            LauncherId.BilibiliZZZ => $"https://hyp-api.mihoyo.com/hyp/hyp-connect/api/{api}?launcher_id=xV0f4r1GT0&language={language}",
            _ => throw new ArgumentOutOfRangeException(nameof(launcherId), "Unknown launcher id."),
        };
    }



    /// <summary>
    /// 游戏信息（包括游戏 ID、名称、图标、背景图等）
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameInfo>> GetGameInfoAsync(string launcherId, string language, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGames", launcherId, language);
        return await CommonGetAsync<List<GameInfo>>(url, "games", cancellationToken);
    }



    /// <summary>
    /// 版本背景图和版本亮点
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameBackgroundInfo>> GetGameBackgroundAsync(string launcherId, string language, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getAllGameBasicInfo", launcherId, language);
        return await CommonGetAsync<List<GameBackgroundInfo>>(url, "game_info_list", cancellationToken);
    }



    /// <summary>
    /// 版本背景图和版本亮点
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="miHoYoApiException"></exception>
    public async Task<GameBackgroundInfo?> GetGameBackgroundAsync(string launcherId, string language, GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getAllGameBasicInfo", launcherId, language) + $"&game_id={gameId.Id}";
        var list = await CommonGetAsync<List<GameBackgroundInfo>>(url, "game_info_list", cancellationToken);
        return list.FirstOrDefault(x => x.GameId == gameId);
    }




    /// <summary>
    /// 轮播图、资讯、媒体标签
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<GameContent> GetGameContentAsync(string launcherId, string language, GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameContent", launcherId, language) + $"&game_id={gameId.Id}";
        return await CommonGetAsync<GameContent>(url, "content", cancellationToken);
    }



    /// <summary>
    /// 游戏安装包
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GamePackage>> GetGamePackageAsync(string launcherId, string language, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGamePackages", launcherId, language);
        return await CommonGetAsync<List<GamePackage>>(url, "game_packages", cancellationToken);
    }



    /// <summary>
    /// 游戏安装包
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GamePackage>> GetGamePackageAsync(string launcherId, string language, IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGamePackages", launcherId, language);
        foreach (var gameId in gameIds)
        {
            url += $"&game_ids[]={gameId.Id}";
        }
        return await CommonGetAsync<List<GamePackage>>(url, "game_packages", cancellationToken);
    }



    /// <summary>
    /// 游戏安装包
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="miHoYoApiException"></exception>
    public async Task<GamePackage?> GetGamePackageAsync(string launcherId, string language, GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGamePackages", launcherId, language) + $"&game_ids[]={gameId.Id}";
        var list = await CommonGetAsync<List<GamePackage>>(url, "game_packages", cancellationToken);
        return list.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameChannelSDK>> GetGameChannelSDKAsync(string launcherId, string language, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameChannelSDKs", launcherId, language);
        if (LauncherId.IsBilibili(launcherId))
        {
            url += "&channel=14&sub_channel=0";
        }
        else
        {
            url += "&channel=1&sub_channel=1";
        }
        return await CommonGetAsync<List<GameChannelSDK>>(url, "game_channel_sdks", cancellationToken);
    }



    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameChannelSDK>> GetGameChannelSDKAsync(string launcherId, string language, IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameChannelSDKs", launcherId, language);
        foreach (var gameId in gameIds)
        {
            url += $"&game_ids[]={gameId.Id}";
        }
        if (LauncherId.IsBilibili(launcherId))
        {
            url += "&channel=14&sub_channel=0";
        }
        else
        {
            url += "&channel=1&sub_channel=1";
        }
        return await CommonGetAsync<List<GameChannelSDK>>(url, "game_channel_sdks", cancellationToken);
    }


    /// <summary>
    /// 渠道服 SDK
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="miHoYoApiException"></exception>
    public async Task<GameChannelSDK?> GetGameChannelSDKAsync(string launcherId, string language, GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameChannelSDKs", launcherId, language) + $"&game_ids[]={gameId.Id}";
        if (LauncherId.IsBilibili(launcherId))
        {
            url += "&channel=14&sub_channel=0";
        }
        else
        {
            url += "&channel=1&sub_channel=1";
        }
        var list = await CommonGetAsync<List<GameChannelSDK>>(url, "game_channel_sdks", cancellationToken);
        return list.FirstOrDefault(x => x.GameId == gameId);
    }



    /// <summary>
    /// 需要删除的文件
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameDeprecatedFileConfig>> GetGameDeprecatedFileConfigAsync(string launcherId, string language, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameDeprecatedFileConfigs", launcherId, language);
        if (LauncherId.IsBilibili(launcherId))
        {
            url += "&channel=14&sub_channel=0";
        }
        else
        {
            url += "&channel=1&sub_channel=1";
        }
        return await CommonGetAsync<List<GameDeprecatedFileConfig>>(url, "deprecated_file_configs", cancellationToken);
    }



    /// <summary>
    /// 需要删除的文件
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameDeprecatedFileConfig>> GetGameDeprecatedFileConfigAsync(string launcherId, string language, IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameDeprecatedFileConfigs", launcherId, language);
        foreach (var gameId in gameIds)
        {
            url += $"&game_ids[]={gameId.Id}";
        }
        if (LauncherId.IsBilibili(launcherId))
        {
            url += "&channel=14&sub_channel=0";
        }
        else
        {
            url += "&channel=1&sub_channel=1";
        }
        return await CommonGetAsync<List<GameDeprecatedFileConfig>>(url, "deprecated_file_configs", cancellationToken);
    }


    /// <summary>
    /// 需要删除的文件
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="miHoYoApiException"></exception>
    public async Task<GameDeprecatedFileConfig?> GetGameDeprecatedFileConfigAsync(string launcherId, string language, GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameDeprecatedFileConfigs", launcherId, language) + $"&game_ids[]={gameId.Id}";
        if (LauncherId.IsBilibili(launcherId))
        {
            url += "&channel=14&sub_channel=0";
        }
        else
        {
            url += "&channel=1&sub_channel=1";
        }
        var list = await CommonGetAsync<List<GameDeprecatedFileConfig>>(url, "deprecated_file_configs", cancellationToken);
        return list.FirstOrDefault(x => x.GameId == gameId);
    }



    /// <summary>
    /// 游戏配置
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameConfig>> GetGameConfigAsync(string launcherId, string language, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameConfigs", launcherId, language);
        return await CommonGetAsync<List<GameConfig>>(url, "launch_configs", cancellationToken);
    }



    /// <summary>
    /// 游戏配置
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameConfig>> GetGameConfigAsync(string launcherId, string language, IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameConfigs", launcherId, language);
        foreach (var gameId in gameIds)
        {
            url += $"&game_ids[]={gameId.Id}";
        }
        return await CommonGetAsync<List<GameConfig>>(url, "launch_configs", cancellationToken);
    }


    /// <summary>
    /// 游戏配置
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="miHoYoApiException"></exception>
    public async Task<GameConfig?> GetGameConfigAsync(string launcherId, string language, GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameConfigs", launcherId, language) + $"&game_ids[]={gameId.Id}";
        var list = await CommonGetAsync<List<GameConfig>>(url, "launch_configs", cancellationToken);
        return list.FirstOrDefault(x => x.GameId == gameId);
    }



    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameBranch>> GetGameBranchAsync(string launcherId, string language, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameBranches", launcherId, language);
        return await CommonGetAsync<List<GameBranch>>(url, "game_branches", cancellationToken);
    }



    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameIds"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameBranch>> GetGameBranchAsync(string launcherId, string language, IEnumerable<GameId> gameIds, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameBranches", launcherId, language);
        foreach (var gameId in gameIds)
        {
            url += $"&game_ids[]={gameId.Id}";
        }
        return await CommonGetAsync<List<GameBranch>>(url, "game_branches", cancellationToken);
    }



    /// <summary>
    /// Chunk 下载模式的正式和预下载分支
    /// </summary>
    /// <param name="launcherId"></param>
    /// <param name="language"></param>
    /// <param name="gameId"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="miHoYoApiException"></exception>
    public async Task<GameBranch?> GetGameBranchAsync(string launcherId, string language, GameId gameId, CancellationToken cancellationToken = default)
    {
        string url = BuildUrl("getGameBranches", launcherId, language) + $"&game_ids[]={gameId.Id}";
        var list = await CommonGetAsync<List<GameBranch>>(url, "game_branches", cancellationToken);
        return list.FirstOrDefault(x => x.GameId == gameId);
    }


    /// <summary>
    /// Chunk 下载模式文件清单
    /// </summary>
    /// <param name="gameBranch"></param>
    /// <param name="gameBranchPackage"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public async Task<GameSophonChunkBuild> GetGameChunkBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage, CancellationToken cancellationToken = default)
    {
        string? url = null;
        if (gameBranch.GameId.GameBiz.IsChinaServer())
        {
            url = "https://downloader-api.mihoyo.com/downloader/sophon_chunk/api/getBuild?";
        }
        if (gameBranch.GameId.GameBiz.IsGlobalServer())
        {
            url = "https://sg-downloader-api.hoyoverse.com/downloader/sophon_chunk/api/getBuild?";
        }
        if (url is null)
        {
            throw new ArgumentOutOfRangeException(nameof(gameBranch), $"Unknown game biz ({gameBranch.GameId.GameBiz}).");
        }
        url += $"branch={gameBranchPackage.Branch}&package_id={gameBranchPackage.PackageId}&password={gameBranchPackage.Password}";
        return await CommonGetAsync<GameSophonChunkBuild>(url, cancellationToken);
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
    public async Task<GameSophonChunkBuild> GetGameSophonChunkBuildAsync(GameBranch gameBranch, GameBranchPackage gameBranchPackage, string version, CancellationToken cancellationToken = default)
    {
        string? url = null;
        if (gameBranch.GameId.GameBiz.IsChinaServer())
        {
            url = "https://downloader-api.mihoyo.com/downloader/sophon_chunk/api/getBuild?";
        }
        if (gameBranch.GameId.GameBiz.IsGlobalServer())
        {
            url = "https://sg-downloader-api.hoyoverse.com/downloader/sophon_chunk/api/getBuild?";
        }
        if (url is null)
        {
            throw new ArgumentOutOfRangeException(nameof(gameBranch), $"Unknown game biz ({gameBranch.GameId.GameBiz}).");
        }
        url += $"branch={gameBranchPackage.Branch}&package_id={gameBranchPackage.PackageId}&password={gameBranchPackage.Password}&tag={version}";
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
        string? url = null;
        if (gameBranch.GameId.GameBiz.IsChinaServer())
        {
            url = "https://downloader-api.mihoyo.com/downloader/sophon_chunk/api/getPatchBuild?";
        }
        if (gameBranch.GameId.GameBiz.IsGlobalServer())
        {
            url = "https://sg-downloader-api.hoyoverse.com/downloader/sophon_chunk/api/getPatchBuild?";
        }
        if (url is null)
        {
            throw new ArgumentOutOfRangeException(nameof(gameBranch), $"Unknown game biz ({gameBranch.GameId.GameBiz}).");
        }
        url += $"branch={gameBranchPackage.Branch}&package_id={gameBranchPackage.PackageId}&password={gameBranchPackage.Password}";
        var request = new HttpRequestMessage(HttpMethod.Post, url);
        return await CommonSendAsync<GameSophonPatchBuild>(request, cancellationToken);
    }




}
