using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha.Genshin;

public class GenshinGachaClient : GachaLogClient
{


    private const string REG_KEY_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\原神";
    private const string REG_KEY_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Genshin Impact";
    private const string REG_KEY_CLOUD_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\云·原神";

    private const string WEB_CACHE_PATH_CN = @"YuanShen_Data\webCaches\Cache\Cache_Data\data_2";
    private const string WEB_CACHE_PATH_OS = @"GenshinImpact_Data\webCaches\Cache\Cache_Data\data_2";

    private const string WEB_PREFIX_CN = "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha-v2/index.html";
    private const string WEB_PREFIX_OS = "https://webstatic-sea.hoyoverse.com/genshin/event/e20190909gacha-v2/index.html";

    private const string API_PREFIX_CN = "https://hk4e-api.mihoyo.com/event/gacha_info/api/getGachaLog";
    private const string API_PREFIX_OS = "https://hk4e-api-os.hoyoverse.com/event/gacha_info/api/getGachaLog";

    private static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_CN = new(Encoding.UTF8.GetBytes(WEB_PREFIX_CN));
    private static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_OS = new(Encoding.UTF8.GetBytes(WEB_PREFIX_OS));



    protected override IReadOnlyCollection<GachaType> GachaTypes { get; init; } = new GachaType[] { (GachaType)100, (GachaType)200, (GachaType)301, (GachaType)302 }.AsReadOnly();



    public GenshinGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }




    protected override string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
    {
        var match = Regex.Match(gachaUrl, @"(https://webstatic[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var auth = gachaUrl.Substring(gachaUrl.IndexOf('?')).Replace("#/log", "");
            if (gachaUrl.Contains("webstatic-sea"))
            {
                gachaUrl = API_PREFIX_OS + auth;
            }
            else
            {
                gachaUrl = API_PREFIX_CN + auth;
            }
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={lang}");
            }
            return gachaUrl;
        }
        match = Regex.Match(gachaUrl, @"(https://hk4e-api[!-z]+)");
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
        throw new ArgumentException("Cannot parse the wish URL.");
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<GenshinGachaItem>(gachaUrl, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaType gachaType, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<GenshinGachaItem>(gachaUrl, gachaType, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        string prefix = GetGachaUrlPrefix(gachaUrl);
        return await GetGachaLogByQueryAsync<GenshinGachaItem>(prefix, query, cancellationToken);
    }




    [SupportedOSPlatform("windows")]
    public override string? GetGameInstallPathFromRegistry(GameBiz biz)
    {
        var key = biz switch
        {
            GameBiz.hk4e_cn => REG_KEY_CN,
            GameBiz.hk4e_global => REG_KEY_OS,
            GameBiz.hk4e_cloud => REG_KEY_CLOUD_CN,
            _ => throw new ArgumentOutOfRangeException($"Unknown region ({biz})"),
        };
        return GetGameInstallPathFromRegistry(key);
    }





    public override string? GetGachaUrlFromWebCache(string installPath)
    {
        var file = Path.Join(installPath, WEB_CACHE_PATH_CN);
        var url = FindMatchStringFromFile(file, MEMORY_WEB_PREFIX_CN, MEMORY_WEB_PREFIX_OS);
        return url ?? FindMatchStringFromFile(Path.Join(installPath, WEB_CACHE_PATH_OS), MEMORY_WEB_PREFIX_CN, MEMORY_WEB_PREFIX_OS);
    }




}
