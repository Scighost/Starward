using System.Text.Json.Serialization;

namespace Starward.GameManagement.Model;


/// <summary>
/// 用于 <see cref="Core.HoYoPlay.DownloadMode.DOWNLOAD_MODE_FILE"/> 模式下版本更新后的补丁文件合并。
/// 差分包解压后根目录下可能存在 <c>hdifffiles.txt</c> 文件，文件内容为 jsonl 格式。<br/>
/// 使用 HDiffPatch 合并补丁，源文件是 <see cref="RemoteName"/>，差分文件是 <c>"{<see cref="RemoteName"/>}.hdiff"</c>，目标文件是 <see cref="RemoteName"/>
/// </summary>
internal class HDiffFile
{
    [JsonPropertyName("remoteName")]
    public string RemoteName { get; set; }
}
