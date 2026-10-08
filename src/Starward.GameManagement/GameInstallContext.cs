using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;
using Starward.GameManagement.Step;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Starward.GameManagement;


/// <summary>
/// 游戏安装上下文
/// </summary>
public class GameInstallContext
{

    public LauncherConfig LauncherConfig { get; init; }

    public GameId GameId { get; init; }

    public string InstallPath { get; init; }

    public GameInstallOperation Operation { get; init; }

    public AudioLanguage AudioLanguage { get; init; }


    public long Timestamp { get; set; }

    public int CurrentStep { get; set; }

    public int TotalStep { get; set; }

    public GameInstallStep Step { get; set; }

    public GameInstallState State { get; set; }

    public ProgressDisplay ProgressDisplay { get; set; }

    public string? ErrorMessage { get; set; }


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
    /// 需要下载的总字节数
    /// </summary>
    public long DownloadTotalBytes { get; set; }
    /// <summary>
    /// 已下载的字节数
    /// </summary>
    public long DownloadFinishedBytes { get => _downloadFinishedBytes; set => _downloadFinishedBytes = value; }
    private long _downloadFinishedBytes;

    /// <summary>
    /// 需要写入的总字节数
    /// </summary>
    public long WriteTotalBytes { get; set; }
    /// <summary>
    /// 已写入的字节数
    /// </summary>
    public long WriteFinishedBytes { get => _writeFinishedBytes; set => _writeFinishedBytes = value; }
    private long _writeFinishedBytes;

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


    public long _networkBytes = 0;

    private long _lastNetworkBytes = 0;

    private long _lastTimestamp = 0;



    internal void ClearProgress()
    {
        PercentProgress = 0;
        DownloadTotalBytes = 0;
        DownloadFinishedBytes = 0;
        WriteTotalBytes = 0;
        WriteFinishedBytes = 0;
        HardLinkBytes = 0;
        NetworkSpeed = 0;
        RemainTimeSeconds = 0;
        _networkBytes = 0;
        _lastNetworkBytes = 0;
        _lastTimestamp = 0;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddNetworkBytes(long value)
    {
        Interlocked.Add(ref _downloadFinishedBytes, value);
        Interlocked.Add(ref _networkBytes, value);
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal void AddStorageBytes(long value)
    {
        Interlocked.Add(ref _writeFinishedBytes, value);
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

        if (DownloadFinishedBytes > DownloadTotalBytes)
        {
            DownloadTotalBytes = DownloadFinishedBytes;
        }
        if (WriteFinishedBytes > WriteTotalBytes)
        {
            WriteTotalBytes = WriteFinishedBytes;
        }

        if (ts - _lastTimestamp < Stopwatch.Frequency)
        {
            // 每秒更新一次
            return;
        }

        long currentNetwork = _networkBytes;
        long time = ts - _lastTimestamp;

        if (ProgressDisplay is ProgressDisplay.Progress)
        {
            return;
        }
        else
        {
            if (ProgressDisplay.HasFlag(ProgressDisplay.NetworkSpeed))
            {
                NetworkSpeed = (currentNetwork - _lastNetworkBytes) * Stopwatch.Frequency / time;
                RemainTimeSeconds = NetworkSpeed > 0 ? (DownloadTotalBytes - DownloadFinishedBytes) / NetworkSpeed : 0;
                if (DownloadTotalBytes > 0)
                {
                    PercentProgress = (double)DownloadFinishedBytes / DownloadTotalBytes;
                }
            }
            if (ProgressDisplay.HasFlag(ProgressDisplay.StorageBytes) && WriteTotalBytes > 0)
            {
                PercentProgress = (double)WriteFinishedBytes / WriteTotalBytes;
            }
        }

        _lastTimestamp = ts;
        _lastNetworkBytes = currentNetwork;
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


    internal GameInstallState CancelState { get; set; }


    internal void Cancel(GameInstallState state)
    {
        CancelState = state;
        _cancellationTokenSource?.Cancel();
    }


    #endregion


}
