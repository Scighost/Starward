using System.Text.RegularExpressions;

namespace Starward.Core.Gacha.StarRail;

public class StarRailGachaClient : GachaLogClient
{



    public override IReadOnlyCollection<IGachaType> QueryGachaTypes { get; init; } = new StarRailGachaType[] { 1, 2, 11, 12, 21, 22 }.Cast<IGachaType>().ToList().AsReadOnly();



    public StarRailGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }





    protected override string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
    {
        var match = Regex.Match(gachaUrl, @"(https://webstatic\.mihoyo\.com[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var auth = gachaUrl.Substring(gachaUrl.IndexOf('?'));
            gachaUrl = API_PREFIX_SR_CN + auth;
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={LanguageUtil.FilterLanguage(lang)}");
            }
            return gachaUrl;
        }
        match = Regex.Match(gachaUrl, @"(https://gs\.hoyoverse\.com[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var auth = gachaUrl.Substring(gachaUrl.IndexOf('?'));
            gachaUrl = API_PREFIX_SR_OS + auth;
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={LanguageUtil.FilterLanguage(lang)}");
            }
            return gachaUrl;
        }
        match = Regex.Match(gachaUrl, @"(https://public-operation-hkrpg[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            gachaUrl = Regex.Replace(gachaUrl, @"&gacha_type=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&page=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&size=\d", "");
            gachaUrl = Regex.Replace(gachaUrl, @"&end_id=\d", "");
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={LanguageUtil.FilterLanguage(lang)}");
            }
            return gachaUrl;
        }
        throw new ArgumentException(CoreLang.Gacha_CannotParseTheWarpRecordURL);
    }




    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        endId = Math.Clamp(endId, 0, long.MaxValue);
        var prefix = GetGachaUrlPrefix(gachaUrl, lang);
        var result = new List<StarRailGachaItem>();
        foreach (var gachaType in QueryGachaTypes)
        {
            if (gachaType.Value is StarRailGachaType.CharacterCollaborationWarp or StarRailGachaType.LightConeCollaborationWarp)
            {
                result.AddRange(await GetGachaLogByTypeAsync<StarRailGachaItem>(prefix.Replace("/getGachaLog", "/getLdGachaLog"), gachaType, endId, progress, cancellationToken));
            }
            else
            {
                result.AddRange(await GetGachaLogByTypeAsync<StarRailGachaItem>(prefix, gachaType, endId, progress, cancellationToken));
            }
        }
        return result;
    }



    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, IGachaType gachaType, long endId = 0, string? lang = null, IProgress<(IGachaType GachaType, int Page)>? progress = null, CancellationToken cancellationToken = default)
    {
        return await GetGachaLogAsync<StarRailGachaItem>(gachaUrl, gachaType, endId, lang, progress, cancellationToken);
    }



    public override async Task<IEnumerable<GachaLogItem>> GetGachaLogAsync(string gachaUrl, GachaLogQuery query, CancellationToken cancellationToken = default)
    {
        var prefix = GetGachaUrlPrefix(gachaUrl);
        return await GetGachaLogByQueryAsync<StarRailGachaItem>(prefix, query, cancellationToken);
    }





}
