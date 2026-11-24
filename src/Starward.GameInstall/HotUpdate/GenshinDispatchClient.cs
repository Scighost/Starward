using Starward.Core;
using System.Globalization;
using System.Security.Cryptography;

namespace Starward.GameInstall.HotUpdate;

public class GenshinDispatchClient
{

    private readonly HttpClient _httpClient;


    public GenshinDispatchClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }



    public async Task<CutsceneCollection> GetCutsceneCollectionAsync(CancellationToken cancellation = default)
    {
        return await GetCutsceneCollectionAsync(CultureInfo.CurrentUICulture.Name, cancellation);
    }



    public async Task<CutsceneCollection> GetCutsceneCollectionAsync(string language, CancellationToken cancellation = default)
    {
        language = LanguageUtil.FilterLanguage(language);
        string url = $"https://starward-static.scighost.com/game-assets/genshin/cutscene/cutscene.{language}.json";
        var result = await _httpClient.GetFromJsonAsync(url, GenshinHotUpdateJsonContext.Default.CutsceneCollection, cancellation);
        if (result is null)
        {
            throw new NullReferenceException("Genshin cutscene has not json content.");
        }
        return result;
    }



    public async Task<QueryRegionListHttpRsp> QueryRegionListAsync(ServerRegion region, string version, CancellationToken cancellation = default)
    {
        string url = region switch
        {
            ServerRegion.HoYoverse => $"https://dispatchosglobal.yuanshen.com/query_region_list?version=OSRELWin{version}&lang=1&platform=3&binary=1&time=415&channel_id=1&sub_channel_id=1",
            ServerRegion.miHoYo => $"https://dispatchcnglobal.yuanshen.com/query_region_list?version=CNRELWin{version}&lang=2&platform=3&binary=1&time=415&channel_id=1&sub_channel_id=1",
            _ => throw new NotSupportedException($"Region {region} is not supported."),
        };
        string base64 = await _httpClient.GetStringAsync(url, cancellation);
        var response = QueryRegionListHttpRsp.Parser.ParseFrom(Convert.FromBase64String(base64));
        if (response.Retcode is not 0)
        {
            throw new miHoYoApiException(response.Retcode, "query_region_list");
        }
        return response;
    }



    public async Task<QueryCurRegionHttpRsp> QueryCurRegionAsync(string dispathUrl, ServerRegion region, string version, string dispatchSeed, string dispatchKey, CancellationToken cancellation = default)
    {
        string url = region switch
        {
            ServerRegion.HoYoverse => $"{dispathUrl}?version=OSRELWin{version}&lang=1&platform=3&binary=1&time=849&channel_id=1&sub_channel_id=1&account_type=1&dispatchSeed={dispatchSeed}&key_id=5",
            ServerRegion.miHoYo => $"{dispathUrl}?version=CNRELWin{version}&lang=2&platform=3&binary=1&time=849&channel_id=1&sub_channel_id=1&account_type=1&dispatchSeed={dispatchSeed}&key_id=4",
            _ => throw new NotSupportedException($"Region {region} is not supported."),
        };
        var response = await _httpClient.GetFromJsonAsync(url, GenshinHotUpdateJsonContext.Default.QueryCurRegionResponse, cancellation);
        if (response is null)
        {
            throw new NullReferenceException("QueryCurRegion has not json content.");
        }
        byte[] cipher = Convert.FromBase64String(response.Content);
        using var rsa = RSA.Create();
        rsa.ImportFromEncryptedPem(dispatchKey, dispatchSeed);
        int blockSize = rsa.KeySize / 8;
        if (cipher.Length % blockSize != 0)
        {
            throw new InvalidDataException("The cipher of query_cur_region content is not matched rsa key block size.");
        }
        MemoryStream ms = new();
        for (int i = 0; i < cipher.Length; i += blockSize)
        {
            ms.Write(rsa.Decrypt(cipher.AsSpan(i, blockSize), RSAEncryptionPadding.Pkcs1));
        }
        ms.Position = 0;
        var res = QueryCurRegionHttpRsp.Parser.ParseFrom(ms);
        if (res.Retcode is not 0)
        {
            throw new miHoYoApiException(res.Retcode, "query_cur_region");
        }
        return res;
    }


}
