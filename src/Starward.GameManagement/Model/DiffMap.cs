using System.Text.Json.Serialization;

namespace Starward.GameManagement.Model;


/// <summary>
/// 用于 <see cref="Core.HoYoPlay.DownloadMode.DOWNLOAD_MODE_FILE"/> 模式下版本更新后的补丁文件合并。
/// 差分包解压后根目录下可能存在 <c>hdiffmap.json</c> 文件。<br/>
/// </summary>
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
