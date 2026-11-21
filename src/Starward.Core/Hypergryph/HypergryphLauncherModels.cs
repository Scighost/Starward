using System.Text.Json;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace Starward.Core.Hypergryph;

public sealed class HypergryphBatchProxyResponse
{
    [JsonPropertyName("proxy_rsps")]
    public List<HypergryphProxyResponse> ProxyResponses { get; set; } = [];
}

public sealed class HypergryphProxyResponse
{
    [JsonPropertyName("kind")]
    public string Kind { get; set; } = "";

    [JsonPropertyName("get_latest_game_rsp")]
    public HypergryphLatestGame? LatestGame { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class HypergryphLatestGame
{
    [JsonPropertyName("action")]
    public int Action { get; set; }

    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("request_version")]
    public string RequestVersion { get; set; } = "";

    [JsonPropertyName("client_version")]
    public string ClientVersion { get; set; } = "";

    [JsonPropertyName("state")]
    public int State { get; set; }

    [JsonPropertyName("launcher_action")]
    public int LauncherAction { get; set; }

    [JsonPropertyName("pkg")]
    public HypergryphGamePackage Package { get; set; } = new();

    [JsonPropertyName("patch")]
    public HypergryphGamePatch? Patch { get; set; }

    [JsonPropertyName("pre_patch")]
    public HypergryphGamePatch? PrePatch { get; set; }
}

public sealed class HypergryphGamePackage
{
    [JsonPropertyName("packs")]
    public List<HypergryphPackagePart> Packs { get; set; } = [];

    [JsonPropertyName("total_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long TotalSize { get; set; }

    [JsonPropertyName("file_path")]
    public string FilePath { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("md5")]
    public string MD5 { get; set; } = "";

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }

    [JsonPropertyName("game_files_md5")]
    public string GameFilesMD5 { get; set; } = "";
}

public sealed class HypergryphGamePatch
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("target_version")]
    public string TargetVersion { get; set; } = "";

    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("md5")]
    public string MD5 { get; set; } = "";

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }

    [JsonPropertyName("total_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long TotalSize { get; set; }

    [JsonPropertyName("patches")]
    public List<HypergryphPackagePart> Patches { get; set; } = [];

    [JsonPropertyName("v2_patch_info_url")]
    public string V2PatchInfoUrl { get; set; } = "";

    [JsonPropertyName("v2_patch_info_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long V2PatchInfoSize { get; set; }

    [JsonPropertyName("v2_patch_info_md5")]
    public string V2PatchInfoMD5 { get; set; } = "";

    [JsonPropertyName("cd_key")]
    public string CDKey { get; set; } = "";

    [JsonPropertyName("v2_verify_files_url")]
    public string V2VerifyFilesUrl { get; set; } = "";

    [JsonPropertyName("v2_verify_files_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long V2VerifyFilesSize { get; set; }

    [JsonPropertyName("v2_verify_files_md5")]
    public string V2VerifyFilesMD5 { get; set; } = "";

    [JsonPropertyName("v2_verify_category_list")]
    public List<string> V2VerifyCategoryList { get; set; } = [];

    [JsonIgnore]
    public bool IsV2 => Patches.Count > 0 && !string.IsNullOrWhiteSpace(CDKey);

    [JsonIgnore]
    public IReadOnlyList<HypergryphPackagePart> DownloadParts => Patches.Count > 0
        ? Patches
        : string.IsNullOrWhiteSpace(Url)
            ? []
            : [new HypergryphPackagePart { Url = Url, MD5 = MD5, PackageSize = PackageSize }];

    public string GetFingerprint()
    {
        string value = string.Join('\n', DownloadParts.Select(x => $"{new Uri(x.Url).AbsolutePath}|{x.PackageSize}|{x.MD5}"));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }
}

public sealed class HypergryphPackagePart
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    [JsonPropertyName("md5")]
    public string MD5 { get; set; } = "";

    [JsonPropertyName("package_size")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long PackageSize { get; set; }

    public string GetFileName()
    {
        return Path.GetFileName(new Uri(Url).AbsolutePath);
    }
}

public sealed class HypergryphPatchManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "";

    [JsonPropertyName("vfs_base_path")]
    public string VfsBasePath { get; set; } = "";

    [JsonPropertyName("files")]
    public List<HypergryphPatchFile> Files { get; set; } = [];
}

public sealed class HypergryphV2VerifyManifest
{
    [JsonPropertyName("move")]
    public List<HypergryphVerifyFile> Move { get; set; } = [];

    [JsonPropertyName("patch")]
    public List<HypergryphVerifyFile> Patch { get; set; } = [];
}

public sealed class HypergryphVerifyFile
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = "";

    [JsonPropertyName("md5")]
    public string MD5 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }
}

public sealed class HypergryphPatchFile
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("name_path")]
    public string NamePath { get; set; } = "";

    [JsonPropertyName("md5")]
    public string MD5 { get; set; } = "";

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("diffType")]
    public int DiffType { get; set; }

    [JsonPropertyName("local_path")]
    public string LocalPath { get; set; } = "";

    [JsonPropertyName("patch")]
    public List<HypergryphPatchDiff> Patches { get; set; } = [];
}

public sealed class HypergryphPatchDiff
{
    [JsonPropertyName("base_file")]
    public string BaseFile { get; set; } = "";

    [JsonPropertyName("base_file_path")]
    public string BaseFilePath { get; set; } = "";

    [JsonPropertyName("base_md5")]
    public string BaseMD5 { get; set; } = "";

    [JsonPropertyName("base_size")]
    public long BaseSize { get; set; }

    [JsonPropertyName("patch")]
    public string Patch { get; set; } = "";

    [JsonPropertyName("patch_path")]
    public string PatchPath { get; set; } = "";

    [JsonPropertyName("patch_size")]
    public long PatchSize { get; set; }
}
