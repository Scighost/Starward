using Starward.GameInstall;
using System;

namespace Starward.Features.GameInstall;

/// <summary>
/// 将安装进度数据转换为按钮和弹层使用的展示文本。
/// </summary>
internal static class GameInstallProgressDisplay
{

    public static string GetOperationText(GameInstallOperation operation) => operation switch
    {
        GameInstallOperation.Install => Lang.LauncherPage_InstallGame,
        GameInstallOperation.Update => Lang.LauncherPage_UpdateGame,
        GameInstallOperation.Repair => Lang.LauncherPage_RepairGame,
        GameInstallOperation.Predownload => Lang.LauncherPage_PreInstall,
        _ => "",
    };


    public static string GetPhaseText(RunnerState state, RunnerStep step) => state switch
    {
        RunnerState.Queue => Lang.StartGameButton_InQueue,
        RunnerState.Paused => Lang.DownloadGamePage_Paused,
        RunnerState.Error => Lang.DownloadGamePage_SomethingError,
        RunnerState.Finish => Lang.DownloadGamePage_Finished,
        RunnerState.Running => step switch
        {
            RunnerStep.Pending => Lang.DownloadGamePage_Preparing,
            RunnerStep.Downloading => Lang.DownloadGamePage_Downloading,
            RunnerStep.Verifying => Lang.DownloadGamePage_Verifying,
            RunnerStep.Patching => Lang.DownloadGamePage_Merging,
            RunnerStep.Decompressing => Lang.DownloadGamePage_Decompressing,
            RunnerStep.Cleaning => Lang.StartGameButton_Cleaning,
            _ => Lang.DownloadGamePage_Preparing,
        },
        _ => "",
    };


    public static string GetOperationPhaseText(GameInstallProgress task)
    {
        string operation = GetOperationText(task.Operation);
        string phase = GetPhaseText(task.State, task.Step);
        if (string.IsNullOrEmpty(operation))
        {
            return phase;
        }
        return string.IsNullOrEmpty(phase) ? operation : $"{operation} · {phase}";
    }


    public static string GetActionText(RunnerState state) => state switch
    {
        RunnerState.Running => Lang.DownloadGamePage_Pause,
        RunnerState.Paused or RunnerState.Queue => Lang.Common_Continue,
        RunnerState.Error => Lang.DownloadGamePage_Retry,
        _ => "",
    };


    /// <summary>
    /// 获取进度圈百分比。返回 false 时使用不确定进度。
    /// 服务端已按“存储优先”规则计算 PercentProgress，这里只判断当前是否有确定进度。
    /// </summary>
    public static bool TryGetPercent(GameInstallProgress task, out double percent)
    {
        percent = 0;
        if (task.State is RunnerState.Finish)
        {
            percent = 100;
            return true;
        }
        if (task.State is RunnerState.Queue or RunnerState.Stop)
        {
            return false;
        }

        bool hasProgress = task.ProgressDisplay.HasFlag(ProgressDisplay.Progress)
            || (task.ProgressDisplay.HasFlag(ProgressDisplay.StorageBytes) && task.StorageTotalBytes > 0)
            || (task.ProgressDisplay.HasFlag(ProgressDisplay.NetworkBytes) && task.NetworkTotalBytes > 0);
        if (!hasProgress)
        {
            return false;
        }
        if (!double.IsFinite(task.PercentProgress))
        {
            return false;
        }

        percent = Math.Clamp(task.PercentProgress * 100, 0, 100);
        if (task.State is not RunnerState.Finish && percent is 100)
        {
            // 任务尚未完成时不要提前显示 100%
            percent = 99;
        }
        return true;
    }


    public static string? GetStepText(GameInstallProgress task)
    {
        if (task.TotalStep <= 0 || task.CurrentStep <= 0)
        {
            return null;
        }
        int current = Math.Clamp(task.CurrentStep, 1, task.TotalStep);
        return string.Format(Lang.StartGameButton_StepProgress, current, task.TotalStep);
    }


    public static string? ToPercentText(double percent)
    {
        return percent >= 100 ? "100%" : $"{percent:0.0}%";
    }


    public static string? ToBytesText(long finish, long total, int decimals = 2)
    {
        const double KB = 1 << 10;
        const double MB = 1 << 20;
        const double GB = 1 << 30;
        if (finish <= 0 && total <= 0)
        {
            return null;
        }
        if (total <= 0)
        {
            return FormatBytes(finish, decimals);
        }
        string format = $"F{Math.Clamp(decimals, 0, 3)}";
        if (total >= GB)
        {
            return $"{(finish / GB).ToString(format)}/{(total / GB).ToString(format)} GB";
        }
        if (total >= MB)
        {
            return $"{(finish / MB).ToString(format)}/{(total / MB).ToString(format)} MB";
        }
        return $"{(finish / KB).ToString(format)}/{(total / KB).ToString(format)} KB";
    }


    public static string FormatBytes(long bytes, int decimals = 2)
    {
        const double KB = 1 << 10;
        const double MB = 1 << 20;
        const double GB = 1 << 30;
        string format = $"F{Math.Clamp(decimals, 0, 3)}";
        if (bytes >= GB)
        {
            return $"{(bytes / GB).ToString(format)} GB";
        }
        if (bytes >= MB)
        {
            return $"{(bytes / MB).ToString(format)} MB";
        }
        if (bytes >= KB)
        {
            return $"{(bytes / KB).ToString(format)} KB";
        }
        return $"{bytes} B";
    }


    public static string? ToSpeedText(long bytes)
    {
        const double KB = 1 << 10;
        const double MB = 1 << 20;
        if (bytes <= 0)
        {
            return null;
        }
        if (bytes >= MB)
        {
            return $"{bytes / MB:F2} MB/s";
        }
        return $"{bytes / KB:F2} KB/s";
    }


    public static string? ToRemainTimeText(long seconds)
    {
        if (seconds <= 0)
        {
            return null;
        }
        return TimeSpan.FromSeconds(seconds).ToString(@"hh\:mm\:ss");
    }

}
