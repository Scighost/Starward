using System.Collections.Concurrent;
using ICSharpCode.SharpZipLib.Zip;
using Starward.Core.ZipStreamDownload.Exceptions;
using Starward.Core.ZipStreamDownload.Extensions;
using Starward.Core.ZipStreamDownload.Http.Exceptions;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// ZIP流式解压类
/// </summary>
/// <param name="targetDirectoryInfo">解压目标文件夹信息对象</param>
/// <param name="tempDirectoryInfo">解压临时文件夹信息对象</param>
public partial class FastZipStreamDownload(DirectoryInfo targetDirectoryInfo, DirectoryInfo tempDirectoryInfo)
{
    /// <summary>
    /// 默认ZIP文件的文件名
    /// </summary>
    private const string DefaultZipFileName = "temp.zip";

    /// <summary>
    /// 中央目录数据文件的后缀名
    /// </summary>
    private const string CentralDirectoryDataFileNameExtension = "zipcdr";

    /// <summary>
    /// 进度改变报告接口
    /// </summary>
    public IProgress<ProgressChangedArgs>? Progress { get; set; }

    /// <summary>
    /// 用于解压数据的目标文件夹信息对象
    /// </summary>
    public DirectoryInfo TargetDirectoryInfo { get; set; } = targetDirectoryInfo;

    /// <summary>
    /// 用于解压数据的目标文件夹路径
    /// </summary>
    public string TargetDirectoryPath
    {
        get => TargetDirectoryInfo.FullName;
        set => TargetDirectoryInfo = new DirectoryInfo(value);
    }

    /// <summary>
    /// 用于存放中央目录文件和Zip文件的临时文件夹对象
    /// <remarks>可与解压文件夹相同</remarks>
    /// </summary>
    public DirectoryInfo TempDirectoryInfo { get; set; } = tempDirectoryInfo;

    /// <summary>
    /// 用于存放中央目录文件和Zip文件的临时文件夹路径
    /// <remarks>可与解压文件夹相同</remarks>
    /// </summary>
    public string TempDirectoryPath
    {
        get => TempDirectoryInfo.FullName;
        set => TempDirectoryInfo = new DirectoryInfo(value);
    }

    /// <summary>
    /// 文件解压完成后是否进行CRC32校验
    /// </summary>
    public bool CheckCrcExtracted { get; set; } = true;

    /// <summary>
    /// 文件验证时是否进行CRC32校验
    /// </summary>
    public bool CheckCrcVerifyingExistingFile { get; set; } = true;

    /// <summary>
    /// 文件验证时是否进行创建日期和修改日期校验
    /// </summary>
    public bool CheckDateTimeVerifyingExistingFile { get; set; } = false;

    /// <summary>
    /// 是否在解压完成后自动删除中央目录文件
    /// </summary>
    public bool AutoDeleteCentralDirectoryDataFile { get; set; } = true;

    /// <summary>
    /// 是否开启全流式下载
    /// <remarks>更节省硬盘空间，不支持单文件断点续传</remarks>
    /// </summary>
    public bool EnableFullStreamDownload { get; set; }

