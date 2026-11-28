using Starward.Setup.Core.Github;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Setup.Core;

public class ReleaseClient
{
    public const string Repository = "NagiYume/Starward";
    public const string RepositoryUrl = "https://github.com/" + Repository;
    private const string ReleasesApi = "https://api.github.com/repos/" + Repository + "/releases";
    private const string ReleaseInfoAsset = "release-info.json";
    private const string DownloadPrefix = RepositoryUrl + "/releases/download/";

    public static string GetReleasePageUrl(string version) => $"{RepositoryUrl}/releases/tag/{GetReleaseTag(version)}";

    private static string GetReleaseTag(string version) =>
        Uri.EscapeDataString(version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}");


    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, List<GithubAsset>> _releaseAssets = new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<string, (Uri Url, DateTimeOffset Expires)> _cdnUrls = new(StringComparer.Ordinal);
    private readonly SemaphoreSlim _cdnUrlLock = new(1);

    public ReleaseClient(HttpClient httpClient)
    {
        if (httpClient is null)
        {
            _httpClient = new HttpClient(new SocketsHttpHandler
            {
                AutomaticDecompression = DecompressionMethods.All,
                EnableMultipleHttp2Connections = true,
                EnableMultipleHttp3Connections = true,
                PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            });
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{Path.GetFileNameWithoutExtension(Environment.ProcessPath)}/*");
        }
        else
        {
            _httpClient = httpClient;
        }
    }


    public async Task<ReleaseInfo> GetLatestReleaseInfoAsync(bool isPrerelease, string currentVersion, CancellationToken cancellationToken = default)
    {
        for (int page = 1; ; page++)
        {
            const int pageSize = 20;
            var releases = await GetGithubReleaseAsync(page, pageSize, cancellationToken).ConfigureAwait(false);
            var release = releases.FirstOrDefault(release => !release.Draft && (isPrerelease || !release.Prerelease)
                && release.Assets?.Any(asset => asset.Name == ReleaseInfoAsset) == true);
            if (release is not null)
            {
                return await ReadReleaseInfoAsync(release.TagName, cancellationToken).ConfigureAwait(false);
            }
            if (releases.Count < pageSize)
            {
                throw new HttpRequestException("No Starward release with update metadata was found.", null, HttpStatusCode.NotFound);
            }
        }
    }


    public async Task<ReleaseInfoDetail> GetLatestReleaseInfoDetailAsync(bool isPrerelease, string currentVersion, Architecture arch, InstallType type, CancellationToken cancellationToken = default)
    {
        var info = await GetLatestReleaseInfoAsync(isPrerelease, currentVersion, cancellationToken).ConfigureAwait(false);
        string key = $"{arch}-{type}".ToLower();
        if (info.Releases?.TryGetValue(key, out var value) ?? false)
        {
            return value;
        }
        else
        {
            throw new PlatformNotSupportedException($"Platform ({arch}, {type}) is not supported.");
        }
    }

    public async Task<ReleaseInfo> GetReleaseInfoAsync(string version, CancellationToken cancellationToken = default)
    {
        return await ReadReleaseInfoAsync(version, cancellationToken).ConfigureAwait(false);
    }


    private async Task<ReleaseInfo> ReadReleaseInfoAsync(string tag, CancellationToken cancellationToken)
    {
        var url = $"{RepositoryUrl}/releases/download/{GetReleaseTag(tag)}/{ReleaseInfoAsset}";
        return await ReadAssetJsonAsync(url, ReleaseJsonContext.Default.ReleaseInfo, cancellationToken).ConfigureAwait(false);
    }


    public async Task<ReleaseManifest> GetReleaseManifestAsync(string url, CancellationToken cancellationToken = default)
    {
        return await ReadAssetJsonAsync(url, ReleaseJsonContext.Default.ReleaseManifest, cancellationToken).ConfigureAwait(false);
    }


    private async Task<T> ReadAssetJsonAsync<T>(string url, JsonTypeInfo<T> type, CancellationToken cancellationToken) where T : class
    {
        using var response = await OpenAssetAsync(url, cancellationToken).ConfigureAwait(false);
        return await response.Content.ReadFromJsonAsync(type, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidDataException($"Cannot get JSON content from '{url}'.");
    }


    public async Task<HttpResponseMessage> DownloadReleaseFileAsync(ReleaseManifest manifest, ReleaseFile file,
        CancellationToken cancellationToken)
    {
        bool bundled = manifest.FileBundleUrl is not null && file.BundleOffset.HasValue;
        string url = bundled ? manifest.FileBundleUrl! : manifest.UrlPrefix + file.Id + (file.UrlSuffix ?? manifest.UrlSuffix);
        long? start = bundled ? file.BundleOffset : null;
        long? end = start + file.CompressedSize - 1;
        var response = await OpenAssetAsync(url, cancellationToken, start, end).ConfigureAwait(false);
        try
        {
            if (bundled && (response.StatusCode != HttpStatusCode.PartialContent
                || response.Content.Headers.ContentRange?.From != start || response.Content.Headers.ContentRange?.To != end))
            {
                throw new InvalidDataException($"Invalid download range for {file.Path}.");
            }
            return response;
        }
        catch
        {
            response.Dispose();
            throw;
        }
    }


    public async Task<HttpResponseMessage> OpenAssetAsync(string url, CancellationToken cancellationToken = default,
        long? rangeFrom = null, long? rangeTo = null)
    {
        RangeHeaderValue? range = rangeFrom.HasValue ? new RangeHeaderValue(rangeFrom, rangeTo) : null;
        if (!url.StartsWith(DownloadPrefix, StringComparison.Ordinal))
        {
            return await SendDownloadAsync(new Uri(url), range, false, cancellationToken).ConfigureAwait(false);
        }

        string[] assetPath = url[DownloadPrefix.Length..].Split('/', 2);
        if (assetPath.Length != 2)
        {
            throw new InvalidDataException($"Invalid asset URL: {url}");
        }
        string tag = Uri.UnescapeDataString(assetPath[0]);
        if (!_releaseAssets.TryGetValue(tag, out var assets))
        {
            assets = (await GetGithubReleaseAsync(tag, cancellationToken).ConfigureAwait(false))?.Assets;
        }
        var asset = assets?.Find(item => item.Name == Uri.UnescapeDataString(assetPath[1]));
        if (asset is null)
        {
            throw new HttpRequestException($"Release asset was not found: {url}", null, HttpStatusCode.NotFound);
        }

        var apiUrl = new Uri(asset.Url + "?download=1");
        if (!rangeTo.HasValue)
        {
            return await SendDownloadAsync(apiUrl, range, true, cancellationToken).ConfigureAwait(false);
        }

        // Byte ranges for one bundle reuse its CDN redirect without repeatedly calling the API.
        Uri downloadUrl;
        await _cdnUrlLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!_cdnUrls.TryGetValue(asset.Url, out var cached) || cached.Expires <= DateTimeOffset.UtcNow)
            {
                var response = await SendDownloadAsync(apiUrl, range, true, cancellationToken).ConfigureAwait(false);
                if (response.RequestMessage?.RequestUri is { } cdnUrl && cdnUrl.Host != apiUrl.Host)
                {
                    _cdnUrls[asset.Url] = (cdnUrl, DateTimeOffset.UtcNow.AddMinutes(5));
                }
                return response;
            }
            downloadUrl = cached.Url;
        }
        finally
        {
            _cdnUrlLock.Release();
        }
        try
        {
            return await SendDownloadAsync(downloadUrl, range, false, cancellationToken).ConfigureAwait(false);
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden or HttpStatusCode.NotFound)
        {
            _cdnUrls.TryRemove(asset.Url, out _);
            return await SendDownloadAsync(apiUrl, range, true, cancellationToken).ConfigureAwait(false);
        }
    }


    private async Task<HttpResponseMessage> SendDownloadAsync(Uri url, RangeHeaderValue? range, bool api,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, url) { VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
        request.Headers.Range = range;
        if (api)
        {
            request.Headers.Accept.ParseAdd("application/octet-stream");
        }
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        if (response.IsSuccessStatusCode)
        {
            return response;
        }
        response.Dispose();
        throw new HttpRequestException($"Failed to download the Starward release asset ({(int)response.StatusCode}).", null, response.StatusCode);
    }



    #region Github



    public async Task<GithubRelease?> GetGithubLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        return (await GetGithubReleaseAsync(1, 1, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }



    public async Task<List<GithubRelease>> GetGithubReleaseAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        string url = $"{ReleasesApi}?page={page}&per_page={perPage}";
        var list = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.ListGithubRelease, cancellationToken).ConfigureAwait(false);
        foreach (var release in list ?? [])
        {
            _releaseAssets[release.TagName] = release.Assets ?? [];
        }
        return list ?? new List<GithubRelease>();
    }



    public async Task<GithubRelease?> GetGithubReleaseAsync(string tag, CancellationToken cancellationToken = default)
    {
        string url = $"{ReleasesApi}/tags/{GetReleaseTag(tag)}";
        var release = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.GithubRelease, cancellationToken).ConfigureAwait(false);
        if (release is not null)
        {
            _releaseAssets[release.TagName] = release.Assets ?? [];
        }
        return release;
    }


    public async Task<string> RenderGithubMarkdownAsync(string markdown, CancellationToken cancellationToken = default)
    {
        const string url = "https://api.github.com/markdown";
        var request = new GithubMarkdownRequest
        {
            Text = markdown,
            Mode = "gfm",
            Context = Repository,
        };
        var content = new StringContent(JsonSerializer.Serialize(request, ReleaseJsonContext.Default.GithubMarkdownRequest), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
    }



    #endregion


}
