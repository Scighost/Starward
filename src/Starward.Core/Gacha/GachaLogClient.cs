using Microsoft.Win32;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha;

public abstract class GachaLogClient
{


    protected const string REG_KEY_YS_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\原神";
    protected const string REG_KEY_YS_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Genshin Impact";
    protected const string REG_KEY_YS_CLOUD = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\云·原神";

    protected const string WEB_CACHE_PATH_YS_CN = @"YuanShen_Data\webCaches\Cache\Cache_Data\data_2";
    protected const string WEB_CACHE_PATH_YS_OS = @"GenshinImpact_Data\webCaches\Cache\Cache_Data\data_2";

    protected const string API_PREFIX_YS_CN = "https://hk4e-api.mihoyo.com/event/gacha_info/api/getGachaLog";
    protected const string API_PREFIX_YS_OS = "https://hk4e-api-os.hoyoverse.com/gacha_info/api/getGachaLog";

    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_YS_CN => "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha-v2/index.html"u8;
    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_YS_OS => "https://gs.hoyoverse.com/genshin/event/e20190909gacha-v2/index.html"u8;



    protected const string REG_KEY_SR_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩坏：星穹铁道";
    protected const string REG_KEY_SR_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Star Rail";

    protected const string WEB_CACHE_SR_PATH = @"StarRail_Data\webCaches\Cache\Cache_Data\data_2";

    protected const string API_PREFIX_SR_CN = "https://api-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";
    protected const string API_PREFIX_SR_OS = "https://api-os-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";

    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_SR_CN => "https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html"u8;
    protected static ReadOnlySpan<byte> SPAN_WEB_PREFIX_SR_OS => "https://gs.hoyoverse.com/hkrpg/event/e20211215gacha-v2/index.html"u8;



    protected const string REG_KEY_BH3_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩坏3";
    protected const string REG_KEY_BH3_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Honkai Impact 3";
    protected const string REG_KEY_BH3_GL = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Honkai Impact 3rd";
    protected const string REG_KEY_BH3_TW = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩壊3rd";
    protected const string REG_KEY_BH3_KR = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\붕괴3rd";
    protected const string REG_KEY_BH3_JP = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩壊3rd";




    protected readonly HttpClient _httpClient;


    protected abstract IReadOnlyCollection<GachaType> GachaTypes { get; init; }



    public GachaLogClient(HttpClient? httpClient = null)
    {
        if (httpClient is null)
        {
            _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
        }
        else
        {
            _httpClient = httpClient;
        }
    }



    #region public method



