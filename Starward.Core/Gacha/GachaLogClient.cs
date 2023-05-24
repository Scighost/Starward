using Microsoft.Win32;
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

    protected const string WEB_PREFIX_YS_CN = "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha-v2/index.html";
    protected const string WEB_PREFIX_YS_OS = "https://webstatic-sea.hoyoverse.com/genshin/event/e20190909gacha-v2/index.html";

    protected const string API_PREFIX_YS_CN = "https://hk4e-api.mihoyo.com/event/gacha_info/api/getGachaLog";
    protected const string API_PREFIX_YS_OS = "https://hk4e-api-os.hoyoverse.com/event/gacha_info/api/getGachaLog";

    protected static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_YS_CN = new(Encoding.UTF8.GetBytes(WEB_PREFIX_YS_CN));
    protected static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_YS_OS = new(Encoding.UTF8.GetBytes(WEB_PREFIX_YS_OS));



    protected const string REG_KEY_SR_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩坏：星穹铁道";
    protected const string REG_KEY_SR_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Star Rail";

    protected const string WEB_CACHE_SR_PATH = @"StarRail_Data\webCaches\Cache\Cache_Data\data_2";

    protected const string WEB_PREFIX_SR_CN = "https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html";
    protected const string WEB_PREFIX_SR_OS = "https://webstatic-sea.hoyoverse.com/hkrpg/event/e20211215gacha-v2/index.html";

    protected const string API_PREFIX_SR_CN = "https://api-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";
    protected const string API_PREFIX_SR_OS = "https://api-os-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";

    protected static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_SR_CN = new(Encoding.UTF8.GetBytes(WEB_PREFIX_SR_CN));
    protected static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_SR_OS = new(Encoding.UTF8.GetBytes(WEB_PREFIX_SR_OS));




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



    public async Task<int> GetUidByGachaUrlAsync(string gachaUrl)
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
            GameBiz.hk4e_cn => REG_KEY_YS_CN,
            GameBiz.hk4e_global => REG_KEY_YS_OS,
            GameBiz.hkrpg_cn => REG_KEY_SR_CN,
            GameBiz.hkrpg_global => REG_KEY_SR_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        return GetGameInstallPathFromRegistry(key);
    }


    public static string? GetGachaUrlFromWebCache(GameBiz gameBiz, string? installPath = null)
    {
        if (gameBiz is GameBiz.hk4e_cloud)
        {
            var file = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GenshinImpactCloudGame\config\logs\MiHoYoSDK.log");
            if (File.Exists(file))
            {
                return FindMatchStringFromFile(file, MEMORY_WEB_PREFIX_YS_CN);
            }
            return null;
        }
        else if (gameBiz.ToGame() is GameBiz.GenshinImpact)
        {
            var file = Path.Join(installPath, WEB_CACHE_PATH_YS_CN);
            if (File.Exists(file))
            {
                return FindMatchStringFromFile(file, MEMORY_WEB_PREFIX_YS_CN, MEMORY_WEB_PREFIX_YS_OS);
            }
            file = Path.Join(installPath, WEB_CACHE_PATH_YS_OS);
            if (File.Exists(file))
            {
                return FindMatchStringFromFile(file, MEMORY_WEB_PREFIX_YS_OS, MEMORY_WEB_PREFIX_YS_CN);
            }
            return null;
        }
        else if (gameBiz.ToGame() is GameBiz.StarRail)
        {
            var file = Path.Join(installPath, WEB_CACHE_SR_PATH);
            if (File.Exists(file))
            {
                return FindMatchStringFromFile(file, MEMORY_WEB_PREFIX_SR_CN, MEMORY_WEB_PREFIX_SR_OS);
            }
            return null;
        }
        throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}");
    }



    #endregion




    #region protected method


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
        var wrapper = await _httpClient.GetFromJsonAsync(url, typeof(MihoyoApiWrapper<GachaLogResult<T>>), GachaLogJsonContext.Default, cancellationToken) as MihoyoApiWrapper<GachaLogResult<T>>;
        if (wrapper is null)
        {
            return new List<T>();
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


    protected static string? FindMatchStringFromFile(string path, params ReadOnlyMemory<byte>[] prefixes)
    {
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        var ms = new MemoryStream();
        fs.CopyTo(ms);
        var span = ms.ToArray().AsSpan();
        foreach (var prefix in prefixes)
        {
            var index = span.LastIndexOf(prefix.Span);
            if (index >= 0)
            {
                var length = span[index..].IndexOfAny("\0\""u8);
                return Encoding.UTF8.GetString(span.Slice(index, length));
            }
        }
        return null;
    }


    #endregion











}
