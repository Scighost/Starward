using System.Text.Json.Serialization;

namespace Starward.GameManagement.Model;


/// <summary>
/// <b>现已基本不可用</b><br/>
/// 用于 <see cref="Core.HoYoPlay.DownloadMode.DOWNLOAD_MODE_FILE"/> 模式下载资源的校验，
/// 文件路径是根目录下的 <c>pkg_version</c>，文件格式是 jsonl<br/>
/// 可将 <see cref="Core.HoYoPlay.GamePackageResource.ResListUrl"/> 和 <see cref="RemoteName"/> 拼接获得下载链接
/// </summary>
internal class PkgVersionItem
{
    [JsonPropertyName("remoteName")]
    public string RemoteName { get; set; }

    [JsonPropertyName("md5")]
    public string MD5 { get; set; }

    [JsonPropertyName("fileSize")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public long FileSize { get; set; }

    [JsonIgnore]
    public bool IsFinished { get; set; }
}
