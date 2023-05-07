using Microsoft.Win32;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Warp;

public class WarpRecordClient
{


    private const string REG_KEY_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩坏：星穹铁道";
    private const string REG_KEY_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Star Rail";

    private const string WEB_CACHE_PATH = @"StarRail_Data\webCaches\Cache\Cache_Data\data_2";

    private const string WEB_PREFIX_CN = "https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html";
    private const string WEB_PREFIX_OS = "https://webstatic-sea.hoyoverse.com/hkrpg/event/e20211215gacha-v2/index.html";

    private const string API_PREFIX_CN = "https://api-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";
    private const string API_PREFIX_OS = "https://api-os-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";


    private readonly HttpClient _httpClient;


    public string? Language { get; set; }




    public WarpRecordClient(string? lang = null, HttpClient? httpClient = null)
    {
        Language = lang;
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
    }





    public async Task<int> GetUidByWarpUrlAsync(string warpUrl)
    {
        var prefix = GetWarpUrlPrefix(warpUrl);
        var param = new WarpRecordQueryParam(WarpType.StellarWarp, 1, 1, 0);
        var list = await GetWarpRecordByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        param.WarpType = WarpType.CharacterEventWarp;
        list = await GetWarpRecordByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        param.WarpType = WarpType.LightConeEventWarp;
        list = await GetWarpRecordByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        param.WarpType = WarpType.DepartureWarp;
        list = await GetWarpRecordByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        return 0;
    }




    public async Task<List<WarpRecordItem>> GetWarpRecordAsync(string warpUrl, long endId = 0, IProgress<(WarpType WarpType, int Page)>? progress = null)
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetWarpUrlPrefix(warpUrl);
        var result = new List<WarpRecordItem>();
        result.AddRange(await GetWarpRecordAsyncInternal(prefix, WarpType.StellarWarp, endId, progress));
        result.AddRange(await GetWarpRecordAsyncInternal(prefix, WarpType.DepartureWarp, endId, progress));
        result.AddRange(await GetWarpRecordAsyncInternal(prefix, WarpType.CharacterEventWarp, endId, progress));
        result.AddRange(await GetWarpRecordAsyncInternal(prefix, WarpType.LightConeEventWarp, endId, progress));
        return result;
    }




    public async Task<List<WarpRecordItem>> GetWarpRecordAsync(string warpUrl, WarpType warpType, long endId = 0, IProgress<(WarpType WarpType, int Page)>? progress = null)
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetWarpUrlPrefix(warpUrl);
        return await GetWarpRecordAsyncInternal(prefix, warpType, endId, progress);
    }






    private async Task<List<WarpRecordItem>> GetWarpRecordAsyncInternal(string prefix, WarpType warpType, long endId = 0, IProgress<(WarpType WarpType, int Page)>? progress = null)
    {
        var param = new WarpRecordQueryParam(warpType, 1, 20, 0);
        var result = new List<WarpRecordItem>();
        while (true)
        {
            progress?.Report((warpType, param.Page));
            var list = await GetWarpRecordByQueryParamAsync(prefix, param);
            result.AddRange(list);
            if (list.Count == 20 && list.Last().Id > endId)
            {
                param.Page++;
                param.EndId = list.Last().Id;
            }
            else
            {
                break;
            }
        }
        return result;
    }





    private async Task<List<WarpRecordItem>> GetWarpRecordByQueryParamAsync(string warpUrlPrefix, WarpRecordQueryParam param)
    {
        await Task.Delay(Random.Shared.Next(200, 300));
        var url = $"{warpUrlPrefix}&{param}";
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(MihoyoApiWrapper<WarpRecordResult>), WarpRecordJsonContext.Default) as MihoyoApiWrapper<WarpRecordResult>;
        if (wrapper is null)
        {
            return new List<WarpRecordItem>();
        }
        else if (wrapper.Retcode != 0)
        {
            throw new MihoyoApiException(wrapper.Retcode, wrapper.Message);
        }
        else
        {
            return wrapper.Data.List;
        }
    }




    private string GetWarpUrlPrefix(string warpUrl)
    {
        var match = Regex.Match(warpUrl, @"(https://webstatic[!-z]+)");
        if (match.Success)
        {
            warpUrl = match.Groups[1].Value;
            var auth = warpUrl.Substring(warpUrl.IndexOf('?'));
            if (warpUrl.Contains("webstatic-sea"))
            {
                warpUrl = API_PREFIX_OS + auth;
            }
            else
            {
                warpUrl = API_PREFIX_CN + auth;
            }
            if (!string.IsNullOrWhiteSpace(Language))
            {
                warpUrl = Regex.Replace(warpUrl, @"&lang=[^&]+", $"&lang={Language}");
            }
            return warpUrl;
        }
        match = Regex.Match(warpUrl, @"(https://api[!-z]+)");
        if (match.Success)
        {
            warpUrl = match.Groups[1].Value;
            warpUrl = Regex.Replace(warpUrl, @"&gacha_type=\d", "");
            warpUrl = Regex.Replace(warpUrl, @"&page=\d", "");
            warpUrl = Regex.Replace(warpUrl, @"&size=\d", "");
            warpUrl = Regex.Replace(warpUrl, @"&end_id=\d", "");
            if (!string.IsNullOrWhiteSpace(Language))
            {
                warpUrl = Regex.Replace(warpUrl, @"&lang=[^&]+", $"&lang={Language}");
            }
            return warpUrl;
        }
        throw new ArgumentException(nameof(warpUrl));
    }






    /// <summary>
    /// 
    /// </summary>
    /// <param name="serverIndex">0 CN, 1 OS</param>
    /// <returns></returns>
    [SupportedOSPlatform("windows")]
    public static string? GetGameInstallPathFromRegistry(int serverIndex)
    {
        var key = serverIndex switch
        {
            0 => REG_KEY_CN,
            1 => REG_KEY_OS,
            _ => REG_KEY_CN,
        };
        var launcherPath = Registry.GetValue(key, "InstallPath", null) as string;
        var configPath = Path.Join(launcherPath, "config.ini");
        if (File.Exists(configPath))
        {
            var str = File.ReadAllText(configPath);
            var installPath = Regex.Match(str, @"game_install_path=(.+)").Groups[1].Value.Trim();
            if (Directory.Exists(installPath))
            {
                return Path.GetFullPath(installPath);
            }
        }
        return null;
    }






    public static string? GetWarpUrlFromWebCache(string installPath)
    {
        var file = Path.Join(installPath, WEB_CACHE_PATH);
        if (File.Exists(file))
        {
            using var fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            var ms = new MemoryStream();
            fs.CopyTo(ms);
            var span = ms.ToArray().AsSpan();
            var index = span.LastIndexOf("https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html"u8);
            if (index >= 0)
            {
                var length = span[index..].IndexOf("\0"u8);
                return Encoding.UTF8.GetString(span.Slice(index, length));
            }
            index = span.LastIndexOf("https://webstatic-sea.hoyoverse.com/hkrpg/event/e20211215gacha-v2/index.html"u8);
            if (index >= 0)
            {
                var length = span[index..].IndexOf("\0"u8);
                return Encoding.UTF8.GetString(span.Slice(index, length));
            }
        }
        return null;
    }










}
