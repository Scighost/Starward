using Starward.Core;
using Starward.Core.HoYoPlay;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace Starward.RPC.GameInstall;

/// <summary>
/// 游戏安装任务
/// </summary>
public class GameInstallTask
{

    public GameId GameId { get; init; }

    /// <summary>
    /// 安装路径
    /// </summary>
    public string InstallPath { get; init; }

    public GameInstallOperation Operation { get; set; }

    public AudioLanguage AudioLanguage { get; init; }

    /// <summary>
    /// 硬链接游戏的路径
    /// </summary>
    public string? HardLinkPath { get; init; }


    public long Timestamp { get; set; }

    public GameInstallState State { get; set; }

    public string? ErrorMessage { get; set; }

    public GameInstallDownloadMode DownloadMode { get; set; }

    /// <summary>
    /// 需要下载的总字节数
    /// </summary>
    public long Progress_DownloadTotalBytes { get; set; }
    /// <summary>
    /// 已下载的字节数
    /// </summary>
    public long Progress_DownloadFinishBytes { get => _progress_DownloadFinishBytes; set => _progress_DownloadFinishBytes = value; }
    internal long _progress_DownloadFinishBytes;

    /// <summary>
    /// 需要读取的总字节数，没用到
    /// </summary>
    public long Progress_ReadTotalBytes { get; set; }
    /// <summary>
    /// 已读取的字节数，没用到
    /// </summary>
    public long Progress_ReadFinishBytes { get => _progress_ReadFinishBytes; set => _progress_ReadFinishBytes = value; }
    internal long _progress_ReadFinishBytes;

    /// <summary>
    /// 需要写入的总字节数
    /// </summary>
    public long Progress_WriteTotalBytes { get; set; }
    /// <summary>
    /// 已写入的字节数
    /// </summary>
    public long Progress_WriteFinishBytes { get => _progress_WriteFinishBytes; set => _progress_WriteFinishBytes = value; }
    internal long _progress_WriteFinishBytes;

    /// <summary>
    /// 解压和合并时的百分比进度，最大值是 1
    /// </summary>
    public double Progress_Percent { get; set; }


    /// <summary>
    /// 网络下载速度，单位是字节每秒
    /// </summary>
    public long NetworkDownloadSpeed { get; set; }

    /// <summary>
    /// 存储读取速度，单位是字节每秒
    /// </summary>
    public long StorageReadSpeed { get; set; }

    /// <summary>
    /// 存储写入速度，单位是字节每秒
    /// </summary>
    public long StorageWriteSpeed { get; set; }

    /// <summary>
    /// 预计剩余时间，仅用于预计下载剩余时间，单位是秒
    /// </summary>
    public long RemainTimeSeconds { get; set; }



    internal string? LocalGameVersion { get; set; }

    internal string? LatestGameVersion { get; set; }

    internal string? PredownloadVersion { get; set; }



    internal GameConfig? GameConfig { get; set; }

    internal GamePackage? GamePackage { get; set; }

    internal GameSophonChunkBuild? GameSophonChunkBuild { get; set; }

    internal GameSophonChunkBuild? LocalVersionSophonChunkBuild { get; set; }

    internal GameSophonPatchBuild? GameSophonPatchBuild { get; set; }

    internal GameChannelSDK? GameChannelSDK { get; set; }

    internal GameDeprecatedFileConfig? DeprecatedFileConfig { get; set; }


    internal List<SophonChunkFile>? SophonChunkFiles { get; set; }

    internal List<SophonChunkFile>? LocalVersionSophonChunkFiles { get; set; }

    internal List<SophonPatchFile>? SophonPatchFiles { get; set; }

    internal List<SophonPatchDeleteFile>? SophonPatchDeleteFiles { get; set; }

    internal List<GameInstallFile>? TaskFiles { get; set; }



    public long networkDownloadBytes = 0;

    public long storageReadBytes = 0;

    public long storageWriteBytes = 0;

    private long lastNetworkBytes = 0;

    private long lastStorageReadBytes = 0;

    private long lastStorageWriteBytes = 0;

    private long lastTimestamp = 0;


    private CancellationTokenSource? _cancellationTokenSource;

    internal CancellationToken CancellationToken => GetCancellation();


    private CancellationToken GetCancellation()
    {
        if (_cancellationTokenSource is null or { IsCancellationRequested: true })
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        return _cancellationTokenSource.Token;
    }


    internal GameInstallState CancelState { get; private set; } = GameInstallState.Queueing;


    internal void Cancel(GameInstallState state)
    {
        CancelState = state;
        _cancellationTokenSource?.Cancel();
    }



