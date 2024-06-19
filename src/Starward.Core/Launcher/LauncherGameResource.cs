using System.Text.Json.Serialization;

namespace Starward.Core.Launcher;


public class LauncherGameResource
{
    [JsonPropertyName("game_packages")]
    public List<GamePackagesWrapper> Resources { get; set; }
}

public class GamePackagesWrapper
{
    [JsonPropertyName("main")]
    public GameBranch Main { get; set; }

    [JsonPropertyName("pre_download")]
    public GameBranch PreDownload { get; set; }
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

public class LauncherGameSdk
{
    [JsonPropertyName("game_channel_sdks")]
    public List<GameSDK> Sdk { get; set; }
}

public class GameSDK
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("channel_sdk_pkg")]
    public GamePkg Pkg { get; set; }
}

public class LauncherGameDeprecatedFiles
{
    [JsonPropertyName("deprecated_file_configs")]
    public List<GameDeprecatedFilesWrapper> Resources { get; set; }
}

public class GameDeprecatedFilesWrapper
{
    [JsonPropertyName("deprecated_files")]
    public List<DeprecatedFile> DeprecatedFiles { get; set; }
}

public class DeprecatedFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; }
}

public interface IGamePackage
{

    public string Url { get; set; }

    public long Size { get; set; }

    public long DecompressedSize { get; set; }

}