    public async Task<long> GetUidByGachaUrlAsync(string gachaUrl)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl);
        foreach (var gachaType in GachaTypes)
        {
            var param = new GachaLogQuery(gachaType, 1, 1, 0);
            var list = await GetGachaLogByQueryAsync<GachaLogItem>(prefix, param);
            if (list.Any())
            {
                return list.First().Uid;
            }
        }
        return 0;
    }



    public abstract Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default);


    public abstract Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaType gachaType, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default);


    public abstract Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default);



    [SupportedOSPlatform("windows")]
    public static string? GetGameInstallPathFromRegistry(GameBiz biz)
    {
        if (biz is GameBiz.hk4e_cloud)
        {
            return Registry.GetValue(REG_KEY_YS_CLOUD, "InstallPath", null) as string;
        }
        var key = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => REG_KEY_YS_CN,
            GameBiz.hk4e_global => REG_KEY_YS_OS,
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => REG_KEY_SR_CN,
            GameBiz.hkrpg_global => REG_KEY_SR_OS,
            GameBiz.bh3_cn => REG_KEY_BH3_CN,
            GameBiz.bh3_global => REG_KEY_BH3_GL,
            GameBiz.bh3_overseas => REG_KEY_BH3_OS,
            GameBiz.bh3_tw => REG_KEY_BH3_TW,
            GameBiz.bh3_kr => REG_KEY_BH3_KR,
            GameBiz.bh3_jp => REG_KEY_BH3_JP,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        return GetGameInstallPathFromRegistry(key);
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
        if (gameBiz is GameBiz.hk4e_cloud)
        {
            return Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GenshinImpactCloudGame\config\logs\MiHoYoSDK.log");
        }
        string file = gameBiz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, WEB_CACHE_PATH_YS_CN),
            GameBiz.hk4e_global => Path.Join(installPath, WEB_CACHE_PATH_YS_OS),
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, WEB_CACHE_SR_PATH),
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
        DateTime lastWriteTime = DateTime.MinValue;
        if (File.Exists(file))
        {
            lastWriteTime = File.GetLastWriteTime(file);
        }
        string prefix = gameBiz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => @"YuanShen_Data\webCaches",
            GameBiz.hk4e_global => @"GenshinImpact_Data\webCaches",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => @"StarRail_Data\webCaches",
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
        return gameBiz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_cloud or GameBiz.hk4e_bilibili => SPAN_WEB_PREFIX_YS_CN,
            GameBiz.hk4e_global => SPAN_WEB_PREFIX_YS_OS,
            GameBiz.hkrpg_cn or GameBiz.hkrpg_bilibili => SPAN_WEB_PREFIX_SR_CN,
            GameBiz.hkrpg_global => SPAN_WEB_PREFIX_SR_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
        };
    }



    #endregion




    #region protected method



    protected async Task<T> CommonGetAsync<T>(string url, CancellationToken cancellationToken = default)
    {
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(miHoYoApiWrapper<T>), GachaLogJsonContext.Default, cancellationToken) as miHoYoApiWrapper<T>;
        if (wrapper is null)
        {
            throw new miHoYoApiException(-1, "Response body is null");
        }
        else if (wrapper.Retcode != 0)
        {
            throw new miHoYoApiException(wrapper.Retcode, wrapper.Message);
        }
        else
        {
            return wrapper.Data;
        }
    }



    protected abstract string GetGachaUrlPrefix(string gachaUrl, string? lang = null);



    protected async Task<List<T>> GetGachaLogAsync<T>(string gachaUrl, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl, lang);
        var result = new List<T>();
        foreach (var gachaType in GachaTypes)
        {
            result.AddRange(await GetGachaLogByTypeAsync<T>(prefix, gachaType, endId, progress, cancellationToken));
        }
        return result;
    }




    protected async Task<List<T>> GetGachaLogAsync<T>(string gachaUrl, GachaType gachaType, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl, lang);
        return await GetGachaLogByTypeAsync<T>(prefix, gachaType, endId, progress, cancellationToken);
    }




    protected async Task<List<T>> GetGachaLogByQueryAsync<T>(string gachaUrlPrefix, GachaLogQuery param, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        await Task.Delay(Random.Shared.Next(200, 300));
        var url = $"{gachaUrlPrefix}&{param}";
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(miHoYoApiWrapper<GachaLogResult<T>>), GachaLogJsonContext.Default, cancellationToken) as miHoYoApiWrapper<GachaLogResult<T>>;
        if (wrapper is null)
        {
            return new List<T>();
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




    private async Task<List<T>> GetGachaLogByTypeAsync<T>(string prefix, GachaType gachaType, long endId = 0, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default) where T : GachaLogItem
    {
        var param = new GachaLogQuery(gachaType, 1, 20, 0);
        var result = new List<T>();
        while (true)
        {
            progress?.Report((gachaType, param.Page));
            var list = await GetGachaLogByQueryAsync<T>(prefix, param, cancellationToken);
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


    [SupportedOSPlatform("windows")]
    protected static string? GetGameInstallPathFromRegistry(string regKey)
    {
        var launcherPath = Registry.GetValue(regKey, "InstallPath", null) as string;
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


    protected static string? FindMatchStringFromFile(string path, ReadOnlySpan<byte> prefix)
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


    #endregion



    public async Task<GenshinGachaWiki> GetGenshinGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = Util.FilterLanguage(lang);
        GenshinGachaWiki wiki;
        if (gameBiz.IsChinaServer() && lang is "zh-cn")
        {
            const string url = "https://api-takumi.mihoyo.com/event/platsimulator/config?gids=2&game=hk4e";
            wiki = await CommonGetAsync<GenshinGachaWiki>(url, cancellationToken);
        }
        else
        {
            string url = $"https://sg-public-api.hoyolab.com/event/simulatoros/config?lang={lang}";
            wiki = await CommonGetAsync<GenshinGachaWiki>(url, cancellationToken);
        }
        wiki.Language = lang;
        return wiki;
    }


    public async Task<StarRailGachaWiki> GetStarRailGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = Util.FilterLanguage(lang);
        StarRailGachaWiki wiki;
        if (gameBiz.IsChinaServer() && lang is "zh-cn")
        {
            const string url = "https://api-takumi.mihoyo.com/event/rpgsimulator/config?game=hkrpg";
            wiki = await CommonGetAsync<StarRailGachaWiki>(url, cancellationToken);
        }
        else
        {
            wiki = new StarRailGachaWiki { Game = "hkrpg", Avatar = new List<StarRailGachaInfo>(), Equipment = new List<StarRailGachaInfo>() };
            for (int i = 1; i <= 10; i++)
            {
                string url = $"https://sg-public-api.hoyolab.com/event/rpgcalc/avatar/list?game=hkrpg&lang={lang}&tab_from=TabAll&page={i}&size=100";
                var wrapper = await CommonGetAsync<StarRailGachaInfoWrapper>(url, cancellationToken);
                wiki.Avatar.AddRange(wrapper.List);
                if (wrapper.List.Count != 100)
                {
                    break;
                }
            }
            for (int i = 1; i <= 10; i++)
            {
                string url = $"https://sg-public-api.hoyolab.com/event/rpgcalc/equipment/list?game=hkrpg&lang={lang}&tab_from=TabAll&page={i}&size=100";
                var wrapper = await CommonGetAsync<StarRailGachaInfoWrapper>(url, cancellationToken);
                wiki.Equipment.AddRange(wrapper.List);
                if (wrapper.List.Count != 100)
                {
                    break;
                }
            }
        }
        wiki.Language = lang;
        return wiki;
    }




}
