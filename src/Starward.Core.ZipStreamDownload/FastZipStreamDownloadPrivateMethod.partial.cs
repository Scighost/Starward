using System.Buffers;
using ICSharpCode.SharpZipLib.Checksum;
using ICSharpCode.SharpZipLib.Zip;
using Starward.Core.ZipStreamDownload.Exceptions;
using Starward.Core.ZipStreamDownload.Extensions;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// ZIP流式解压类
/// </summary>
public partial class FastZipStreamDownload
{
    private async Task<bool> FileVerify(EntryTaskData entryTaskData, CancellationToken cancellationToken = default)
    {
        var entry = entryTaskData.Entry;
        var extractedFileInfo = entryTaskData.ExtractedFileInfo;


        if (!extractedFileInfo.Exists) return true;
        var verifySuccess = false;
        ProgressStageChangeCallback(false);
        if (extractedFileInfo.Length == entry.Size &&
            (!CheckDateTimeVerifyingExistingFile ||
             (extractedFileInfo.CreationTimeUtc == entry.DateTime &&
              extractedFileInfo.LastWriteTimeUtc == entry.DateTime)))
        {
            if (CheckCrcVerifyingExistingFile)
            {
                var fileLength = extractedFileInfo.Length;
                ProgressStageChangeCallback(false, 0, fileLength);
                var bytesCount = 0L;
                if (entry.Crc == await GetCrcAsync(extractedFileInfo, new Progress<long>(count =>
                    {
                        bytesCount = count;
                        ProgressStageChangeCallback(false, count, fileLength);
                    }), cancellationToken).ConfigureAwait(false))
                    verifySuccess = true;
                ProgressStageChangeCallback(true, bytesCount, fileLength);
            }
            else
            {
                verifySuccess = true;
                ProgressStageChangeCallback(true);
            }
        }
        if (!verifySuccess) extractedFileInfo.Delete();
        return !verifySuccess;

        void ProgressStageChangeCallback(bool completed, long? progress = null, long? byteCount = null)
        {
            ProgressReport(ProcessingStageEnum.VerifyingExistingFile,
                completed, progress, byteCount, entry: entry); //报告实体进度
        }
    }

    private async Task EntryDownloadStream(ZipFileDownload zipFileDownload, EntryTaskData entryTaskData,
        CancellationToken cancellationToken = default)
    {
        var entry = entryTaskData.Entry;
        var extractedFileTempInfo = entryTaskData.ExtractedFileTempInfo;


        long downloadBytesCompleted = 0;
        long? downloadBytesTotal = null;
        ProgressStageChangeCallback(false, downloadBytesCompleted, downloadBytesTotal);
        extractedFileTempInfo.Delete();
        var extractedFileTempWriteStream = extractedFileTempInfo.Open(new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            PreallocationSize = entry.Size,
            Options = FileOptions.WriteThrough
        });
        await using var _ = extractedFileTempWriteStream.ConfigureAwait(false);
        try
        {
            var inputStream = await zipFileDownload.GetInputStreamAsync(entry,
                new Progress<ZipFileDownload.ProgressChangedArgs>(args =>
                {
                    downloadBytesCompleted = args.DownloadBytesCompleted;
                    downloadBytesTotal = args.DownloadBytesTotal;
                    ProgressStageChangeCallback(false, downloadBytesCompleted, downloadBytesTotal);
                }), cancellationToken).ConfigureAwait(false);
            await using var ___ = inputStream.ConfigureAwait(false);
            await inputStream.CopyToAsync(extractedFileTempWriteStream, cancellationToken).ConfigureAwait(false);
            await extractedFileTempWriteStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            extractedFileTempInfo.CreationTimeUtc = extractedFileTempInfo.LastWriteTimeUtc = entry.DateTime;
        }
        catch
        {
            await extractedFileTempWriteStream.DisposeAsync().ConfigureAwait(false);
            extractedFileTempInfo.Delete();
            throw;
        }