    /// <summary>
    /// 可允许的最大验证线程数
    /// <remarks>取值范围(0,30)，默认为CPU线程数</remarks>
    /// </summary>
    public int ExistingFileVerifyThreadCount
    {
        get => _existingFileVerifyThreadCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 30);
            _existingFileVerifyThreadCount = value;
        }
    }

    /// <summary>
    /// （内部）可允许的最大验证线程数
    /// </summary>
    private int _existingFileVerifyThreadCount = Math.Max(Environment.ProcessorCount, 30);

    /// <summary>
    /// 可允许的最大下载线程数
    /// <remarks>取值范围(0,20)，默认10</remarks>
    /// </summary>
    public int DownloadThreadCount
    {
        get => _downloadThreadCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 20);
            _downloadThreadCount = value;
        }
    }

    /// <summary>
    /// （内部）可允许的最大下载线程数
    /// </summary>
    private int _downloadThreadCount = 10;

    /// <summary>
    /// 可允许的最大解压和CRC校验线程数
    /// <remarks>取值范围(0,30)，默认为CPU线程数</remarks>
    /// </summary>
    public int ExtractAndCrcVerifyThreadCount
    {
        get => _extractAndCrcVerifyThreadCount;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 30);
            _extractAndCrcVerifyThreadCount = value;
        }
    }

    /// <summary>
    /// （内部）可允许的最大解压和CRC校验线程数
    /// </summary>
    private int _extractAndCrcVerifyThreadCount = Math.Max(Environment.ProcessorCount, 30);

    /// <summary>
    /// 当前的实体异常列表
    /// </summary>
    public IReadOnlyDictionary<ZipEntry, Exception> EntriesExceptionDictionary => _entriesExceptionDictionary;

    /// <summary>
    /// （内部）ZIP文件下载是否正在执行
    /// </summary>
    private int _downloadZipFileIsRunning;

    /// <summary>
    /// （内部）当前的实体异常列表
    /// </summary>
    private readonly ConcurrentDictionary<ZipEntry, Exception> _entriesExceptionDictionary = new();

    /// <summary>
    /// 创建一个ZIP流式解压类的实例
    /// </summary>
    /// <param name="targetDirectoryPath">解压目标文件夹路径</param>
    /// <param name="tempDirectoryPath">解压临时文件夹路径</param>
    public FastZipStreamDownload(string targetDirectoryPath, string tempDirectoryPath) :
        this(new DirectoryInfo(targetDirectoryPath), new DirectoryInfo(tempDirectoryPath))
    {
    }

    /// <summary>
    /// 创建一个ZIP流式解压类的实例
    /// </summary>
    /// <param name="targetDirectoryInfo">解压目标文件夹信息对象</param>
    /// <remarks>临时文件夹路径和目标文件夹路径相同</remarks>
    public FastZipStreamDownload(DirectoryInfo targetDirectoryInfo) :
        this(targetDirectoryInfo, targetDirectoryInfo)
    {
    }

    /// <summary>
    /// 创建一个ZIP流式解压类的实例
    /// </summary>
    /// <param name="targetDirectorPath">解压目标文件夹路径</param>
    /// <remarks>临时文件夹路径和目标文件夹路径相同</remarks>
    public FastZipStreamDownload(string targetDirectorPath) :
        this(new DirectoryInfo(targetDirectorPath))
    {
    }

    /// <summary>
    /// 下载并获取<see cref="ZipFile"/>的实例，使用该实例可获取实体文件的列表。
    /// </summary>
    /// <param name="zipFileDownloadFactory"><see cref="IZipFileDownloadFactory"/>的实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，返回一个<see cref="ZipFile"/>的实例。</returns>
    private async Task<ZipFile> GetZipFileCentralDirectoryDataFileAsync(IZipFileDownloadFactory zipFileDownloadFactory,
        CancellationToken cancellationToken = default)
    {
        //参数校验
        ThrowException.ThrowDirectoryNotFoundExceptionIfDirectoryNotExists(TargetDirectoryInfo);
        ThrowException.ThrowDirectoryNotFoundExceptionIfDirectoryNotExists(TempDirectoryInfo);

        //不允许执行多次
        if (Interlocked.Increment(ref _downloadZipFileIsRunning) > 1)
        {
            Interlocked.Decrement(ref _downloadZipFileIsRunning);
            throw new InvalidOperationException();
        }

        _entriesExceptionDictionary.Clear();

        try
        {
            var result = await CoreAsync().ConfigureAwait(false);
            ProgressReport(ProcessingStageEnum.None, true); //报告全局进度
            return result;
        }
        finally
        {
            Interlocked.Decrement(ref _downloadZipFileIsRunning);
        }

        async Task<ZipFile> CoreAsync()
        {
            var zipFileName =
                (zipFileDownloadFactory.ZipFileUri == null ? null : zipFileDownloadFactory.ZipFileUri.GetFileName()) ??
                DefaultZipFileName; //如果找不到URL中的文件名，则默认为temp.zip。

            var centralDirectoryDataFileInfo =
                new FileInfo(Path.Join(TempDirectoryInfo.FullName,
                    $"{zipFileName}.{CentralDirectoryDataFileNameExtension}"));

            var zipFileDownload = zipFileDownloadFactory.GetInstance();

            Exception? exception = null;
            try
            {
                return await DownloadAndOpenCentralDirectoryDataFileAsync(zipFileDownload,
                    centralDirectoryDataFileInfo, (processingStage, completed,
                        progress, downloadByteCount) => ProgressReport(processingStage, completed,
                        progress, downloadByteCount) //报告实体进度
                    , cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                exception = e;
                centralDirectoryDataFileInfo.Delete(); //删除中央目录文件，下次重新获取
                throw;
            }
            finally
            {
                ProgressReport(ProcessingStageEnum.None, true, exception: exception);
            }
        }
    }

    /// <summary>
    /// 获取一个<see cref="ZipEntry"/>的列表，该列表表示一个文件实体的列表。
    /// </summary>
    /// <param name="zipFileDownloadFactory"><see cref="IZipFileDownloadFactory"/>的实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，返回一个<see cref="ZipEntry"/>的列表。</returns>
    public async Task<List<ZipEntry>> GetZipFileFileEntriesAsync(IZipFileDownloadFactory zipFileDownloadFactory,
        CancellationToken cancellationToken = default)
    {
        using var centralDirectoryDataFile =
            await GetZipFileCentralDirectoryDataFileAsync(zipFileDownloadFactory, cancellationToken);
        return await centralDirectoryDataFile.GetFileEntriesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 获取一个<see cref="ZipEntry"/>的列表，该列表表示一个目录实体的列表。
    /// </summary>
    /// <param name="zipFileDownloadFactory"><see cref="IZipFileDownloadFactory"/>的实例</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，返回一个<see cref="ZipEntry"/>的列表。</returns>
    public async Task<List<ZipEntry>> GetZipFileDirectoryEntriesAsync(IZipFileDownloadFactory zipFileDownloadFactory,
        CancellationToken cancellationToken = default)
    {
        using var centralDirectoryDataFile =
            await GetZipFileCentralDirectoryDataFileAsync(zipFileDownloadFactory, cancellationToken);
        return await centralDirectoryDataFile.GetDirectoryEntriesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// 开始下载ZIP并解压文件（异步）
    /// </summary>
    /// <param name="zipFileDownloadFactory"><see cref="IZipFileDownloadFactory"/>的实例</param>
    /// <param name="extractFiles">是否对下载好的文件进行解压（只对半流式下载模式生效）</param>
    /// <param name="downloadFiles">需要下载或忽略（由<paramref name="ignoreFiles"/>决定）的文件列表，为空下载全部文件</param>
    /// <param name="ignoreFiles">在进行文件名比较时是包含还是忽略</param>
    /// <param name="ignoreCase">在进行文件名比较时是否忽略大小写</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public async Task DownloadZipFileAsync(IZipFileDownloadFactory zipFileDownloadFactory, bool extractFiles = true,
        List<string>? downloadFiles = null, bool ignoreFiles = false, bool ignoreCase = false,
        CancellationToken cancellationToken = default)
    {
        //参数校验
        ThrowException.ThrowDirectoryNotFoundExceptionIfDirectoryNotExists(TargetDirectoryInfo);
        ThrowException.ThrowDirectoryNotFoundExceptionIfDirectoryNotExists(TempDirectoryInfo);

        //不允许执行多次
        if (Interlocked.Increment(ref _downloadZipFileIsRunning) > 1)
        {
            Interlocked.Decrement(ref _downloadZipFileIsRunning);
            throw new InvalidOperationException();
        }

        _entriesExceptionDictionary.Clear();

        try
        {
            await CoreAsync().ConfigureAwait(false);
            ProgressReport(ProcessingStageEnum.None, true); //报告全局进度
        }
        finally
        {
            Interlocked.Decrement(ref _downloadZipFileIsRunning);
        }
        return;

        async Task CoreAsync()
        {
            var zipFileName =
                (zipFileDownloadFactory.ZipFileUri == null ? null : zipFileDownloadFactory.ZipFileUri.GetFileName()) ??
                DefaultZipFileName; //如果找不到URL中的文件名，则默认为temp.zip。

            var centralDirectoryDataFileInfo =
                new FileInfo(Path.Join(TempDirectoryInfo.FullName,
                    $"{zipFileName}.{CentralDirectoryDataFileNameExtension}"));

            var zipFileDownload = zipFileDownloadFactory.GetInstance();

            //下载中央目录文件
            ZipFile centralDirectoryDataFile;
            try
            {
                centralDirectoryDataFile =
                    await DownloadAndOpenCentralDirectoryDataFileAsync(zipFileDownload, centralDirectoryDataFileInfo,
                        (processingStage, completed,
                            progress, downloadByteCount) => ProgressReport(processingStage, completed,
                            progress, downloadByteCount) //报告实体进度
                        , cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                centralDirectoryDataFileInfo.Delete(); //删除中央目录文件，下次重新获取
                ProgressReport(ProcessingStageEnum.DownloadingCentralDirectoryDataFile, true, exception: e);
                throw;
            }

            //创建临时目录和解压目录结构
            var directoryEntries =
                await centralDirectoryDataFile.GetDirectoryEntriesAsync(cancellationToken).ConfigureAwait(false);
            ProgressReport(ProcessingStageEnum.CreatingDirectory, false, entries: directoryEntries); //报告全局进度
            foreach (var entry in directoryEntries)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    CreateDirectoryByEntry(entry, extractFiles, (processingStage, completed) =>
                    {
                        ProgressReport(processingStage, completed, entry: entry); //报告实体进度
                    });
                    ProgressReport(ProcessingStageEnum.None, true, entry: entry); //报告实体进度
                }
                catch (Exception e)
                {
                    ProgressReport(ProcessingStageEnum.None, true, exception: e, entry: entry); //报告实体进度
                    _entriesExceptionDictionary[entry] = e;

                    if (e is OperationCanceledException or TaskCanceledException) throw;
                }
            }

            ProgressReport(ProcessingStageEnum.CreatingDirectory, true, entries: directoryEntries); //报告全局进度

            //下载和解压文件
            var fileEntries =
                await centralDirectoryDataFile.GetFileEntriesAsync(cancellationToken).ConfigureAwait(false);
            if (downloadFiles is { Count: > 0 })
            {
                var stringComparison = ignoreCase
                    ? StringComparison.InvariantCultureIgnoreCase
                    : StringComparison.InvariantCulture;
                if (ignoreFiles)
                    fileEntries = fileEntries.Where(
                        e => downloadFiles.All(f => !f.Equals(ZipEntry.CleanName(e.Name), stringComparison))).ToList();
                else
                    fileEntries = fileEntries.Where(
                        e => downloadFiles.Any(f => f.Equals(ZipEntry.CleanName(e.Name), stringComparison))).ToList();
            }

            ProgressReport(ProcessingStageEnum.DownloadingAndExtractingFile, false, entries: fileEntries); //报告全局进度

            fileEntries.ForEach(AddEntryTask);
            try
            {
                await WaitExecuteEntryTasksAsync(zipFileDownloadFactory, extractFiles, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception e)
                when (e is HttpFileModifiedDuringPartialDownload ||
                      (e is AggregateException exception && exception.InnerExceptions.Any(
                          ie => ie is HttpFileModifiedDuringPartialDownload)))
            {
                //文件在下载过程中被修改了
                centralDirectoryDataFile.Close();
                centralDirectoryDataFileInfo.Delete(); //删除中央目录文件，下次重新获取
                ProgressReport(ProcessingStageEnum.DownloadingAndExtractingFile, true, entries: fileEntries,
                    exception: e); //报告全局进度
                throw;
            }
            catch (Exception e)
            {
                ProgressReport(ProcessingStageEnum.DownloadingAndExtractingFile, true, entries: fileEntries,
                    exception: e); //报告全局进度
                throw;
            }

            if (_entriesExceptionDictionary.IsEmpty
                && !cancellationToken.IsCancellationRequested && AutoDeleteCentralDirectoryDataFile)
            {
                centralDirectoryDataFile.Close();
                centralDirectoryDataFileInfo.Delete();
            }
        }
    }
}