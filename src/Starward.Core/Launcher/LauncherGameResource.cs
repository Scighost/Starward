using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;


public class LauncherGameResource
{
    [JsonPropertyName("game")]
    public GameResource Game { get; set; }

    [JsonPropertyName("plugin")]
    public Plugin Plugin { get; set; }

    [JsonPropertyName("web_url")]
    public string WebUrl { get; set; }

    [JsonPropertyName("force_update")]
    public object ForceUpdate { get; set; }

    [JsonPropertyName("pre_download_game")]
    public GameResource PreDownloadGame { get; set; }

    [JsonPropertyName("deprecated_packages")]
    public List<DeprecatedPackage> DeprecatedPackages { get; set; }

    [JsonPropertyName("sdk")]
    public GameSDK Sdk { get; set; }

    [JsonPropertyName("deprecated_files")]
    public List<DeprecatedFile> DeprecatedFiles { get; set; }
}

public class GameSDK
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("pkg_version")]
    public string PkgVersion { get; set; }

    [JsonPropertyName("desc")]
    public string Desc { get; set; }

    [JsonPropertyName("channel_id")]
    public string ChannelId { get; set; }

    [JsonPropertyName("sub_channel_id")]
    public string SubChannelId { get; set; }

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }
}


public class DeprecatedFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }
}

public class DeprecatedPackage
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }
}

public class DiffPackage : IGamePackage
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("is_recommended_update")]
    public bool IsRecommendedUpdate { get; set; }

    [JsonPropertyName("voice_packs")]
    public List<VoicePack> VoicePacks { get; set; }

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }
}

public class GameResource
{
    [JsonPropertyName("latest")]
    public LatestVersion Latest { get; set; }

    [JsonPropertyName("diffs")]
    public List<DiffPackage> Diffs { get; set; }
}

public class LatestVersion : IGamePackage
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("entry")]
    public string Entry { get; set; }

    [JsonPropertyName("voice_packs")]
    public List<VoicePack> VoicePacks { get; set; }

    [JsonPropertyName("decompressed_path")]
    public string DecompressedPath { get; set; }

    [JsonPropertyName("segments")]
    public List<Segment> Segments { get; set; }

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }
}

public class Plugin
{
    [JsonPropertyName("plugins")]
    public List<PluginItem> Plugins { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }
}

public class PluginItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("entry")]
    public string Entry { get; set; }
}


public class Segment
{
    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }
}

public class VoicePack : IGamePackage
{
    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }
}



public interface IGamePackage
{

    public string Path { get; set; }

    public long Size { get; set; }

    public long PackageSize { get; set; }

}