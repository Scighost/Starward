using System.Net.Http.Json;
using System.Runtime.InteropServices;

namespace Starward.Core.Metadata;

public class MetadataClient
{

#if DEBUG
    private const string API_PREFIX = "https://starward.scighost.com/metadata/dev/";
#else
    private const string API_PREFIX = "https://starward.scighost.com/metadata/v1/";
#endif


    private readonly HttpClient _httpClient;


    public MetadataClient(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All });
    }



    private async Task<T> CommonGetAsync<T>(string url, CancellationToken cancellationToken = default) where T : class
    {
        var res = await _httpClient.GetFromJsonAsync(url, typeof(T), MetadataJsonContext.Default, cancellationToken) as T;
        if (res == null)
        {
            throw new NullReferenceException("");
        }
        else
        {
            return res;
        }
    }



    public async Task<List<GameInfo>> GetGameInfoAsync(CancellationToken cancellationToken = default)
    {
        var url = API_PREFIX + "game_info.json";
        return await CommonGetAsync<List<GameInfo>>(url, cancellationToken);
    }



    public async Task<ReleaseVersion> GetVersionAsync(bool isPrerelease, Architecture architecture, CancellationToken cancellationToken = default)
    {
        var name = (isPrerelease, architecture) switch
        {
            (false, Architecture.X64) => "version_stable_x64.json",
            (true, Architecture.X64) => "version_preview_x64.json",
            (false, Architecture.Arm64) => "version_stable_arm64.json",
            (true, Architecture.Arm64) => "version_preview_arm64.json",
            _ => throw new PlatformNotSupportedException($"{architecture} is not supported."),
        };
        var url = API_PREFIX + name;
        return await CommonGetAsync<ReleaseVersion>(url, cancellationToken);
    }



    public async Task<ReleaseVersion> GetReleaseAsync(bool isPrerelease, Architecture architecture, CancellationToken cancellationToken = default)
    {
        var name = (isPrerelease, architecture) switch
        {
            (false, Architecture.X64) => "release_stable_x64.json",
            (true, Architecture.X64) => "release_preview_x64.json",
            (false, Architecture.Arm64) => "release_stable_arm64.json",
            (true, Architecture.Arm64) => "release_preview_arm64.json",
            _ => throw new PlatformNotSupportedException($"{architecture} is not supported."),
        };
        var url = API_PREFIX + name;
        return await CommonGetAsync<ReleaseVersion>(url, cancellationToken);
    }





}
