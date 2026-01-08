using Starward.Setup.Core.Github;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Setup.Core;

public class ReleaseClient
{
    public static Uri DefaultBaseAddress { get; set; } = new("https://starward-release.scighost.com");


    private readonly HttpClient _httpClient;

    public ReleaseClient(HttpClient httpClient)
    {
        if (httpClient is null)
        {
            _httpClient = new HttpClient(
#if NET9_0_OR_GREATER
                new SocketsHttpHandler
                {
                    AutomaticDecompression = DecompressionMethods.All,
                    EnableMultipleHttp2Connections = true,
                    EnableMultipleHttp3Connections = true,
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5)
#else
                new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
#endif
                });
            _httpClient.DefaultRequestHeaders.Add("User-Agent", $"{Process.GetCurrentProcess().ProcessName}/*");
        }
        else
        {
            _httpClient = httpClient;
        }
        _httpClient.BaseAddress = DefaultBaseAddress;
    }


    public async Task<ReleaseInfo> GetLatestReleaseInfoAsync(bool isPrerelease, string appVersion, CancellationToken cancellationToken = default)
    {
        var url = isPrerelease switch
        {
            false => $"/release/latest?version={appVersion}",
            true => $"/release/latest-preview?version={appVersion}",
        };
        var info = await _httpClient.GetFromJsonAsync(url, typeof(ReleaseInfo), ReleaseJsonContext.Default, cancellationToken);
        return info as ReleaseInfo ?? throw new NullReferenceException($"Cannot get json content from '{url}'.");
    }


    public async Task<ReleaseInfoDetail> GetLatestReleaseInfoDetailAsync(bool isPrerelease, string appVersion, Architecture arch, InstallType type, CancellationToken cancellationToken = default)
    {
        var url = isPrerelease switch
        {
            false => $"/release/latest?version={appVersion}",
            true => $"/release/latest-preview?version={appVersion}",
        };
        var info = await _httpClient.GetFromJsonAsync(url, typeof(ReleaseInfo), ReleaseJsonContext.Default, cancellationToken) as ReleaseInfo;
        if (info is null)
        {
            throw new NullReferenceException($"Cannot get json content from '{url}'.");
        }
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
        var url = $"/release/version/{version}";
        var info = await _httpClient.GetFromJsonAsync(url, typeof(ReleaseInfo), ReleaseJsonContext.Default, cancellationToken);
        return info as ReleaseInfo ?? throw new NullReferenceException($"Cannot get json content from '{url}'.");
    }


    public async Task<ReleaseManifest> GetReleaseManifestAsync(string url, CancellationToken cancellationToken = default)
    {
        var manifest = await _httpClient.GetFromJsonAsync(url, typeof(ReleaseManifest), ReleaseJsonContext.Default, cancellationToken);
        return manifest as ReleaseManifest ?? throw new NullReferenceException($"Cannot get json content from '{url}'.");
    }



    #region Github



    public async Task<GithubRelease?> GetGithubLatestReleaseAsync(CancellationToken cancellationToken = default)
    {
        const string url = "https://api.github.com/repos/Scighost/Starward/releases?page=1&per_page=1";
        var list = await _httpClient.GetFromJsonAsync(url, typeof(List<GithubRelease>), ReleaseJsonContext.Default, cancellationToken) as List<GithubRelease>;
        return list?.FirstOrDefault();
    }



    public async Task<List<GithubRelease>> GetGithubReleaseAsync(int page, int perPage, CancellationToken cancellationToken = default)
    {
        string url = $"https://api.github.com/repos/Scighost/Starward/releases?page={page}&per_page={perPage}";
        var list = await _httpClient.GetFromJsonAsync(url, typeof(List<GithubRelease>), ReleaseJsonContext.Default, cancellationToken) as List<GithubRelease>;
        return list ?? new List<GithubRelease>();
    }



    public async Task<GithubRelease?> GetGithubReleaseAsync(string tag, CancellationToken cancellationToken = default)
    {
        string url = $"https://api.github.com/repos/Scighost/Starward/releases/tags/{tag}";
        return await _httpClient.GetFromJsonAsync(url, typeof(GithubRelease), ReleaseJsonContext.Default, cancellationToken) as GithubRelease;
    }


    public async Task<string> RenderGithubMarkdownAsync(string markdown, CancellationToken cancellationToken = default)
    {
        const string url = "https://api.github.com/markdown";
        var request = new GithubMarkdownRequest
        {
            Text = markdown,
            Mode = "gfm",
            Context = "Scighost/Starward",
        };
        var content = new StringContent(JsonSerializer.Serialize(request, typeof(GithubMarkdownRequest), ReleaseJsonContext.Default), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }



    #endregion


}
