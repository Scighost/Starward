using Microsoft.Extensions.DependencyInjection;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Model;
using Starward.GameInstall.Step;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Starward.GameInstall;


/// <summary>
/// 游戏安装上下文
/// </summary>
internal class GameInstallContext
{

    public LauncherConfig LauncherConfig { get; init; }

    public GameId GameId { get; init; }

    public string InstallPath { get; init; }

    public GameInstallOperation Operation { get; init; }


    public long Timestamp { get; set; }

    public int CurrentStep { get; set; }

    public int TotalStep { get; set; }

    public RunnerStep Step { get; set; }

    public RunnerState State { get; set; }

    public ProgressDisplay ProgressDisplay { get; set; }

    public string? ErrorMessage { get; set; }


    internal List<string> CutsceneBlockList { get; set; }

    internal IServiceScope ServiceScope { get; set; }

    internal AudioLanguage AudioLanguage { get; set; }

    internal string CategoryScenario { get; set; }

    internal GameConfig GameConfig { get; set; }

    internal GameConfigIni ConfigIni { get; set; }

    internal List<StepBase> StepList { get; set; }



    #region Progress


    /// <summary>
    /// 当前阶段进度百分比，最大值1
    /// </summary>
    public double PercentProgress { get; set; }


    /// <summary>
    /// 需要从网络下载的总字节数
    /// </summary>
    public long NetworkTotalBytes { get; set; }
    /// <summary>
    /// 已从网络下载的字节数
    /// </summary>
    public long NetworkFinishedBytes { get => _networkFinishedBytes; set => _networkFinishedBytes = value; }
    private long _networkFinishedBytes;

    /// <summary>
    /// 需要写入存储的总字节数
    /// </summary>
    public long StorageTotalBytes { get; set; }
    /// <summary>
    /// 已写入存储的字节数
    /// </summary>
    public long StorageFinishedBytes { get => _storageFinishedBytes; set => _storageFinishedBytes = value; }
    private long _storageFinishedBytes;

    /// <summary>
    /// 硬链接字节数
    /// </summary>
    public long HardLinkBytes { get => _hardLinkBytes; set => _hardLinkBytes = value; }
    private long _hardLinkBytes;


    /// <summary>
    /// 网络下载速度，单位是字节每秒
    /// </summary>
    public long NetworkSpeed { get; set; }

    /// <summary>
    /// 预计剩余时间，仅用于预计下载剩余时间，单位是秒
    /// </summary>
    public long RemainTimeSeconds { get; set; }


    public long _downloadBytes = 0;

    private long _lastDownloadBytes = 0;

    private long _lastTimestamp = 0;

    private long _lastNetworkSpeed = 0;


    internal void ClearProgress()
    {
        PercentProgress = 0;
        NetworkTotalBytes = 0;
        NetworkFinishedBytes = 0;
        StorageTotalBytes = 0;
        StorageFinishedBytes = 0;
        HardLinkBytes = 0;
        NetworkSpeed = 0;
        RemainTimeSeconds = 0;
        _downloadBytes = 0;
        _lastDownloadBytes = 0;
        _lastTimestamp = 0;
        _lastNetworkSpeed = 0;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddDownloadBytes(long value)
    {
        Interlocked.Add(ref _downloadBytes, value);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddNetworkBytes(long value)
    {
        Interlocked.Add(ref _networkFinishedBytes, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddStorageBytes(long value)
    {
        Interlocked.Add(ref _storageFinishedBytes, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddHardLinkBytes(long value)
    {
        Interlocked.Add(ref _hardLinkBytes, value);
    }


    internal void RefreshSpeed()
    {
        long ts = Stopwatch.GetTimestamp();
        Timestamp = ts;

        if (NetworkFinishedBytes > NetworkTotalBytes)
        {
            NetworkTotalBytes = NetworkFinishedBytes;
        }
        if (StorageFinishedBytes > StorageTotalBytes)
        {
            StorageTotalBytes = StorageFinishedBytes;
        }
        if (ProgressDisplay.HasFlag(ProgressDisplay.StorageBytes) && StorageTotalBytes > 0)
        {
            PercentProgress = Math.Clamp((double)StorageFinishedBytes / StorageTotalBytes, 0, 1);
        }
        else if (ProgressDisplay.HasFlag(ProgressDisplay.NetworkBytes) && NetworkTotalBytes > 0)
        {
            PercentProgress = Math.Clamp((double)NetworkFinishedBytes / NetworkTotalBytes, 0, 1);
        }

        if (ts - _lastTimestamp < Stopwatch.Frequency)
        {
            // 每秒更新一次
            return;
        }

        long currentDownload = _downloadBytes;
        long time = ts - _lastTimestamp;

        if (ProgressDisplay.HasFlag(ProgressDisplay.NetworkSpeed))
        {
            long speed = (currentDownload - _lastDownloadBytes) * Stopwatch.Frequency / time;
            NetworkSpeed = (speed + _lastNetworkSpeed) / 2;
            _lastNetworkSpeed = speed;
            RemainTimeSeconds = NetworkSpeed > 0 ? (NetworkTotalBytes - NetworkFinishedBytes) / NetworkSpeed : 0;
        }

        _lastTimestamp = ts;
        _lastDownloadBytes = currentDownload;
    }


    #endregion




    #region Cancel


    private CancellationTokenSource? _cancellationTokenSource;

    internal CancellationToken CancellationToken => GetCancellation();

    internal bool IsCancellationRequested => _cancellationTokenSource?.IsCancellationRequested ?? false;


    private CancellationToken GetCancellation()
    {
        if (_cancellationTokenSource is null or { IsCancellationRequested: true })
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }
        return _cancellationTokenSource.Token;
    }


    public SemaphoreSlim ExecuteLock { get; set; } = new(1, 1);


    internal RunnerState CancelState { get; set; }


    internal void Cancel(RunnerState state)
    {
        CancelState = state;
        _cancellationTokenSource?.Cancel();
    }


    #endregion


}
