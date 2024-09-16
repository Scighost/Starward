using System.Text.RegularExpressions;

namespace Starward.Core.Gacha.Genshin;

public class GenshinGachaClient : GachaLogClient
{



    public override IReadOnlyCollection<IGachaType> QueryGachaTypes { get; init; } = new GenshinGachaType[] { 100, 200, 301, 302, 500 }.Cast<IGachaType>().ToList().AsReadOnly();



    public GenshinGachaClient(HttpClient? httpClient = null) : base(httpClient)
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




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<GenshinGachaItem>(gachaUrl, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, IGachaType gachaType, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<GenshinGachaItem>(gachaUrl, gachaType, endId, lang, progress, cancellationToken);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        string prefix = GetGachaUrlPrefix(gachaUrl);
        return await GetGachaLogByQueryAsync<GenshinGachaItem>(prefix, query, cancellationToken);
    }





}
