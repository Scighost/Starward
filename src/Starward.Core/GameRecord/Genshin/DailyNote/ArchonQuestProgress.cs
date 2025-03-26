using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.DailyNote;

/// <summary>
/// 魔神任务进度
/// </summary>
public class ArchonQuestProgress
{

    [JsonPropertyName("list")]
    public List<ArchonQuest> List { get; set; }

    /// <summary>
    /// 魔神任务是否开启
    /// </summary>
    [JsonPropertyName("is_open_archon_quest")]
    public bool IsOpenArchonQuest { get; set; }

    /// <summary>
    /// 是否完成所有主线
    /// </summary>
    [JsonPropertyName("is_finish_all_mainline")]
    public bool IsFinishAllMainline { get; set; }

    /// <summary>
    /// 是否完成所有间章
    /// </summary>
    [JsonPropertyName("is_finish_all_interchapter")]
    public bool IsFinishAllInterchapter { get; set; }

    [JsonPropertyName("wiki_url")]
    public string WikiUrl { get; set; } = default!;
}



/// <summary>
/// 魔神任务状态
/// </summary>
public class ArchonQuest
{

    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// 第X章 第Y幕
    /// </summary>
    [JsonPropertyName("chapter_num")]
    public string ChapterNum { get; set; }

    /// <summary>
    /// 章节标题
    /// </summary>
    [JsonPropertyName("chapter_title")]
    public string ChapterTitle { get; set; }


    [JsonPropertyName("id")]
    public int Id { get; set; }


    /// <summary>
    /// 已完成
    /// </summary>
    public const string StatusFinished = nameof(StatusFinished);

    /// <summary>
    /// 进行中
    /// </summary>
    public const string StatusOngoing = nameof(StatusOngoing);

    /// <summary>
    /// 未开启
    /// </summary>
    public const string StatusNotOpen = nameof(StatusNotOpen);

}