    internal void RefreshSpeed()
    {
        long ts = Stopwatch.GetTimestamp();
        Timestamp = ts;
        if (ts - lastTimestamp < Stopwatch.Frequency)
        {
            // 每秒更新一次
            return;
        }

        long currentDownload = networkDownloadBytes;
        long currentRead = storageReadBytes;
        long currentWrite = storageWriteBytes;
        long time = ts - lastTimestamp;

        NetworkDownloadSpeed = (currentDownload - lastNetworkBytes) * Stopwatch.Frequency / time;
        StorageReadSpeed = (currentRead - lastStorageReadBytes) * Stopwatch.Frequency / time;
        StorageWriteSpeed = (currentWrite - lastStorageWriteBytes) * Stopwatch.Frequency / time;
        RemainTimeSeconds = NetworkDownloadSpeed > 0 ? (Progress_DownloadTotalBytes - Progress_DownloadFinishBytes) / NetworkDownloadSpeed : 0;

        lastTimestamp = ts;
        lastNetworkBytes = currentDownload;
        lastStorageReadBytes = currentRead;
        lastStorageWriteBytes = currentWrite;
    }


}



public partial class GameInstallTaskDTO
{

    public GameId GetGameId() => new GameId { GameBiz = GameBiz, Id = GameId };


    public GameInstallTask UpdateTask(GameInstallTask? task = null)
    {
        task ??= new GameInstallTask
        {
            AudioLanguage = (AudioLanguage)AudioLanguage,
            GameId = GetGameId(),
            HardLinkPath = HardLinkPath,
            InstallPath = InstallPath,
        };
        task.Operation = (GameInstallOperation)Operation;
        task.Timestamp = Timestamp;
        task.State = (GameInstallState)State;
        task.Progress_DownloadTotalBytes = ProgressDownloadTotalBytes;
        task.Progress_DownloadFinishBytes = ProgressDownloadFinishBytes;
        task.Progress_ReadTotalBytes = ProgressReadTotalBytes;
        task.Progress_ReadFinishBytes = ProgressReadFinishBytes;
        task.Progress_WriteTotalBytes = ProgressWriteTotalBytes;
        task.Progress_WriteFinishBytes = ProgressWriteFinishBytes;
        task.Progress_Percent = ProgressPercent;
        task.ErrorMessage = ErrorMessage;
        task.NetworkDownloadSpeed = NetworkDownloadSpeed;
        task.StorageReadSpeed = StorageReadSpeed;
        task.StorageWriteSpeed = StorageWriteSpeed;
        task.RemainTimeSeconds = RemainTimeSeconds;
        task.DownloadMode = (GameInstallDownloadMode)DownloadMode;
        return task;
    }



    public static GameInstallTaskDTO FromTask(GameInstallTask task) => new GameInstallTaskDTO
    {
        AudioLanguage = (int)task.AudioLanguage,
        GameBiz = task.GameId.GameBiz,
        GameId = task.GameId.Id,
        HardLinkPath = task.HardLinkPath,
        InstallPath = task.InstallPath,
        Operation = (int)task.Operation,
        Timestamp = task.Timestamp,
        State = (int)task.State,
        ProgressDownloadTotalBytes = task.Progress_DownloadTotalBytes,
        ProgressDownloadFinishBytes = task.Progress_DownloadFinishBytes,
        ProgressReadTotalBytes = task.Progress_ReadTotalBytes,
        ProgressReadFinishBytes = task.Progress_ReadFinishBytes,
        ProgressWriteTotalBytes = task.Progress_WriteTotalBytes,
        ProgressWriteFinishBytes = task.Progress_WriteFinishBytes,
        ProgressPercent = task.Progress_Percent,
        ErrorMessage = task.ErrorMessage,
        NetworkDownloadSpeed = task.NetworkDownloadSpeed,
        StorageReadSpeed = task.StorageReadSpeed,
        StorageWriteSpeed = task.StorageWriteSpeed,
        RemainTimeSeconds = task.RemainTimeSeconds,
        DownloadMode = (int)task.DownloadMode,
    };

}


public partial class GameInstallTaskRequest
{

    public GameId GetGameId() => new GameId { GameBiz = GameBiz, Id = GameId };


    public GameInstallTask ToTask() => new GameInstallTask
    {
        AudioLanguage = (AudioLanguage)AudioLanguage,
        GameId = GetGameId(),
        HardLinkPath = HardLinkPath,
        InstallPath = InstallPath,
        Operation = (GameInstallOperation)Operation
    };


    public static GameInstallTaskRequest FromTask(GameInstallTask task)
    {
        return new GameInstallTaskRequest
        {
            GameBiz = task.GameId.GameBiz,
            GameId = task.GameId.Id,
            InstallPath = task.InstallPath,
            Operation = (int)task.Operation,
            AudioLanguage = (int)task.AudioLanguage,
            HardLinkPath = task.HardLinkPath,
        };
    }


}