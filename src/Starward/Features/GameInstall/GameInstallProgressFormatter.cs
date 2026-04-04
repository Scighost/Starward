using Starward.RPC.GameInstall;
using System;

namespace Starward.Features.GameInstall;

internal static class GameInstallProgressFormatter
{


    public static string GetInstallStateText(GameInstallState state)
    {
        return state switch
        {
            GameInstallState.Waiting => Lang.StartGameButton_Waiting,
            GameInstallState.Downloading => Lang.DownloadGamePage_Downloading,
            GameInstallState.Decompressing => Lang.DownloadGamePage_Decompressing,
            GameInstallState.Merging => Lang.DownloadGamePage_Merging,
            GameInstallState.Verifying => Lang.DownloadGamePage_Verifying,
            GameInstallState.Paused => Lang.DownloadGamePage_Paused,
            GameInstallState.Finish => Lang.DownloadGamePage_Finished,
            GameInstallState.Error => Lang.DownloadGamePage_SomethingError,
            GameInstallState.Queueing => Lang.StartGameButton_InQueue,
            _ => "State Error",
        };
    }


    public static double GetProgressPercent(GameInstallContext task)
    {
        if (task.State is GameInstallState.Downloading)
        {
            double progress = task.Progress_DownloadTotalBytes > 0
                ? (double)task.Progress_DownloadFinishBytes / task.Progress_DownloadTotalBytes
                : 0d;

            if (task.Operation is GameInstallOperation.Update && task.DownloadMode is GameInstallDownloadMode.Chunk)
            {
                progress = task.Progress_WriteTotalBytes > 0
                    ? (double)task.Progress_WriteFinishBytes / task.Progress_WriteTotalBytes
                    : progress;
            }

            return progress;
        }

        if (task.State is GameInstallState.Decompressing or GameInstallState.Merging)
        {
            return task.Progress_Percent;
        }

        if (task.State is GameInstallState.Finish)
        {
            return 1d;
        }

        return task.Progress_Percent > 0 ? task.Progress_Percent : 0d;
    }


    public static string? ToBytesText(long finish, long total)
    {
        const double MB = 1 << 20;
        const double GB = 1 << 30;
        if (total == 0)
        {
            return null;
        }

        if (total >= GB)
        {
            return $"{finish / GB:F2}/{total / GB:F2} GB";
        }

        return $"{finish / MB:F2}/{total / MB:F2} MB";
    }


    public static string ToSpeedText(long bytes)
    {
        const double KB = 1 << 10;
        const double MB = 1 << 20;
        if (bytes >= MB)
        {
            return $"{bytes / MB:F2} MB/s";
        }

        return $"{bytes / KB:F2} KB/s";
    }


    public static string ToRemainTimeText(long seconds)
    {
        if (seconds <= 0)
        {
            return "--:--:--";
        }

        var timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }
}

