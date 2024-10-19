using System.Collections.Concurrent;
using System.Diagnostics;
using ICSharpCode.SharpZipLib.Zip;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// <see cref="Starward.Core.ZipStreamDownload.FastZipStreamDownload"/>的进度报告帮助类
/// </summary>
public class FastZipStreamDownloadProgressUtils
{
    /// <summary>
    /// 实体进度状态
    /// </summary>
    public class EntryStatus
    {
        /// <summary>
        /// 当前处理阶段
        /// </summary>
        public FastZipStreamDownload.ProcessingStageEnum ProcessingStage { get; internal set; }

        /// <summary>
        /// 当前任务是否完成
        /// </summary>
        public bool VerifyCompleted { get; internal set; }

        /// <summary>
        /// 当前任务是否完成
        /// </summary>
        public bool DownloadCompleted { get; internal set; }

        /// <summary>
        /// 当前任务是否完成
        /// </summary>
        public bool ExtractCompleted { get; internal set; }

        /// <summary>
        /// 当前任务是否完成
        /// </summary>
        public bool CrcVerifyCompleted { get; internal set; }

        /// <summary>
        /// 当前实体已经验证的字节数
        /// </summary>
        public long? VerifyBytesCompleted { get; internal set; }

        /// <summary>
        /// 当前实体体需要验证的字节数
        /// </summary>
        public long? VerifyBytesTotal { get; internal set; }

        /// <summary>
        /// 当前实体已经下载的字节数
        /// </summary>
        public long? DownloadBytesCompleted { get; internal set; }

        /// <summary>
        /// 当前实体已经下载的字节数（验证成功跳过）
        /// </summary>
        public long? DownloadBytesCompletedIfVerified { get; internal set; }

        /// <summary>
        /// 当前实体需要已经下载的字节数
        /// </summary>
        public long? DownloadBytesTotal { get; internal set; }

        /// <summary>
        /// 当前实体已经进行解压的字节数
        /// </summary>
        public long? ExtractBytesCompleted { get; internal set; }

        /// <summary>
        /// 当前实体已经进行解压的字节数（验证成功跳过）
        /// </summary>
        public long? ExtractBytesCompletedIfVerified { get; internal set; }

        /// <summary>
        /// 当前实体需要解压的字节数
        /// </summary>
        public long? ExtractBytesTotal { get; internal set; }

        /// <summary>
        /// 当前实体已经进行CRC校验的字节数
        /// </summary>
        public long? CrcVerifyBytesCompleted { get; internal set; }

        /// <summary>
        /// 当前实体需要进行CRC校验的字节数
        /// </summary>
        public long? CrcVerifyBytesTotal { get; internal set; }

        /// <summary>
        /// 当前任务异常
        /// </summary>
        public Exception? Exception { get; internal set; }
    }

    /// <summary>
    /// 获取传递给<see cref="Starward.Core.ZipStreamDownload.FastZipStreamDownload"/>的Progress属性的实例。
    /// </summary>
    public IProgress<FastZipStreamDownload.ProgressChangedArgs> Progress { get; }

    /// <summary>
    /// 进度改变事件
    /// </summary>
    /// <remarks>基于性能考虑，该事件最短触发间隔为100ms。</remarks>
    public event EventHandler? ProgressUpdateEvent;

    /// <summary>
    /// 当前的整体状态
    /// </summary>
    public FastZipStreamDownload.ProcessingStageEnum CurrentProcessingStage { get; private set; } =
        FastZipStreamDownload.ProcessingStageEnum.None;

    /// <summary>
    /// 各实体的当前状态
    /// </summary>
    public IReadOnlyDictionary<ZipEntry, EntryStatus> EntriesStatus =>
        _entriesStatus.AsReadOnly();

    /// <summary>
    /// 需要下载的文件的实体集合
    /// </summary>
    public IReadOnlyCollection<ZipEntry>? FileEntries => _fileEntries;

    /// <summary>
    /// 需要下载的文件夹实体集合
    /// </summary>
    public IReadOnlyCollection<ZipEntry>? DirectoryEntries => _directoryEntries;

    /// <summary>
    /// 需下载的字节总数
    /// </summary>
    public long? DownloadBytesTotal { get; private set; }

    /// <summary>
    /// 需解压的字节总数
    /// </summary>
    public long? ExtractBytesTotal { get; private set; }

    /// <summary>
    /// 下载完成的字节数（包含验证成功跳过的字节数）
    /// </summary>
    public long DownloadBytesCompleted { get; private set; }

    /// <summary>
    /// 解压完成的字节数（包含验证成功跳过的字节数）
    /// </summary>
    public long ExtractBytesCompleted { get; private set; }

    /// <summary>
    /// 下载完成百分比
    /// </summary>
    public double DownloadCompletionPercentage { get; private set; }

    /// <summary>
    /// 解压完成百分比
    /// </summary>
    public double ExtractCompletionPercentage { get; private set; }

