using Starward.Core.JsonConverter;
using System.Text.Json.Serialization;

namespace Starward.Core.GameRecord.Genshin.DailyNote;

/// <summary>
/// 每日任务
/// </summary>
public class DailyTask
{

    /// <summary>
    /// 任务总数
    /// </summary>
    [JsonPropertyName("total_num")]
    public int TotalNum { get; set; }

    /// <summary>
    /// 已完成任务数
    /// </summary>
    [JsonPropertyName("finished_num")]
    public int FinishedNum { get; set; }

    /// <summary>
    /// 额外奖励已领取
    /// </summary>
    [JsonPropertyName("is_extra_task_reward_received")]
    public bool IsExtraTaskRewardReceived { get; set; }

    /// <summary>
    /// 委托任务
    /// </summary>
    [JsonPropertyName("task_rewards")]
    public List<TaskRewardStatus> TaskRewards { get; set; }

    /// <summary>
    /// 历练点
    /// </summary>
    [JsonPropertyName("attendance_rewards")]
    public List<AttendanceRewardStatus> AttendanceRewards { get; set; }

    /// <summary>
    /// 历练点可见
    /// </summary>
    [JsonPropertyName("attendance_visible")]
    public bool AttendanceVisible { get; set; }

    /// <summary>
    /// 长效历练点
    /// </summary>
    [JsonPropertyName("stored_attendance")]
    [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
    public double StoredAttendance { get; set; }

    /// <summary>
    /// 长效历练点重置时间，单位：秒
    /// </summary>
    [JsonPropertyName("stored_attendance_refresh_countdown")]
    [JsonConverter(typeof(TimeSpanSecondNumberJsonConverter))]
    public TimeSpan StoredAttendanceRefreshCountdown { get; set; }

}



/// <summary>
/// 每日任务奖励状态
/// </summary>
public sealed class TaskRewardStatus
{

    [JsonPropertyName("status")]
    public string Status { get; set; }


    /// <summary>
    /// 任务已完成
    /// </summary>
    [JsonIgnore]
    public bool IsTakenAward => Status is TaskRewardStatusTakenAward;

    /// <summary>
    /// 任务已完成，但没有奖励
    /// </summary>
    [JsonIgnore]
    public bool IsFinished => Status is TaskRewardStatusFinished;

    /// <summary>
    /// 任务未完成
    /// </summary>
    [JsonIgnore]
    public bool IsUnfinished => Status is TaskRewardStatusInvalid or TaskRewardStatusUnfinished;


    /// <summary>
    /// 不可用
    /// </summary>
    public const string TaskRewardStatusInvalid = nameof(TaskRewardStatusInvalid);

    /// <summary>
    /// 奖励已领取
    /// </summary>
    public const string TaskRewardStatusTakenAward = nameof(TaskRewardStatusTakenAward);

    /// <summary>
    /// 任务已完成，但没有奖励
    /// </summary>
    public const string TaskRewardStatusFinished = nameof(TaskRewardStatusFinished);

    /// <summary>
    /// 任务未完成
    /// </summary>
    public const string TaskRewardStatusUnfinished = nameof(TaskRewardStatusUnfinished);

}



/// <summary>
/// 历练点奖励状态
/// </summary>
public class AttendanceRewardStatus
{

    [JsonPropertyName("status")]
    public string Status { get; set; }

    /// <summary>
    /// 历练点进度，满值2000
    /// </summary>
    [JsonPropertyName("progress")]
    public int Progress { get; set; }


    /// <summary>
    /// 已领取奖励
    /// </summary>
    [JsonIgnore]
    public bool IsTakenAward => Status is AttendanceRewardStatusTakenAward;


    /// <summary>
    /// 等待领取奖励
    /// </summary>
    [JsonIgnore]
    public bool IsWaitTaken => Status is AttendanceRewardStatusWaitTaken;


    /// <summary>
    /// 未完成
    /// </summary>
    [JsonIgnore]
    public bool IsUnfinished => Status is AttendanceRewardStatusUnfinished;


    /// <summary>
    /// 未完成或等待领取
    /// </summary>
    [JsonIgnore]
    public bool IsUnfinishedOrWaitTaken => IsUnfinished || IsWaitTaken;


    /// <summary>
    /// 不可用
    /// </summary>
    [JsonIgnore]
    public bool IsForbid => Status is AttendanceRewardStatusInvalid or AttendanceRewardStatusForbid;


    /// <summary>
    /// 不可用
    /// </summary>
    public const string AttendanceRewardStatusInvalid = nameof(AttendanceRewardStatusInvalid);

    /// <summary>
    /// 奖励已领取
    /// </summary>
    public const string AttendanceRewardStatusTakenAward = nameof(AttendanceRewardStatusTakenAward);

    /// <summary>
    /// 等待领取
    /// </summary>
    public const string AttendanceRewardStatusWaitTaken = nameof(AttendanceRewardStatusWaitTaken);

    /// <summary>
    /// 未完成
    /// </summary>
    public const string AttendanceRewardStatusUnfinished = nameof(AttendanceRewardStatusUnfinished);

    /// <summary>
    /// 已完成，无奖励（进度达到2000，但完成了每日任务）
    /// </summary>
    public const string AttendanceRewardStatusFinishedNonReward = nameof(AttendanceRewardStatusFinishedNonReward);

    /// <summary>
    /// 无法使用（进度未达到2000，已完成了每日任务）
    /// </summary>
    public const string AttendanceRewardStatusForbid = nameof(AttendanceRewardStatusForbid);

}