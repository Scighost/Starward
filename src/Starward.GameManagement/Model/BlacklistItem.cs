using System.Text.Json.Serialization;

namespace Starward.GameManagement.Model;


/// <summary>
/// 用于已完成任务的资源管理，避免游戏内删除后被启动器重新下载，<see cref="Core.HoYoPlay.DownloadMode.DOWNLOAD_MODE_CHUNK"/> 下载模式使用。<br/>
/// 文件路径参考 <see cref="Core.HoYoPlay.GameConfig.BlacklistDir"/>，文件格式 jsonl
/// </summary>
internal class BlacklistItem
{
    [JsonPropertyName("fileName")]
    public string FileName { get; set; }
}
