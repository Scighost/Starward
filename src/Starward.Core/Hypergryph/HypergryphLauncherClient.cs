using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.Hypergryph;

public sealed class HypergryphLauncherClient
{
    private readonly HttpClient _httpClient;

    public HypergryphLauncherClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HypergryphLatestGame> GetLatestGameAsync(
        GameBiz gameBiz,
        string? localVersion,
        CancellationToken cancellationToken = default)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameBiz);
        var request = new HypergryphBatchProxyRequest
        {
            Sequence = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(),
            ProxyRequests =
            [
                new HypergryphProxyRequest
                {
                    LatestGameRequest = new HypergryphLatestGameRequest
                    {
                        AppCode = profile.GameAppCode,
                        Channel = profile.Channel,
                        LauncherAppCode = profile.LauncherAppCode,
                        SubChannel = profile.SubChannel,
                        Version = localVersion ?? "",
                    },
                },
            ],
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(profile.BatchProxyUrl, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        HypergryphBatchProxyResponse? result = await response.Content.ReadFromJsonAsync<HypergryphBatchProxyResponse>(cancellationToken);
        return result?.ProxyResponses.FirstOrDefault(x => x.Kind is "get_latest_game")?.LatestGame
            ?? throw new InvalidDataException($"The Hypergryph launcher returned no package information for {profile.GameAppCode}.");
    }

    public async Task<HypergryphLauncherContent> GetGameContentAsync(
        GameBiz gameBiz,
        string? language,
        CancellationToken cancellationToken = default)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameBiz);
        var contentRequest = new HypergryphWebContentRequest
        {
            AppCode = profile.GameAppCode,
            Language = LanguageUtil.FilterLanguage(language),
            Channel = profile.Channel,
            SubChannel = profile.SubChannel,
        };
        var request = new HypergryphWebBatchProxyRequest
        {
            ProxyRequests =
            [
                new HypergryphWebProxyRequest
                {
                    Kind = "get_main_bg_image",
                    MainBackgroundRequest = contentRequest,
                },
                new HypergryphWebProxyRequest
                {
                    Kind = "get_banner",
                    BannerRequest = contentRequest,
                },
                new HypergryphWebProxyRequest
                {
                    Kind = "get_announcement",
                    AnnouncementRequest = contentRequest,
                },
            ],
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(profile.WebBatchProxyUrl, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        HypergryphWebBatchProxyResponse result = await response.Content.ReadFromJsonAsync<HypergryphWebBatchProxyResponse>(cancellationToken)
            ?? throw new InvalidDataException($"The Hypergryph launcher returned no content information for {profile.GameAppCode}.");

        return new HypergryphLauncherContent
        {
            MainBackground = result.ProxyResponses.FirstOrDefault(x => x.Kind is "get_main_bg_image")?.MainBackgroundResponse?.MainBackground,
            Banners = result.ProxyResponses.FirstOrDefault(x => x.Kind is "get_banner")?.BannerResponse?.Banners ?? [],
            AnnouncementTabs = result.ProxyResponses.FirstOrDefault(x => x.Kind is "get_announcement")?.AnnouncementResponse?.Tabs ?? [],
        };
    }

    public async Task<HypergryphPatchManifest> GetPatchManifestAsync(HypergryphGamePatch patch, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(patch.V2PatchInfoUrl))
        {
            throw new InvalidDataException("The Hypergryph patch manifest URL is empty.");
        }
        byte[] bytes = await GetVerifiedBytesAsync(patch.V2PatchInfoUrl, patch.V2PatchInfoSize, patch.V2PatchInfoMD5, cancellationToken);
        return JsonSerializer.Deserialize<HypergryphPatchManifest>(bytes)
            ?? throw new InvalidDataException("The Hypergryph patch manifest is empty.");
    }

    public Task<byte[]> GetGameFilesAsync(HypergryphGamePackage package, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(package.FilePath))
        {
            throw new InvalidDataException("The Hypergryph game files path is empty.");
        }
        string url = $"{package.FilePath.TrimEnd('/')}/game_files";
        return GetVerifiedBytesAsync(url, 0, package.GameFilesMD5, cancellationToken);
    }

    public Task<byte[]> GetV2VerifyFilesAsync(HypergryphGamePatch patch, CancellationToken cancellationToken = default)
    {
        return GetVerifiedBytesAsync(patch.V2VerifyFilesUrl, patch.V2VerifyFilesSize, patch.V2VerifyFilesMD5, cancellationToken);
    }

    private async Task<byte[]> GetVerifiedBytesAsync(string url, long size, string md5, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            throw new InvalidDataException("The Hypergryph file URL is empty.");
        }
        using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
        byte[] bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        if (size > 0 && bytes.LongLength != size)
        {
            throw new InvalidDataException("The Hypergryph file size does not match.");
        }
        string actualMD5 = Convert.ToHexStringLower(MD5.HashData(bytes));
        if (!string.IsNullOrWhiteSpace(md5) && !string.Equals(actualMD5, md5, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("The Hypergryph file MD5 does not match.");
        }
        return bytes;
    }

    private sealed class HypergryphBatchProxyRequest
    {
        [JsonPropertyName("proxy_reqs")]
        public List<HypergryphProxyRequest> ProxyRequests { get; set; } = [];

        [JsonPropertyName("seq")]
        public string Sequence { get; set; } = "";
    }

    private sealed class HypergryphProxyRequest
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "get_latest_game";

        [JsonPropertyName("get_latest_game_req")]
        public HypergryphLatestGameRequest LatestGameRequest { get; set; } = new();
    }

    private sealed class HypergryphLatestGameRequest
    {
        [JsonPropertyName("appcode")]
        public string AppCode { get; set; } = "";

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = "";

        [JsonPropertyName("launcher_appcode")]
        public string LauncherAppCode { get; set; } = "";

        [JsonPropertyName("sub_channel")]
        public string SubChannel { get; set; } = "";

        [JsonPropertyName("version")]
        public string Version { get; set; } = "";
    }

    private sealed class HypergryphWebBatchProxyRequest
    {
        [JsonPropertyName("proxy_reqs")]
        public List<HypergryphWebProxyRequest> ProxyRequests { get; set; } = [];
    }

    private sealed class HypergryphWebProxyRequest
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "";

        [JsonPropertyName("get_banner_req")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HypergryphWebContentRequest? BannerRequest { get; set; }

        [JsonPropertyName("get_announcement_req")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HypergryphWebContentRequest? AnnouncementRequest { get; set; }

        [JsonPropertyName("get_main_bg_image_req")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HypergryphWebContentRequest? MainBackgroundRequest { get; set; }
    }

    private sealed class HypergryphWebContentRequest
    {
        [JsonPropertyName("appcode")]
        public string AppCode { get; set; } = "";

        [JsonPropertyName("language")]
        public string Language { get; set; } = "";

        [JsonPropertyName("channel")]
        public string Channel { get; set; } = "";

        [JsonPropertyName("sub_channel")]
        public string SubChannel { get; set; } = "";

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = "Windows";

        [JsonPropertyName("source")]
        public string Source { get; set; } = "launcher";
    }

    private sealed class HypergryphWebBatchProxyResponse
    {
        [JsonPropertyName("proxy_rsps")]
        public List<HypergryphWebProxyResponse> ProxyResponses { get; set; } = [];
    }

    private sealed class HypergryphWebProxyResponse
    {
        [JsonPropertyName("kind")]
        public string Kind { get; set; } = "";

        [JsonPropertyName("get_banner_rsp")]
        public HypergryphBannerResponse? BannerResponse { get; set; }

        [JsonPropertyName("get_announcement_rsp")]
        public HypergryphAnnouncementResponse? AnnouncementResponse { get; set; }

        [JsonPropertyName("get_main_bg_image_rsp")]
        public HypergryphMainBackgroundResponse? MainBackgroundResponse { get; set; }
    }

    private sealed class HypergryphMainBackgroundResponse
    {
        [JsonPropertyName("main_bg_image")]
        public HypergryphMainBackground? MainBackground { get; set; }
    }

    private sealed class HypergryphBannerResponse
    {
        [JsonPropertyName("banners")]
        public List<HypergryphContentBanner> Banners { get; set; } = [];
    }

    private sealed class HypergryphAnnouncementResponse
    {
        [JsonPropertyName("tabs")]
        public List<HypergryphAnnouncementTab> Tabs { get; set; } = [];
    }
}
