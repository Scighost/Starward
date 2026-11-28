using Starward.Setup.Core.Github;
using System;
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
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Setup.Core;

public class ReleaseClient
{
    public const string Repository = "NagiYume/Starward";
    public const string RepositoryUrl = "https://github.com/" + Repository;
    private const string ReleasesApi = "https://api.github.com/repos/" + Repository + "/releases";
    private const string ReleaseInfoAsset = "release-info.json";

    public static string GetReleasePageUrl(string version) => $"{RepositoryUrl}/releases/tag/{GetReleaseTag(version)}";

    private static string GetReleaseTag(string version) =>
        Uri.EscapeDataString(version.StartsWith("v", StringComparison.OrdinalIgnoreCase) ? version : $"v{version}");


    private readonly HttpClient _httpClient;

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
        var info = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.ReleaseInfo, cancellationToken).ConfigureAwait(false);
        return info ?? throw new InvalidDataException($"Cannot get JSON content from '{url}'.");
    }


    public async Task<ReleaseManifest> GetReleaseManifestAsync(string url, CancellationToken cancellationToken = default)
    {
        var manifest = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.ReleaseManifest, cancellationToken).ConfigureAwait(false);
        return manifest ?? throw new NullReferenceException($"Cannot get json content from '{url}'.");
    }


    public static async Task<HttpResponseMessage> DownloadReleaseFileAsync(HttpClient client, ReleaseManifest manifest,
        ReleaseFile file, CancellationToken cancellationToken)
    {
        bool bundled = manifest.FileBundleUrl is not null && file.BundleOffset.HasValue;
        string url = bundled ? manifest.FileBundleUrl! : manifest.UrlPrefix + file.Id + (file.UrlSuffix ?? manifest.UrlSuffix);
        using var request = new HttpRequestMessage(HttpMethod.Get, url) { VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
        long? start = file.BundleOffset;
        long? end = start + file.CompressedSize - 1;
        if (bundled)
        {
            request.Headers.Range = new RangeHeaderValue(start, end);
        }
        var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        try
        {
            response.EnsureSuccessStatusCode();
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



    #region Github



    public async Task<GithubRelease?> GetGithubLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        return (await GetGithubReleaseAsync(1, 1, cancellationToken).ConfigureAwait(false)).FirstOrDefault();
    }



    public async Task<List<GithubRelease>> GetGithubReleaseAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        string url = $"{ReleasesApi}?page={page}&per_page={perPage}";
        var list = await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.ListGithubRelease, cancellationToken).ConfigureAwait(false);
        return list ?? new List<GithubRelease>();
    }



    public async Task<GithubRelease?> GetGithubReleaseAsync(string tag, CancellationToken cancellationToken = default)
    {
        string url = $"{ReleasesApi}/tags/{GetReleaseTag(tag)}";
        return await _httpClient.GetFromJsonAsync(url, ReleaseJsonContext.Default.GithubRelease, cancellationToken).ConfigureAwait(false);
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
