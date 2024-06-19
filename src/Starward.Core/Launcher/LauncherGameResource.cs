using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;

public class LauncherGameResource
{
    [JsonPropertyName("game_packages")]
    public List<Resource> Resources { get; set; }
}

public class Resource
{
    [JsonPropertyName("game")]
    public GameInfo Game { get; set; }

    [JsonPropertyName("main")]
    public GameBranch Main { get; set; }

    [JsonPropertyName("pre_download")]
    public GameBranch PreDownload { get; set; }

    public GameSDK Sdk { get; set; } // TODO: Adapt to the new SDK API
}

public class GameInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("biz")]
    public string Biz { get; set; }
}

public class GameBranch
{
    [JsonPropertyName("major")]
    public GamePackages Major { get; set; }

    [JsonPropertyName("patches")]
    public List<GamePackages> Patches { get; set; }
}

public class GamePackages
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("game_pkgs")]
    public List<GamePkg> GamePkgs { get; set; }

    [JsonPropertyName("audio_pkgs")]
    public List<AudioPkg> AudioPkgs { get; set; }

    [JsonPropertyName("res_list_url")]
    public string ResListUrl { get; set; }
}

public class GamePkg : IGamePackage
{
    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    [JsonPropertyName("decompressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long DecompressedSize { get; set; }
}

public class AudioPkg : IGamePackage
{
    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }

    [JsonPropertyName("md5")]
    public string Md5 { get; set; }

    [JsonPropertyName("size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long Size { get; set; }

    [JsonPropertyName("decompressed_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long DecompressedSize { get; set; }
}

// TODO: Adapt to the new SDK API
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

public interface IGamePackage
{

    public string Url { get; set; }

    public long Size { get; set; }

    public long DecompressedSize { get; set; }

}