using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha.StarRail;

public class WarpRecordClient : GachaLogClient
{


    private const string REG_KEY_CN = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\崩坏：星穹铁道";
    private const string REG_KEY_OS = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Star Rail";

    private const string WEB_CACHE_PATH = @"StarRail_Data\webCaches\Cache\Cache_Data\data_2";

    private const string WEB_PREFIX_CN = "https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html";
    private const string WEB_PREFIX_OS = "https://webstatic-sea.hoyoverse.com/hkrpg/event/e20211215gacha-v2/index.html";

    private const string API_PREFIX_CN = "https://api-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";
    private const string API_PREFIX_OS = "https://api-os-takumi.mihoyo.com/common/gacha_record/api/getGachaLog";



    protected override IReadOnlyCollection<int> GachaTypes { get; init; } = new int[] { 1, 2, 11, 12 }.AsReadOnly();



    public WarpRecordClient(HttpClient? httpClient = null) : base(httpClient)
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





    public async Task<List<WarpRecordItem>> GetWarpRecordAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(WarpType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var progres_internal = new Progress<(int GachaType, int Page)>((x) => progress?.Report(((WarpType)x.GachaType, x.Page)));
        return await base.GetGachaLogAsync<WarpRecordItem>(gachaUrl, endId, lang, progres_internal, cancellationToken);
    }




    public async Task<List<WarpRecordItem>> GetWarpRecordAsync(string gachaUrl, WarpType gachaType, long endId = 0, string? lang = null, IProgress<(WarpType WarpType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        var progres_internal = new Progress<(int GachaType, int Page)>((x) => progress?.Report(((WarpType)x.GachaType, x.Page)));
        return await base.GetGachaLogAsync<WarpRecordItem>(gachaUrl, (int)gachaType, endId, lang, progres_internal, cancellationToken);
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
        return GetGameInstallPathFromRegistry(key);
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
