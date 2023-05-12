using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha.StarRail;

public class StarRailGachaClient : GachaLogClient
{


    private const string REG_KEY_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩坏：星穹铁道";
    private const string REG_KEY_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Star Rail";

    private const string WEB_CACHE_PATH = @"StarRail_Data\webCaches\Cache\Cache_Data\data_2";

    private const string WEB_PREFIX_CN = "https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html";
    private const string WEB_PREFIX_OS = "https://webstatic-sea.hoyoverse.com/hkrpg/event/e20211215gacha-v2/index.html";

    private const string API_PREFIX_CN = "https://api-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";
    private const string API_PREFIX_OS = "https://api-os-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";

    private static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_CN = new(Encoding.UTF8.GetBytes(WEB_PREFIX_CN));
    private static readonly ReadOnlyMemory<byte> MEMORY_WEB_PREFIX_OS = new(Encoding.UTF8.GetBytes(WEB_PREFIX_OS));


    protected override IReadOnlyCollection<GachaType> GachaTypes { get; init; } = new GachaType[] { (GachaType)1, (GachaType)2, (GachaType)11, (GachaType)12 }.AsReadOnly();



    public StarRailGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }





    protected override string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
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
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={lang}");
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
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={lang}");
            }
            return gachaUrl;
        }
        throw new ArgumentException("Cannot parse the warp URL.");
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<StarRailGachaItem>(gachaUrl, endId, lang, progress, cancellationToken);
    }



    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaType gachaType, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<StarRailGachaItem>(gachaUrl, gachaType, endId, lang, progress, cancellationToken);
    }



    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl);
        return await GetGachaLogByQueryAsync<StarRailGachaItem>(prefix, query, cancellationToken);
    }




    [SupportedOSPlatform("windows")]
    public override string? GetGameInstallPathFromRegistry(GameBiz biz)
    {
        var key = biz switch
        {
            GameBiz.hkrpg_cn => REG_KEY_CN,
            GameBiz.hkrpg_global => REG_KEY_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region ({biz})"),
        };
        return GetGameInstallPathFromRegistry(key);
    }



    public override string? GetGachaUrlFromWebCache(string installPath)
    {
        var file = Path.Join(installPath, WEB_CACHE_PATH);
        return FindMatchStringFromFile(file, MEMORY_WEB_PREFIX_CN, MEMORY_WEB_PREFIX_OS);
    }



}
