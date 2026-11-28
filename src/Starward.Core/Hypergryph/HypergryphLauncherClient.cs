using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Starward.Core.HoYoPlay;

namespace Starward.Core.Hypergryph;

public sealed class HypergryphLauncherClient
{
    private readonly HttpClient _httpClient;

    public HypergryphLauncherClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<HypergryphLatestGame> GetLatestGameAsync(
        GameId gameId,
        string? localVersion,
        CancellationToken cancellationToken = default)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameId);
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

    public Task<HypergryphLauncherContent> GetGameContentAsync(
        GameId gameId,
        string? language,
        CancellationToken cancellationToken = default) =>
        GetWebContentAsync(gameId, language, artworkOnly: false, cancellationToken);

    public Task<HypergryphLauncherContent> GetGameArtworkAsync(
        GameId gameId,
        string? language,
        CancellationToken cancellationToken = default) =>
        GetWebContentAsync(gameId, language, artworkOnly: true, cancellationToken);

    private async Task<HypergryphLauncherContent> GetWebContentAsync(
        GameId gameId,
        string? language,
        bool artworkOnly,
        CancellationToken cancellationToken)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameId);
        var contentRequest = new HypergryphWebContentRequest
        {
            AppCode = profile.GameAppCode,
            Language = LanguageUtil.FilterLanguage(language),
            Channel = profile.Channel,
            SubChannel = profile.SubChannel,
        };
        var mainBackground = new HypergryphWebProxyRequest
        {
            Kind = "get_main_bg_image",
            MainBackgroundRequest = contentRequest,
        };
        var request = new HypergryphWebBatchProxyRequest
        {
            ProxyRequests = artworkOnly
                ? [mainBackground, new() { Kind = "get_game_image", ArtworkRequest = contentRequest }]
                : [mainBackground, new() { Kind = "get_banner", BannerRequest = contentRequest },
                    new() { Kind = "get_announcement", AnnouncementRequest = contentRequest }],
        };

        using HttpResponseMessage response = await _httpClient.PostAsJsonAsync(profile.WebBatchProxyUrl, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        HypergryphWebBatchProxyResponse result = await response.Content.ReadFromJsonAsync<HypergryphWebBatchProxyResponse>(cancellationToken)
            ?? throw new InvalidDataException($"The Hypergryph launcher returned no content information for {profile.GameAppCode}.");

        return new HypergryphLauncherContent
        {
            Artwork = result.ProxyResponses.FirstOrDefault(x => x.Kind is "get_game_image")?.Artwork,
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

    public async Task<bool> HasEndfieldPackageAsync(GameId gameId, CancellationToken cancellationToken)
    {
        try
        {
            HypergryphLatestGame latest = await GetLatestGameAsync(gameId, null, cancellationToken);
            if (latest.Package is not { Packs.Count: > 0 } package || string.IsNullOrWhiteSpace(package.GameFilesMD5))
            {
                return false;
            }
            ReadEndfieldGameFiles(await GetGameFilesAsync(package, cancellationToken));
            return true;
        }
        catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
        {
            return false;
        }
        catch (Exception ex) when (ex is InvalidDataException or JsonException or CryptographicException)
        {
            return false;
        }
    }

    public static IReadOnlyList<HypergryphVerifyFile> ReadEndfieldGameFiles(byte[] encrypted)
    {
        // The launcher uses the same AES-CBC format across Endfield channels.
        using Aes aes = Aes.Create();
        aes.Key = Convert.FromHexString("C0F30E1CE763BBC21CC355A34303AC50399444BFF68C4A22AF398C0A166EE143");
        byte[] decrypted = aes.DecryptCbc(encrypted, Convert.FromHexString("33467861192750649501937264608400"));
        using StreamReader reader = new(new MemoryStream(decrypted), Encoding.UTF8);
        var files = new List<HypergryphVerifyFile>();
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is string line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }
            HypergryphVerifyFile file = JsonSerializer.Deserialize<HypergryphVerifyFile>(line)
                ?? throw new InvalidDataException("The Endfield game_files manifest contains an empty entry.");
            file.Path = file.Path?.Replace('\\', '/') ?? "";
            string[] segments = file.Path.Split('/');
            if (file.Size < 0 || file.MD5 is not { Length: 32 } || !file.MD5.All(Uri.IsHexDigit)
                || segments.Any(x => x.Length == 0 || x.EndsWith('.') || x.EndsWith(' ') || x.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
                || segments[0].Equals(".starward", StringComparison.OrdinalIgnoreCase)
                || file.Path.Equals("game_files", StringComparison.OrdinalIgnoreCase)
                || !paths.Add(file.Path))
            {
                throw new InvalidDataException($"The Endfield game_files manifest contains an invalid entry: {file.Path}.");
            }
            files.Add(file);
        }
        if (!paths.Contains(HypergryphGameConstants.EndfieldExeName))
        {
            throw new InvalidDataException("The Endfield game_files manifest does not include the game executable.");
        }
        return files;
    }

    public Task<byte[]> GetV2VerifyFilesAsync(HypergryphGamePatch patch, CancellationToken cancellationToken = default)
    {
        return GetVerifiedBytesAsync(patch.V2VerifyFilesUrl, patch.V2VerifyFilesSize, patch.V2VerifyFilesMD5, cancellationToken);
    }

    public async Task<string?> GetEndfieldMainResourceIndexAsync(GameId gameId, string installedManifestMD5, CancellationToken cancellationToken)
    {
        HypergryphLatestGame latest = await GetLatestGameAsync(gameId, null, cancellationToken);
        return string.Equals(latest.Package.GameFilesMD5, installedManifestMD5, StringComparison.OrdinalIgnoreCase)
            ? await GetEndfieldMainResourceIndexAsync(gameId, latest.Package, cancellationToken)
            : null;
    }

    public async Task<string?> GetEndfieldMainResourceIndexAsync(GameId gameId, HypergryphGamePackage package, CancellationToken cancellationToken)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameId);
        if (!Uri.TryCreate(package.FilePath, UriKind.Absolute, out Uri? packageUri)
            || !Uri.TryCreate(profile.BatchProxyUrl, UriKind.Absolute, out Uri? proxyUri))
        {
            return null;
        }
        string[] path = packageUri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (path.Length < 4)
        {
            return null;
        }
        string[] seed = path[^2].Split('_', 2);
        if (seed.Length != 2)
        {
            return null;
        }
        string endpoint = new Uri(proxyUri, "/api/game/get_latest_resources").AbsoluteUri;
        string query = $"appcode={profile.GameAppCode}&platform=Windows&game_version={Uri.EscapeDataString(path[1])}"
            + $"&version={Uri.EscapeDataString(seed[0])}&rand_str={Uri.EscapeDataString(seed[1])}";
        using Stream response = await _httpClient.GetStreamAsync($"{endpoint}?{query}", cancellationToken);
        using JsonDocument document = await JsonDocument.ParseAsync(response, cancellationToken: cancellationToken);
        foreach (JsonElement resource in document.RootElement.GetProperty("resources").EnumerateArray())
        {
            if (resource.GetProperty("name").GetString() == "main"
                && resource.GetProperty("path").GetString() is string resourcePath)
            {
                return await _httpClient.GetStringAsync($"{resourcePath.TrimEnd('/')}/index_main.json", cancellationToken);
            }
        }
        return null;
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

        [JsonPropertyName("get_game_image_req")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public HypergryphWebContentRequest? ArtworkRequest { get; set; }

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

        [JsonPropertyName("get_game_image_rsp")]
        public HypergryphGameArtwork? Artwork { get; set; }

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
