using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Starward.RPC.GameInstall;

internal class DiffMap
{

    [JsonPropertyName("diff_map")]
    public List<DiffMapItem> DiffMapItems { get; set; }

}


internal class DiffMapItem
{

    [JsonPropertyName("source_file_name")]
    public string SourceFileName { get; set; }

    [JsonPropertyName("source_file_md5")]
    public string SourceFileMd5 { get; set; }

    [JsonPropertyName("source_file_size")]
    public long SourceFileSize { get; set; }

    [JsonPropertyName("target_file_name")]
    public string TargetFileName { get; set; }

    [JsonPropertyName("target_file_md5")]
    public string TargetFileMd5 { get; set; }

    [JsonPropertyName("target_file_size")]
    public long TargetFileSize { get; set; }

    [JsonPropertyName("patch_file_name")]
    public string PatchFileName { get; set; }

    [JsonPropertyName("patch_file_md5")]
    public string PatchFileMd5 { get; set; }

    [JsonPropertyName("patch_file_size")]
    public long PatchFileSize { get; set; }

}



internal class HDiffFile
{
    [JsonPropertyName("remoteName")]
    public string RemoteName { get; set; }
}