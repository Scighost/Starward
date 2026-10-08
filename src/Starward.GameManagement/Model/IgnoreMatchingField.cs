using System.Text.Json.Serialization;

namespace Starward.GameManagement.Model;


/// <summary>
/// 用于已完成任务的资源管理，避免游戏内删除后被启动器重新下载，<see cref="Core.HoYoPlay.DownloadMode.DOWNLOAD_MODE_CHUNK"/> 下载模式使用。<br/>
/// 文件路径参考 <see cref="Core.HoYoPlay.GameConfig.ResCategoryDir"/>，文件格式 jsonl
/// <see cref="Category"/> 对应 <see cref="Core.HoYoPlay.GameBranchPackageCategory.MatchingField"/>
/// </summary>
internal class IgnoreMatchingField
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("is_delete")]
    public bool IsDelete { get; set; }
}