    /// <summary>
    /// 每秒下载字节数
    /// </summary>
    public double DownloadBytesPerSecond { get; private set; }

    /// <summary>
    /// 每秒解压字节数
    /// </summary>
    public double ExtractBytesPerSecond { get; private set; }

    /// <summary>
    /// 下载预估剩余时间
    /// </summary>
    public TimeSpan DownloadRemainingTime { get; private set; }

    /// <summary>
    /// 解压预估剩余时间
    /// </summary>
    public TimeSpan ExtractRemainingTime { get; private set; }

    /// <summary>
    /// 总体任务异常
    /// </summary>
    public Exception? Exception { get; private set; }

    /// <summary>
    /// 各实体的当前状态
    /// </summary>
    private readonly ConcurrentDictionary<ZipEntry, EntryStatus> _entriesStatus;

    /// <summary>
    /// 需要下载的文件的实体集合
    /// </summary>
    private IReadOnlyCollection<ZipEntry>? _fileEntries;

    /// <summary>
    /// 需要下载的文件夹实体集合
    /// </summary>
    private IReadOnlyCollection<ZipEntry>? _directoryEntries;

    /// <summary>
    /// 进度刷新任务
    /// </summary>
    private readonly Task _progressRefreshTask;

    /// <summary>
    /// 速度刷新任务
    /// </summary>
    private readonly Task _speedRefreshTask;

    /// <summary>
    /// 进度刷新取消令牌源
    /// </summary>
    private readonly CancellationTokenSource _progressRefreshCancellationTokenSource;


    /// <summary>
    /// 是否需要刷新进度（0：不需要，1：需要）
    /// </summary>
    private int _needRefreshProgress;
    /// <summary>
    /// 是否需要刷新速度（0：不需要，1：需要）
    /// </summary>
    private int _needRefreshSpeed;

    /// <summary>
    /// 初始化一个<see cref="Starward.Core.ZipStreamDownload.FastZipStreamDownload"/>的进度报告帮助类的实例
    /// </summary>
    public FastZipStreamDownloadProgressUtils()
    {
        _entriesStatus = new ConcurrentDictionary<ZipEntry, EntryStatus>();

        _progressRefreshCancellationTokenSource = new CancellationTokenSource();
        var progressRefreshCancellationToken = _progressRefreshCancellationTokenSource.Token;
        _progressRefreshTask = Task.Run(() =>
            ProgressRefresh(progressRefreshCancellationToken), progressRefreshCancellationToken);
        _speedRefreshTask = Task.Run(() =>
            SpeedRefresh(progressRefreshCancellationToken), progressRefreshCancellationToken);

        Progress = new Progress<FastZipStreamDownload.ProgressChangedArgs>(ProgressUpdate);
    }

    ~FastZipStreamDownloadProgressUtils()
    {
        _progressRefreshCancellationTokenSource.Cancel();
        _progressRefreshTask.Wait();
        _speedRefreshTask.Wait();
    }

    /// <summary>
    /// 进度更新方法
    /// </summary>
    /// <param name="args">进度报告参数</param>
    private void ProgressUpdate(FastZipStreamDownload.ProgressChangedArgs args)
    {
        if (args.Entry == null)
        {
            CurrentProcessingStage = args.ProcessingStage;
            Exception = args.Exception;
            switch (args.ProcessingStage)
            {
                case FastZipStreamDownload.ProcessingStageEnum.CreatingDirectory:
                {
                    if (args.Entries != null) _directoryEntries = args.Entries;
                    break;
                }
                case FastZipStreamDownload.ProcessingStageEnum.DownloadingAndExtractingFile:
                {
                    if (args.Entries != null) _fileEntries = args.Entries;
                    break;
                }
            }
        }
        else SetEntryStatus(args);

        _needRefreshProgress = 1;
        _needRefreshSpeed = 1;
    }

