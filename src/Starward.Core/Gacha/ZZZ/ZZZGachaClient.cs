using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Starward.Core.Gacha.ZZZ;

public class ZZZGachaClient : GachaLogClient
{




    protected override IReadOnlyCollection<GachaType> GachaTypes { get; init; } = new GachaType[] { (GachaType)100, (GachaType)200, (GachaType)301, (GachaType)302, (GachaType)500 }.AsReadOnly();



    public ZZZGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }




    protected override string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
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
        throw new ArgumentException(CoreLang.Gacha_CannotParseTheWishRecordURL);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<ZZZGachaItem>(gachaUrl, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaType gachaType, long endId = 0, string? lang = null, IProgress<(GachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<ZZZGachaItem>(gachaUrl, gachaType, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        string prefix = GetGachaUrlPrefix(gachaUrl);
        return await GetGachaLogByQueryAsync<ZZZGachaItem>(prefix, query, cancellationToken);
    }







}
