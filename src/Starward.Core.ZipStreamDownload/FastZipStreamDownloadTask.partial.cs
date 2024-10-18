using System.Collections.Concurrent;
using ICSharpCode.SharpZipLib.Zip;
using Starward.Core.ZipStreamDownload.Exceptions;
using Starward.Core.ZipStreamDownload.Extensions;
using Starward.Core.ZipStreamDownload.Http.Exceptions;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// ZIP流式解压类
/// </summary>
public partial class FastZipStreamDownload
{
    private class EntryTaskData
    {
        public required ZipEntry Entry { get; init; }

        public required DirectoryInfo ExtractedFileDirectoryInfo { get; init; }

        public required FileInfo ExtractedFileInfo { get; init; }

        public required FileInfo ExtractedFileTempInfo { get; init; }

        public required DirectoryInfo CompressedFileDirectoryInfo { get; init; }

        public required FileInfo CompressedFileInfo { get; init; }

        public Stream? CompressedFileStream { get; set; }
    }

    private readonly ConcurrentQueue<EntryTaskData> _fileVerifyTaskQueue = new();

    private int _fileVerifyTaskCount;

    private readonly ConcurrentQueue<EntryTaskData> _entryDownloadTaskQueue = new();

    private int _entryDownloadTaskCount;

    private readonly ConcurrentQueue<EntryTaskData> _entryExtractAndFileCrcVerifyTaskQueue = new();

    private int _entryExtractAndFileCrcVerifyTaskCount;

    private int _entryTaskCount;

    private void CleanEntryTasks()
    {
        _fileVerifyTaskQueue.Clear();
        _fileVerifyTaskCount = 0;
        _entryDownloadTaskQueue.Clear();
        _entryDownloadTaskCount = 0;
        _entryExtractAndFileCrcVerifyTaskQueue.Clear();
        _entryExtractAndFileCrcVerifyTaskCount = 0;
        _entryTaskCount = 0;
    }

    private void AddEntryTask(ZipEntry entry)
    {
        var fileDirectory = entry.GetFileDirectory() ?? "";
        var fileName = entry.GetFileName();
        if (fileName == null) InvalidZipEntryNameException.ThrowByZipEntryName(entry.Name);

        var tempFileDirectory = Path.Join(TempDirectoryInfo.FullName, fileDirectory);
        var tempFileInfo = new FileInfo(Path.Join(tempFileDirectory, fileName));
        _ = Directory.CreateDirectory(tempFileDirectory);

        FileInfo targetFileInfo;
        if (TempDirectoryInfo.FullName != TargetDirectoryInfo.FullName)
        {
            var targetFileDirectory = Path.Join(TargetDirectoryInfo.FullName, fileDirectory);
            targetFileInfo = new FileInfo(Path.Join(targetFileDirectory, fileName));
            _ = Directory.CreateDirectory(targetFileDirectory);
        }
        else targetFileInfo = tempFileInfo;

        var compressedFileInfo = new FileInfo($"{tempFileInfo.FullName}.zip");

        var entryData = new EntryTaskData
        {
            Entry = entry,
            CompressedFileDirectoryInfo = TempDirectoryInfo,
            CompressedFileInfo = compressedFileInfo,
            ExtractedFileDirectoryInfo = TargetDirectoryInfo,
            ExtractedFileInfo = targetFileInfo,
            ExtractedFileTempInfo = new FileInfo($"{targetFileInfo.FullName}_tmp")
        };

        _fileVerifyTaskQueue.Enqueue(entryData);
        _entryTaskCount += 1;
    }

    private async Task WaitExecuteEntryTasksAsync(IZipFileDownloadFactory zipFileDownloadFactory,
        bool extractFiles, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var existingFileVerifyThreadCount = Math.Min(_entryTaskCount, _existingFileVerifyThreadCount);
        var downloadThreadCount = Math.Min(_entryTaskCount, _downloadThreadCount);
        var extractAndCrcVerifyThreadCount = Math.Min(_entryTaskCount, _extractAndCrcVerifyThreadCount);
        try
        {
            using var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var taskList = new List<Task>(
                existingFileVerifyThreadCount + downloadThreadCount + extractAndCrcVerifyThreadCount);
            for (var i = 0; i < existingFileVerifyThreadCount; i++)
                taskList.Add(FileVerifyTaskMethod(cancellationTokenSource.Token));
            for (var i = 0; i < downloadThreadCount; i++)
                taskList.Add(EntryDownloadTaskMethod(zipFileDownloadFactory, EnableFullStreamDownload,
                    cancellationTokenSource.Token));
            for (var i = 0; i < extractAndCrcVerifyThreadCount; i++)
                taskList.Add(EntryExtractAndFileCrcVerifyTaskMethod(EnableFullStreamDownload, extractFiles,
                    cancellationTokenSource.Token));
            taskList.ForEach(t => t.ConfigureAwait(false).GetAwaiter().OnCompleted(() =>
            {
                // ReSharper disable once AccessToDisposedClosure
                if (t.IsFaulted) cancellationTokenSource.CancelAsync();
            }));
            var whenAllTask = Task.WhenAll(taskList);
            try
            {
                await whenAllTask.ConfigureAwait(false);
            }
            catch
            {
                // ignored
            }
            if (whenAllTask.Exception is { InnerExceptions.Count: > 0 })
            {
                var innerExceptions = whenAllTask.Exception.InnerExceptions;
                if (innerExceptions.Count == 1) throw innerExceptions[0];
                if (innerExceptions.Count > 1) throw new AggregateException(innerExceptions);
            }
            cancellationToken.ThrowIfCancellationRequested();
        }
        finally
        {
            CleanEntryTasks();
        }
    }

