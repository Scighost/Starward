using System.Text.Json;
using System.Text.Json.Serialization;

namespace Starward.Core.Hypergryph;

public sealed class HypergryphInstallMetadata
{
    public const string MetadataDirectoryName = ".starward";

    public const string MetadataFileName = "hypergryph.json";

    public const string DownloadsDirectoryName = "downloads";

    [JsonPropertyName("app_code")]
    public string AppCode { get; set; } = "";

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("game_files_md5")]
    public string GameFilesMD5 { get; set; } = "";

    [JsonPropertyName("predownload_fingerprint")]
    public string PredownloadFingerprint { get; set; } = "";

    public bool IsFor(GameBiz gameBiz)
    {
        HypergryphGameProfile profile = HypergryphGameConstants.GetGameProfile(gameBiz);
        return string.Equals(AppCode, profile.GameAppCode, StringComparison.Ordinal);
    }

    public static string GetMetadataDirectory(string installPath)
    {
        return Path.Combine(installPath, MetadataDirectoryName);
    }

    public static string GetMetadataPath(string installPath)
    {
        return Path.Combine(GetMetadataDirectory(installPath), MetadataFileName);
    }

    public static string GetDownloadsDirectory(string installPath)
    {
        return Path.Combine(GetMetadataDirectory(installPath), DownloadsDirectoryName);
    }

    public static async Task<HypergryphInstallMetadata?> ReadAsync(string installPath, CancellationToken cancellationToken = default)
    {
        string path = GetMetadataPath(installPath);
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            await using FileStream stream = File.OpenRead(path);
            return await JsonSerializer.DeserializeAsync<HypergryphInstallMetadata>(stream, cancellationToken: cancellationToken);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task WriteAsync(string installPath, CancellationToken cancellationToken = default)
    {
        string directory = GetMetadataDirectory(installPath);
        Directory.CreateDirectory(directory);
        string path = GetMetadataPath(installPath);
        string temporaryPath = path + ".tmp";
        await using (FileStream stream = File.Create(temporaryPath))
        {
            await JsonSerializer.SerializeAsync(stream, this, cancellationToken: cancellationToken);
        }
        File.Move(temporaryPath, path, true);
    }
}
