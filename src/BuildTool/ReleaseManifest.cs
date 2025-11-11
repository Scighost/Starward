using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace BuildTool;


public class ReleaseManifest
{
    [JsonPropertyName("version")]
    public string Version { get; set; }

    [JsonPropertyName("architecture")]
    [JsonConverter(typeof(JsonStringIgnoreCaseEnumConverter<Architecture>))]
    public Architecture Architecture { get; set; }

    [JsonPropertyName("install_type")]
    [JsonConverter(typeof(JsonStringIgnoreCaseEnumConverter<InstallType>))]
    public InstallType InstallType { get; set; }

    [JsonPropertyName("file_count")]
    public int FileCount { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("compressed_size")]
    public long CompressedSize { get; set; }

    [JsonPropertyName("diff_version")]
    public string? DiffVersion { get; set; }

    [JsonPropertyName("diff_file_count")]
    public int DiffFileCount { get; set; }

    [JsonPropertyName("diff_size")]
    public long DiffSize { get; set; }

    [JsonPropertyName("url_prefix")]
    public string UrlPrefix { get; set; }

    [JsonPropertyName("files")]
    public List<ReleaseFile> Files { get; set; }

    [JsonPropertyName("delete_files")]
    public List<string> DeleteFiles { get; set; }

}


public class ReleaseFile
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("path")]
    public string Path { get; set; }

    [JsonPropertyName("size")]
    public long Size { get; set; }

    [JsonPropertyName("compressed_size")]
    public long CompressedSize { get; set; }

    [JsonPropertyName("hash")]
    public string Hash { get; set; }

    [JsonPropertyName("compressed_hash")]
    public string CompressedHash { get; set; }

    [JsonPropertyName("patch")]
    public ReleaseFilePatch? Patch { get; set; }
}


public class ReleaseFilePatch
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("old_path")]
    public string OldPath { get; set; }

    [JsonPropertyName("old_file_size")]
    public long OldFileSize { get; set; }

    [JsonPropertyName("old_file_hash")]
    public string OldFileHash { get; set; }

    [JsonPropertyName("patch_size")]
    public long PatchSize { get; set; }

    [JsonPropertyName("patch_hash")]
    public string? PatchHash { get; set; }

    [JsonPropertyName("offset")]
    public long Offset { get; set; }

    [JsonPropertyName("length")]
    public long Length { get; set; }
}