    private async Task FileVerifyTaskMethod(
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_fileVerifyTaskQueue.TryDequeue(out var taskData))
            {
                if (_fileVerifyTaskCount >= _entryTaskCount) break;
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var skipFollowupTasks = true;
            try
            {
                if (await FileVerify(taskData, cancellationToken).ConfigureAwait(false))
                {
                    _entryDownloadTaskQueue.Enqueue(taskData);
                    skipFollowupTasks = false;
                }
                else ProgressReport(ProcessingStageEnum.None, true, entry: taskData.Entry);
            }
            catch (Exception e)
            {
                ProgressReport(ProcessingStageEnum.None, true, exception: e, entry: taskData.Entry);
                _entriesExceptionDictionary[taskData.Entry] = e;

                if (e is OperationCanceledException or TaskCanceledException) return;
            }
            finally
            {
                if (skipFollowupTasks)
                {
                    Interlocked.Increment(ref _entryDownloadTaskCount);
                    Interlocked.Increment(ref _entryExtractAndFileCrcVerifyTaskCount);
                }
                Interlocked.Increment(ref _fileVerifyTaskCount);
            }
        }
    }

    private async Task EntryDownloadTaskMethod(IZipFileDownloadFactory zipFileDownloadFactory,
        bool enableFullStreamDownload, CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_entryDownloadTaskQueue.TryDequeue(out var taskData))
            {
                if (_entryDownloadTaskCount >= _entryTaskCount) break;
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            var skipFollowupTasks = true;
            try
            {
                var zipFileDownloadThreadLocal =
                    new ThreadLocal<ZipFileDownload>(zipFileDownloadFactory.GetInstance);
                if (enableFullStreamDownload)
                    await EntryDownloadStream(zipFileDownloadThreadLocal.Value!, taskData, cancellationToken)
                        .ConfigureAwait(false);
                else
                    await EntryDownloadCompressedFile(zipFileDownloadThreadLocal.Value!, taskData, cancellationToken)
                        .ConfigureAwait(false);
                _entryExtractAndFileCrcVerifyTaskQueue.Enqueue(taskData);
                skipFollowupTasks = false;
            }
            catch (HttpFileModifiedDuringPartialDownload)
            {
                //文件在下载过程中被修改了
                if (taskData.CompressedFileStream != null)
                    await taskData.CompressedFileStream.DisposeAsync().ConfigureAwait(false);
                throw;
            }
            catch (Exception e)
            {
                ProgressReport(ProcessingStageEnum.None, true, exception: e, entry: taskData.Entry);
                _entriesExceptionDictionary[taskData.Entry] = e;

                if (taskData.CompressedFileStream != null)
                    await taskData.CompressedFileStream.DisposeAsync().ConfigureAwait(false);

                if (e is OperationCanceledException or TaskCanceledException) return;
            }
            finally
            {
                if (skipFollowupTasks)
                    Interlocked.Increment(ref _entryExtractAndFileCrcVerifyTaskCount);
                Interlocked.Increment(ref _entryDownloadTaskCount);
            }
        }
    }

    private async Task EntryExtractAndFileCrcVerifyTaskMethod(bool enableFullStreamDownload, bool extractFiles,
        CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (!_entryExtractAndFileCrcVerifyTaskQueue.TryDequeue(out var taskData))
            {
                if (_entryExtractAndFileCrcVerifyTaskCount >= _entryTaskCount) break;
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                continue;
            }

            try
            {
                if (!enableFullStreamDownload && extractFiles)
                    await EntryExtract(taskData, cancellationToken).ConfigureAwait(false);
                var crcCheckResult = true;
                if (CheckCrcExtracted) crcCheckResult =
                    await FileCrcVerify(taskData, cancellationToken).ConfigureAwait(false);
                if (crcCheckResult)
                    taskData.ExtractedFileTempInfo.MoveTo(taskData.ExtractedFileInfo.FullName, true);
            }
            catch (Exception e)
            {
                ProgressReport(ProcessingStageEnum.None, true, exception: e, entry: taskData.Entry);
                _entriesExceptionDictionary[taskData.Entry] = e;

                if (e is OperationCanceledException or TaskCanceledException) return;
            }
            finally
            {
                Interlocked.Increment(ref _entryExtractAndFileCrcVerifyTaskCount);

                if (taskData.CompressedFileStream != null)
                    await taskData.CompressedFileStream.DisposeAsync().ConfigureAwait(false);
            }
        }
    }
}