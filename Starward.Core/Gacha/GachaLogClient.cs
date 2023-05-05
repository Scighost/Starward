using Microsoft.Win32;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha;

public class GachaLogClient
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




    public GachaLogClient(string? lang = null, HttpClient? httpClient = null)
    {
        Language = lang;
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
    }





    public async Task<int> GetUidByGachaUrlAsync(string gachaUrl)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl);
        var param = new GachaLogQueryParam(GachaType.StellarWarp, 1, 1, 0);
        var list = await GetGachaLogByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        param.GachaType = GachaType.CharacterEventWarp;
        list = await GetGachaLogByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        param.GachaType = GachaType.LightConeEventWarp;
        list = await GetGachaLogByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        param.GachaType = GachaType.DepartureWarp;
        list = await GetGachaLogByQueryParamAsync(prefix, param);
        if (list.Any())
        {
            return list.First().Uid;
        }
        return 0;
    }




    public async Task<List<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, IProgress<(GachaType GachaType, int Page)>? progress = null)
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl);
        var result = new List<GachaLogItem>();
        result.AddRange(await GetGachaLogInternelAsync(prefix, GachaType.StellarWarp, endId, progress));
        result.AddRange(await GetGachaLogInternelAsync(prefix, GachaType.DepartureWarp, endId, progress));
        result.AddRange(await GetGachaLogInternelAsync(prefix, GachaType.CharacterEventWarp, endId, progress));
        result.AddRange(await GetGachaLogInternelAsync(prefix, GachaType.LightConeEventWarp, endId, progress));
        return result;
    }




    public async Task<List<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaType gachaType, long endId = 0, IProgress<(GachaType GachaType, int Page)>? progress = null)
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl);
        return await GetGachaLogInternelAsync(prefix, gachaType, endId, progress);
    }






    private async Task<List<GachaLogItem>> GetGachaLogInternelAsync(string prefix, GachaType gachaType, long endId = 0, IProgress<(GachaType GachaType, int Page)>? progress = null)
    {
        var param = new GachaLogQueryParam(gachaType, 1, 20, 0);
        var result = new List<GachaLogItem>();
        while (true)
        {
            progress?.Report((gachaType, param.Page));
            var list = await GetGachaLogByQueryParamAsync(prefix, param);
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





    private async Task<List<GachaLogItem>> GetGachaLogByQueryParamAsync(string gachaUrlPrefix, GachaLogQueryParam param)
    {
        await Task.Delay(Random.Shared.Next(200, 300));
        var url = $"{gachaUrlPrefix}&{param}";
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(MihoyoApiWrapper<GachaLogResult>), GachaLogJsonContext.Default) as MihoyoApiWrapper<GachaLogResult>;
        if (wrapper is null)
        {
            return new List<GachaLogItem>();
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




    private string GetGachaUrlPrefix(string gachaUrl)
    {
        var match = Regex.Match(gachaUrl, @"(https://webstatic[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var auth = gachaUrl.Substring(gachaUrl.IndexOf('?'));
            if (gachaUrl.Contains("webstatic-sea"))
            {
                gachaUrl = API_PREFIX_OS + auth;
            }
            else
            {
                gachaUrl = API_PREFIX_CN + auth;
            }
            if (!string.IsNullOrWhiteSpace(Language))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={Language}");
            }
            return gachaUrl;
        }
        match = Regex.Match(gachaUrl, @"(https://api[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            gachaUrl = Regex.Replace(gachaUrl, @"&gacha_type=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&page=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&size=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&end_id=\d", "");
            if (!string.IsNullOrWhiteSpace(Language))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={Language}");
            }
            return gachaUrl;
        }
        throw new ArgumentException(nameof(gachaUrl));
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






    public static string? GetGachaUrlFromWebCache(string installPath)
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
