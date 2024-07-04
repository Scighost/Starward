using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Starward.Core.Gacha;

namespace Starward.Core.Gacha.ZZZ;

public class ZZZGachaClient : GachaLogClient
{



    // todo
    protected override IReadOnlyCollection<GachaType> GachaTypes { get; init; } = new GachaType[] { (GachaType)1, (GachaType)2, (GachaType)3, (GachaType)4 }.AsReadOnly();



    public ZZZGachaClient(HttpClient? httpClient = null) : base(httpClient)
    {

    }



    // todo
    protected override string GetGachaUrlPrefix(string gachaUrl, string? lang = null)
    {
        var match = Regex.Match(gachaUrl, @"(https://webstatic\.mihoyo\.com[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            var auth = gachaUrl.Substring(gachaUrl.IndexOf('?')).Replace("#/log", "");
            gachaUrl = API_PREFIX_ZZZ_CN + auth;
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
            gachaUrl = API_PREFIX_ZZZ_OS + auth;
            if (!string.IsNullOrWhiteSpace(lang))
            {
                gachaUrl = Regex.Replace(gachaUrl, @"&lang=[^&]+", $"&lang={lang}");
            }
            return gachaUrl;
        }
        match = Regex.Match(gachaUrl, @"(https://public-operation-nap.mihoyo.com[!-z]+)");
        if (match.Success)
        {
            gachaUrl = match.Groups[1].Value;
            gachaUrl = Regex.Replace(gachaUrl, @"&real_gacha_type=\d", "");
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


    protected override async Task<List<T>> GetGachaLogByQueryAsync<T>(string gachaUrlPrefix, GachaLogQuery param, CancellationToken cancellationToken = default)
    {
        await Task.Delay(Random.Shared.Next(200, 300));
        var url = $"{gachaUrlPrefix}&{param.ToZZZString()}";
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




}
