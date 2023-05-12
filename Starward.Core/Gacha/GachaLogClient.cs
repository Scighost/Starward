using Microsoft.Win32;
using System.Net.Http.Json;
using System.Runtime.Versioning;
using System.Text;
using System.Text.RegularExpressions;

namespace Starward.Core.Gacha;

public abstract class GachaLogClient
{


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
    public abstract string? GetGameInstallPathFromRegistry(GameBiz biz);


    public abstract string? GetGachaUrlFromWebCache(string installPath);



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
        if (File.Exists(path))
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
                    var length = span[index..].IndexOf("\0"u8);
                    return Encoding.UTF8.GetString(span.Slice(index, length));
                }
            }
        }
        return null;
    }


    #endregion











}
