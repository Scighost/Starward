using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha.Genshin;

public class WishRecordClient : GachaLogClient
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



    protected override IReadOnlyCollection<int> GachaTypes { get; init; } = new int[] { 100, 200, 301, 302 }.AsReadOnly();



    public WishRecordClient(HttpClient? httpClient = null) : base(httpClient)
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



    public async Task<List<WishRecordItem>> GetWishRecordAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(WishType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var progres_internal = new Progress<(int GachaType, int Page)>((x) => progress?.Report(((WishType)x.GachaType, x.Page)));
        return await GetGachaLogAsync<WishRecordItem>(gachaUrl, endId, lang, progres_internal, cancellationToken);
    }




    public async Task<List<WishRecordItem>> GetWishRecordAsync(string gachaUrl, WishType gachaType, long endId = 0, string? lang = null, IProgress<(WishType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var progres_internal = new Progress<(int GachaType, int Page)>((x) => progress?.Report(((WishType)x.GachaType, x.Page)));
        return await GetGachaLogAsync<WishRecordItem>(gachaUrl, (int)gachaType, endId, lang, progres_internal, cancellationToken);
    }



    public async Task<List<WishRecordItem>> GetWishRecordAsync(string gachaUrl, GachaLogQuery query, string? lang = null, CancellationToken cancellationToken = default)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl, lang);
        return await GetGachaLogAsync<WishRecordItem>(prefix, query, cancellationToken);
    }





    [SupportedOSPlatform("windows")]
    public override string? GetGameInstallPathFromRegistry(RegionType region)
    {
        var key = region switch
        {
            RegionType.China => REG_KEY_CN,
            RegionType.Global => REG_KEY_OS,
            RegionType.ChinaCloud => REG_KEY_CLOUD_CN,
            _ => throw new ArgumentOutOfRangeException($"Unknown region ({region})"),
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