    /// <summary>
    /// 进度刷新方法
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    private async Task ProgressRefresh(CancellationToken cancellationToken)
    {
        while (true)
        {
            try
            {
                await Task.Delay(100, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            if (Interlocked.CompareExchange(ref _needRefreshProgress, 0, 1) != 1) continue;

            DownloadBytesTotal ??= _fileEntries?.Sum(e => e.CompressedSize);
            DownloadBytesCompleted = _entriesStatus.Values.Sum(e => e.DownloadBytesCompletedIfVerified ?? 0);
            DownloadCompletionPercentage = DownloadBytesTotal is null or 0
                ? 0
                : (double)DownloadBytesCompleted * 100 / DownloadBytesTotal.Value;
            if (DownloadBytesTotal.HasValue && DownloadBytesPerSecond > 0)
                DownloadRemainingTime =
                    TimeSpan.FromSeconds((DownloadBytesTotal.Value - DownloadBytesCompleted) / DownloadBytesPerSecond);

            ExtractBytesTotal ??= _fileEntries?.Sum(e => e.Size);
            ExtractBytesCompleted = _entriesStatus.Values.Sum(e => e.ExtractBytesCompletedIfVerified ?? 0);
            ExtractCompletionPercentage = ExtractBytesTotal is null or 0
                ? 0
                : (double)ExtractBytesCompleted * 100 / ExtractBytesTotal.Value;
            if (ExtractBytesTotal.HasValue && ExtractBytesPerSecond > 0)
                ExtractRemainingTime =
                    TimeSpan.FromSeconds((ExtractBytesTotal.Value - ExtractBytesCompleted) / ExtractBytesPerSecond);

            ProgressUpdateEvent?.Invoke(this, EventArgs.Empty);
        }
    }

    /// <summary>
    /// 速度刷新方法
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    private async Task SpeedRefresh(CancellationToken cancellationToken)
    {
        var downloadBytesCompleted = 0L;
        var extractBytesCompleted = 0L;

        var stopwatch = new Stopwatch();
        while (true)
        {
            try
            {
                await Task.Delay(1000, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            var lastDownloadBytesCompleted = downloadBytesCompleted;
            var lastExtractBytesCompleted = extractBytesCompleted;
            if (Interlocked.CompareExchange(ref _needRefreshSpeed, 0, 1) == 1)
            {
                downloadBytesCompleted = _entriesStatus.Values.Sum(e => e.DownloadBytesCompleted ?? 0);
                extractBytesCompleted = _entriesStatus.Values.Sum(e => e.ExtractBytesCompleted ?? 0);
                DownloadBytesPerSecond =
                    (downloadBytesCompleted - lastDownloadBytesCompleted) * 1000 / (double)stopwatch.ElapsedMilliseconds;
                ExtractBytesPerSecond =
                    (extractBytesCompleted - lastExtractBytesCompleted) * 1000 / (double)stopwatch.ElapsedMilliseconds;
            }
            else
            {
                DownloadBytesPerSecond = ExtractBytesPerSecond = 0;
            }

            stopwatch.Restart();
        }
        stopwatch.Stop();
    }

    /// <summary>
    /// 根据进度报告参数设置实体状态
    /// </summary>
    /// <param name="args">进度报告参数</param>
    private void SetEntryStatus(FastZipStreamDownload.ProgressChangedArgs args)
    {
        if (args.Entry == null) return;
        var entryStatus = _entriesStatus.GetOrAdd(args.Entry, _ => new EntryStatus());

        entryStatus.ProcessingStage = args.ProcessingStage;
        entryStatus.Exception = args.Exception;

        switch (args.ProcessingStage)
        {
            case FastZipStreamDownload.ProcessingStageEnum.VerifyingExistingFile:
            {
                if (args.BytesCompleted != null) entryStatus.VerifyBytesCompleted = args.BytesCompleted;
                if (args.BytesTotal != null) entryStatus.VerifyBytesTotal = args.BytesTotal;
                if (args.Completed) entryStatus.VerifyCompleted = true;
                break;
            }
            case FastZipStreamDownload.ProcessingStageEnum.DownloadingFile or
                FastZipStreamDownload.ProcessingStageEnum.StreamExtractingFile:
            {
                if (args.BytesCompleted != null)
                    entryStatus.DownloadBytesCompletedIfVerified = entryStatus.DownloadBytesCompleted = args.BytesCompleted;
                if (args.BytesTotal != null) entryStatus.DownloadBytesTotal = args.BytesTotal;
                if (args.Completed) entryStatus.DownloadCompleted = true;
                break;
            }
            case FastZipStreamDownload.ProcessingStageEnum.ExtractingFile:
            {
                if (args.BytesCompleted != null)
                    entryStatus.ExtractBytesCompletedIfVerified = entryStatus.ExtractBytesCompleted = args.BytesCompleted;
                if (args.BytesTotal != null) entryStatus.ExtractBytesTotal = args.BytesTotal;
                if (args.Completed) entryStatus.ExtractCompleted = true;
                break;
            }
            case FastZipStreamDownload.ProcessingStageEnum.CrcVerifyingFile:
            {
                if (args.BytesCompleted != null) entryStatus.CrcVerifyBytesCompleted = args.BytesCompleted;
                if (args.BytesTotal != null) entryStatus.CrcVerifyBytesTotal = args.BytesTotal;
                if (args.Completed) entryStatus.CrcVerifyCompleted = true;
                break;
            }
            case FastZipStreamDownload.ProcessingStageEnum.None:
            {
                if (!entryStatus.DownloadCompleted)
                    entryStatus.DownloadBytesCompletedIfVerified = args.Entry.CompressedSize;
                if (!entryStatus.ExtractCompleted)
                    entryStatus.ExtractBytesCompletedIfVerified = args.Entry.Size;
                break;
            }
        }
    }
}