        ProgressStageChangeCallback(true, downloadBytesCompleted, downloadBytesTotal);
        return;


        void ProgressStageChangeCallback(bool completed, long? progress = null, long? byteCount = null)
        {
            ProgressReport(ProcessingStageEnum.StreamExtractingFile,
                completed, progress, byteCount, entry: entry); //报告实体进度
        }
    }

    private async Task EntryDownloadCompressedFile(ZipFileDownload zipFileDownload, EntryTaskData entryTaskData,
        CancellationToken cancellationToken = default)
    {
        var entry = entryTaskData.Entry;
        var compressedFileInfo = entryTaskData.CompressedFileInfo;


        long downloadBytesCompleted = 0;
        long? downloadBytesTotal = null;
        ProgressStageChangeCallback(false, downloadBytesCompleted, downloadBytesTotal);
        var compressedFileStream = compressedFileInfo.Open(FileMode.OpenOrCreate, FileAccess.ReadWrite);
        try
        {
            await zipFileDownload.GetEntryZipFileAsync(entry, compressedFileInfo,
                new Progress<ZipFileDownload.ProgressChangedArgs>(args =>
                {
                    downloadBytesCompleted = args.DownloadBytesCompleted;
                    downloadBytesTotal = args.DownloadBytesTotal;
                    ProgressStageChangeCallback(false, downloadBytesCompleted, downloadBytesTotal);
                }), cancellationToken).ConfigureAwait(false);
            compressedFileStream.Seek(0, SeekOrigin.Begin);
        } catch
        {
            await compressedFileStream.DisposeAsync().ConfigureAwait(false);
            compressedFileInfo.Delete();
            throw;
        }
        entryTaskData.CompressedFileStream = compressedFileStream;
        ProgressStageChangeCallback(true, downloadBytesCompleted, downloadBytesTotal);
        return;


        void ProgressStageChangeCallback(bool completed, long? progress = null, long? byteCount = null)
        {
            ProgressReport(ProcessingStageEnum.DownloadingFile,
                completed, progress, byteCount, entry: entry); //报告实体进度
        }
    }

    private async Task EntryExtract(EntryTaskData entryTaskData, CancellationToken cancellationToken = default)
    {
        var entry = entryTaskData.Entry;
        var compressedFileInfo = entryTaskData.CompressedFileInfo;
        var compressedFileStream = entryTaskData.CompressedFileStream!;
        var extractedFileTempInfo = entryTaskData.ExtractedFileTempInfo;

        ProgressStageChangeCallback(false, 0, entry.Size);
        using var zipFile = new ZipFile(compressedFileStream);
        string? testErrorMessage = null;
        var testResult = zipFile.TestArchive(testData: false, TestStrategy.FindFirstError, (_, message) =>
        {
            if (!string.IsNullOrEmpty(message)) testErrorMessage = message;
        });
        if (!testResult)
        {
            zipFile.Close();
            await compressedFileStream.DisposeAsync().ConfigureAwait(false);
            compressedFileInfo.Delete();
            ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name, testErrorMessage!);
        }

        var copiedBytesCount = 0L;
        extractedFileTempInfo.Delete();
        var extractedFileTempWriteStream = extractedFileTempInfo.Open(new FileStreamOptions
        {
            Mode = FileMode.CreateNew,
            Access = FileAccess.Write,
            PreallocationSize = entry.Size,
            Options = FileOptions.WriteThrough
        });
        await using var _ = extractedFileTempWriteStream.ConfigureAwait(false);
        try
        {
            var inputStream = zipFile.GetInputStream(0);
            await using var ____ = inputStream.ConfigureAwait(false);
            await inputStream.CopyToAsync(extractedFileTempWriteStream, new Progress<long>(count =>
            {
                copiedBytesCount = count;
                ProgressStageChangeCallback(false, copiedBytesCount, entry.Size);
            }), cancellationToken).ConfigureAwait(false);
            await extractedFileTempWriteStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            extractedFileTempInfo.CreationTimeUtc = extractedFileTempInfo.LastWriteTimeUtc = entry.DateTime;
        }
        catch
        {
            await extractedFileTempWriteStream.DisposeAsync().ConfigureAwait(false);
            extractedFileTempInfo.Delete();
            throw;
        }

        ProgressStageChangeCallback(true, copiedBytesCount, entry.Size);
        return;


        void ProgressStageChangeCallback(bool completed, long? progress = null, long? byteCount = null)
        {
            ProgressReport(ProcessingStageEnum.ExtractingFile,
                completed, progress, byteCount, entry: entry); //报告实体进度
        }
    }

    private async Task<bool> FileCrcVerify(EntryTaskData entryTaskData, CancellationToken cancellationToken = default)
    {
        var entry = entryTaskData.Entry;
        var extractedFileTempInfo = entryTaskData.ExtractedFileTempInfo;

        var extractedFileLength = extractedFileTempInfo.Length;

        ProgressStageChangeCallback(false, 0, extractedFileLength);
        var bytesCount = 0L;

        var result = true;
        if (entry.Crc != await GetCrcAsync(extractedFileTempInfo, new Progress<long>(count =>
            {
                bytesCount = count;
                ProgressStageChangeCallback(false, count, extractedFileLength);
            }), cancellationToken).ConfigureAwait(false))
        {
            result = false;
            extractedFileTempInfo.Delete();
            CrcVerificationFailedException.ThrowByZipEntryName(entry.Name);
        }

        ProgressStageChangeCallback(true, bytesCount, extractedFileLength);
        return result;


        void ProgressStageChangeCallback(bool completed, long? progress = null, long? byteCount = null)
        {
            ProgressReport(ProcessingStageEnum.CrcVerifyingFile,
                completed, progress, byteCount, entry: entry); //报告实体进度
        }
    }

    /// <summary>
    /// 通过ZIP实体对象创建文件夹
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="extractFiles">是否对下载好的文件进行解压（只对半流式下载模式生效）</param>
    /// <param name="processingStageChangedCallback">进度报告回调方法</param>
    private void CreateDirectoryByEntry(ZipEntry entry, bool extractFiles,
        Action<ProcessingStageEnum, bool> processingStageChangedCallback)
    {
        DirectoryInfo? fullTempDirectoryInfo = null;
        DirectoryInfo fullTargetDirectoryInfo;
        var entryCleanName = ZipEntry.CleanName(entry.Name);
        var stageSent = false; //如果文件夹已经存在了，不再报告进度

        if (!EnableFullStreamDownload)
        {
            fullTempDirectoryInfo = new DirectoryInfo(Path.Join(TempDirectoryInfo.FullName, entryCleanName));
            if (!fullTempDirectoryInfo.Exists)
            {
                processingStageChangedCallback(ProcessingStageEnum.CreatingDirectory, false);
                stageSent = true;
                fullTempDirectoryInfo.Create();
            }

            if (!extractFiles) return;
        }

        //如果临时文件夹和目标文件夹相同，则不再重复创建文件夹
        if (TempDirectoryInfo.FullName != TargetDirectoryInfo.FullName || fullTempDirectoryInfo == null)
        {
            fullTargetDirectoryInfo = new DirectoryInfo(Path.Join(TargetDirectoryInfo.FullName, entryCleanName));
            if (!fullTargetDirectoryInfo.Exists)
            {
                if (!stageSent)
                {
                    processingStageChangedCallback(ProcessingStageEnum.CreatingDirectory, false);
                    stageSent = true;
                }

                fullTargetDirectoryInfo.Create();
            }
        }
        else fullTargetDirectoryInfo = fullTempDirectoryInfo;

        //设置文件夹创建时间为压缩文件时间
        fullTargetDirectoryInfo.CreationTimeUtc = fullTargetDirectoryInfo.LastWriteTimeUtc = entry.DateTime;
        if (stageSent) processingStageChangedCallback(ProcessingStageEnum.CreatingDirectory, true);
    }

    /// <summary>
    /// 下载并打开中央目录文件。
    /// </summary>
    /// <param name="zipFileDownload"><see cref="ZipFileDownload"/>的实例</param>
    /// <param name="fileInfo">要下载的文件的文件信息</param>
    /// <param name="processingStageChangedCallback">进度报告回调</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取<see cref="ZipFile"/>的实例。</returns>
    private static async Task<ZipFile> DownloadAndOpenCentralDirectoryDataFileAsync
    (ZipFileDownload zipFileDownload, FileInfo fileInfo,
        Action<ProcessingStageEnum, bool, long, long?> processingStageChangedCallback,
        CancellationToken cancellationToken = default)
    {
        ZipFile? zipFile = null;
        if (fileInfo.Exists)
        {
            //尝试解析已存在的文件，如果是解析不了，说明为损坏文件，重新下载
            try
            {
                zipFile = new ZipFile(fileInfo.FullName);
            }
            catch (ZipException)
            {
            }
            catch (EndOfStreamException)
            {
                //ICSharpCode.SharpZipLib库没有处理好，解析文件出错也有可能引发读取到流末尾的异常
            }
            if (zipFile != null) return zipFile;
            fileInfo.Delete();
        }

        var progress = 0L;
        long? downloadByteCount = null;
        processingStageChangedCallback(ProcessingStageEnum.DownloadingCentralDirectoryDataFile,
            false, progress, downloadByteCount);
        var fileStream = fileInfo.Open(new FileStreamOptions
        {
            Mode = FileMode.OpenOrCreate,
            Access = FileAccess.ReadWrite,
            Options = FileOptions.SequentialScan
        });
        await using var _ = fileStream.ConfigureAwait(false);
        try
        {
            fileStream.SetLength(0);
            await zipFileDownload.GetCentralDirectoryDataAsync(fileStream,
                new Progress<ZipFileDownload.ProgressChangedArgs>(args =>
                {
                    progress = args.DownloadBytesCompleted;
                    downloadByteCount = args.DownloadBytesTotal;
                    processingStageChangedCallback(ProcessingStageEnum.DownloadingCentralDirectoryDataFile,
                        false, progress, downloadByteCount);
                }), cancellationToken).ConfigureAwait(false);
            await fileStream.FlushAsync(cancellationToken).ConfigureAwait(false);
            processingStageChangedCallback(ProcessingStageEnum.DownloadingCentralDirectoryDataFile,
                true, progress, downloadByteCount);
            return new ZipFile(fileStream);
        }
        finally
        {
            await fileStream.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 计算CRC32校验和（异步）
    /// </summary>
    /// <param name="fileInfo">要计算CRC32校验和的文件的文件信息</param>
    /// <param name="progress">进度报告对象（报告已经读取的字节数）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取CRC32校验和。</returns>
    private static async Task<long> GetCrcAsync(FileInfo fileInfo,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var fileStream = fileInfo.Open(new FileStreamOptions
        {
            Mode = FileMode.Open,
            Access = FileAccess.Read,
            Options = FileOptions.SequentialScan
        });
        await using var _ = fileStream.ConfigureAwait(false);
        return await GetCrcAsync(fileStream, progress, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 计算CRC32校验和（异步）
    /// </summary>
    /// <param name="stream">要计算CRC32校验和的数据流</param>
    /// <param name="progress">进度报告对象（报告已经读取的字节数）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取CRC32校验和。</returns>
    private static async Task<long> GetCrcAsync(Stream stream,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var crc32ThreadLocal = new ThreadLocal<Crc32>(() => new Crc32());
        var crc32 = crc32ThreadLocal.Value!;
        crc32.Reset();

        var count = 0;
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) > 0)
            {
                crc32.Update(buffer[..bytesRead]);
                count += bytesRead;
                progress?.Report(count);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return crc32.Value;
    }
}