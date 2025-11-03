using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha.Genshin;

public class GenshinBeyondGachaClient
{


    private const string WEB_CACHE_PATH_YS_CN = @"YuanShen_Data\webCaches\Cache\Cache_Data\data_2";
    private const string WEB_CACHE_PATH_YS_OS = @"GenshinImpact_Data\webCaches\Cache\Cache_Data\data_2";

    private static ReadOnlySpan<byte> SPAN_WEB_PREFIX_YS_CN => "https://webstatic.mihoyo.com/hk4e/event/e20250716gacha/index.html"u8;
    private static ReadOnlySpan<byte> SPAN_WEB_PREFIX_YS_OS => "https://gs.hoyoverse.com/genshin/event/e20250716gacha/index.html"u8;

    private const string API_PREFIX_YS_CN = "https://public-operation-hk4e.mihoyo.com/gacha_info/api/getBeyondGachaLog";
    private const string API_PREFIX_YS_OS = "https://public-operation-hk4e-sg.hoyoverse.com/gacha_info/api/getBeyondGachaLog";


    public IReadOnlyCollection<int> QueryGachaTypes { get; init; } = new int[] { 1000, 2000 }.ToList().AsReadOnly();



    private readonly HttpClient _httpClient;


    public GenshinBeyondGachaClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient();
    }





    public async Task<IEnumerable<GenshinBeyondGachaItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(int GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl, lang);
        var result = new List<GenshinBeyondGachaItem>();
        foreach (var gachaType in QueryGachaTypes)
        {
            result.AddRange(await GetGachaLogByTypeAsync(prefix, gachaType, endId, progress, cancellationToken));
        }
        return result;
    }



    protected async Task<List<GenshinBeyondGachaItem>> GetGachaLogByTypeAsync(string prefix, int gachaType, long endId = 0, IProgress<(int GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var param = new BeyondGachaLogQuery(gachaType, 1, 5, 0);
        var result = new List<GenshinBeyondGachaItem>();
        while (true)
        {
            progress?.Report((gachaType, param.Page));
            var list = await GetGachaLogByQueryAsync(prefix, param, cancellationToken);
            result.AddRange(list);
            if (list.Count == 5 && list.Last().Id > endId)
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



    protected string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
    {
        var match = Regex.Match(gachaUrl, @"(https://webstatic\.mihoyo\.com[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var auth = gachaUrl.Substring(gachaUrl.IndexOf('?')).Replace("#/log", "");
            gachaUrl = API_PREFIX_YS_CN + auth;
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={lang}");
            }
            else
            {
                lang = Regex.Match(gachaUrl, @"&lang=([^&]+)").Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    gachaUrl = Regex.Replace(gachaUrl, @"&lang=([^&]+)", $"&lang={LanguageUtil.FilterLanguage(lang)}");
                }
            }
            return gachaUrl;
        }
        match = Regex.Match(gachaUrl, @"(https://gs\.hoyoverse\.com[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var auth = gachaUrl.Substring(gachaUrl.IndexOf('?')).Replace("#/log", "");
            gachaUrl = API_PREFIX_YS_OS + auth;
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={lang}");
            }
            else
            {
                lang = Regex.Match(gachaUrl, @"&lang=([^&]+)").Groups[1].Value;
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    gachaUrl = Regex.Replace(gachaUrl, @"&lang=([^&]+)", $"&lang={LanguageUtil.FilterLanguage(lang)}");
                }
            }
            return gachaUrl;
        }
        match = Regex.Match(gachaUrl, @"(https://public-operation-hk4e[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            gachaUrl = Regex.Replace(gachaUrl, @"&gacha_type=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&page=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&size=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&end_id=\d", "");
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={lang}");
            }
            return gachaUrl;
        }
        throw new ArgumentException(CoreLang.Gacha_CannotParseTheWishRecordURL);
    }



    protected virtual async Task<List<GenshinBeyondGachaItem>> GetGachaLogByQueryAsync(string gachaUrlPrefix, BeyondGachaLogQuery param, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Random.Shared.Next(200, 300), cancellationToken);
        var url = $"{gachaUrlPrefix}&{param}";
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(miHoYoApiWrapper<GenshinBeyondGachaResult>), GachaLogJsonContext.Default, cancellationToken) as miHoYoApiWrapper<GenshinBeyondGachaResult>;
        if (wrapper is null)
        {
            return new List<GenshinBeyondGachaItem>();
        }
        else if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        else
        {
            return wrapper.Data.List;
        }
    }




    public static string? GetGachaUrlFromWebCache(GameBiz gameBiz, string? installPath = null)
    {
        var file = GetGachaCacheFilePath(gameBiz, installPath);
        if (File.Exists(file))
        {
            return FindMatchStringFromFile(file, GetGachaUrlPattern(gameBiz));
        }
        return null;
    }



    public static string GetGachaCacheFilePath(GameBiz gameBiz, string? installPath)
    {
        string file = gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, WEB_CACHE_PATH_YS_CN),
            GameBiz.hk4e_global => Path.Join(installPath, WEB_CACHE_PATH_YS_OS),
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
        DateTime lastWriteTime = DateTime.MinValue;
        if (File.Exists(file))
        {
            lastWriteTime = File.GetLastWriteTime(file);
        }
        string prefix = gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => @"YuanShen_Data\webCaches",
            GameBiz.hk4e_global => @"GenshinImpact_Data\webCaches",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
        string webCache = Path.Join(installPath, prefix);
        if (Directory.Exists(webCache))
        {
            foreach (var item in Directory.GetDirectories(webCache))
            {
                string target = Path.Join(item, @"Cache\Cache_Data\data_2");
                if (File.Exists(target) && File.GetLastWriteTime(target) > lastWriteTime)
                {
                    file = target;
                }
            }
        }
        return file;
    }



    private static ReadOnlySpan<byte> GetGachaUrlPattern(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => SPAN_WEB_PREFIX_YS_CN,
            GameBiz.hk4e_global => SPAN_WEB_PREFIX_YS_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
    }



    private static string? FindMatchStringFromFile(string path, ReadOnlySpan<byte> prefix)
    {
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var ms = new MemoryStream();
        fs.CopyTo(ms);
        var span = ms.ToArray().AsSpan();
        var index = span.LastIndexOf(prefix);
        if (index >= 0)
        {
            var length = span[index..].IndexOfAny("\0\""u8);
            return Encoding.UTF8.GetString(span.Slice(index, length));
        }

        return null;
    }



    public async Task<long> GetUidByGachaUrlAsync(string gachaUrl)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl);
        foreach (var gachaType in QueryGachaTypes)
        {
            var param = new BeyondGachaLogQuery(gachaType, 1, 1, 0);
            var list = await GetGachaLogByQueryAsync(prefix, param);
            if (list.Count != 0)
            {
                return list.First().Uid;
            }
        }
        return 0;
    }



    public async Task<List<GenshinBeyondGachaInfo>> GetGenshinBeyondGachaInfoAsync(CancellationToken cancellationToken = default)
    {
        const string url = "https://starward-static.scighost.com/game-assets/genshin/GenshinBeyondGachaInfo.json";
        var result = await _httpClient.GetFromJsonAsync(url, typeof(List<GenshinBeyondGachaInfo>), GachaLogJsonContext.Default, cancellationToken) as List<GenshinBeyondGachaInfo>;
        return result ?? [];
    }



}
