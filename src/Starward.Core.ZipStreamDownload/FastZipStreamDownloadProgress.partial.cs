using ICSharpCode.SharpZipLib.Zip;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// ZIP流式解压类
/// </summary>
public partial class FastZipStreamDownload
{
    /// <summary>
    /// 处理阶段枚举
    /// </summary>
    public enum ProcessingStageEnum
    {
        /// <summary>
        /// 无操作
        /// </summary>
        None,

        /// <summary>
        /// 正在下载中央文件夹信息文件
        /// </summary>
        DownloadingCentralDirectoryDataFile,

        /// <summary>
        /// 正在创建文件夹
        /// </summary>
        CreatingDirectory,

        /// <summary>
        /// 正在验证已经存在的文件
        /// </summary>
        VerifyingExistingFile,

        /// <summary>
        /// 正在下载文件
        /// </summary>
        DownloadingFile,

        /// <summary>
        /// 正在解压文件
        /// </summary>
        ExtractingFile,

        /// <summary>
        /// 正在下载和解压文件
        /// </summary>
        DownloadingAndExtractingFile,

        /// <summary>
        /// 正在流式解压文件
        /// </summary>
        StreamExtractingFile,

        /// <summary>
        /// 正在进行CRC32校验
        /// </summary>
        CrcVerifyingFile
    }

    /// <summary>
    /// 进度报告参数
    /// </summary>
    /// <param name="processingStage">当前处理阶段</param>
    /// <param name="completed">当前任务是否完成</param>
    /// <param name="bytesCompleted">当前任务已经完成的字节数</param>
    /// <param name="bytesTotal">当前任务需处理的总字节数</param>
    /// <param name="exception">当前任务异常</param>
    /// <param name="entry">当前任务处理的<see cref="ZipEntry"/></param>
    /// <param name="entries">需处理的<see cref="ZipEntry"/>列表</param>
    public class ProgressChangedArgs(
        ProcessingStageEnum processingStage,
        bool completed,
        long? bytesCompleted,
        long? bytesTotal,
        Exception? exception,
        ZipEntry? entry,
        IReadOnlyCollection<ZipEntry>? entries
        )
    {
        /// <summary>
        /// 当前处理阶段
        /// </summary>
        public ProcessingStageEnum ProcessingStage { get; } = processingStage;

        /// <summary>
        /// 当前任务是否完成
        /// </summary>
        public bool Completed { get; } = completed;

        /// <summary>
        /// 当前任务已经完成的字节数
        /// </summary>
        public long? BytesCompleted { get; } = bytesCompleted;

        /// <summary>
        /// 当前任务需处理的总字节数
        /// </summary>
        public long? BytesTotal { get; } = bytesTotal;

        /// <summary>
        /// 当前任务异常
        /// </summary>
        public Exception? Exception { get; } = exception;

        /// <summary>
        /// 当前任务处理的<see cref="ZipEntry"/>
        /// </summary>
        public ZipEntry? Entry { get; } = entry;

        /// <summary>
        /// 需处理的<see cref="ZipEntry"/>列表
        /// </summary>
        public IReadOnlyCollection<ZipEntry>? Entries { get; } = entries;
    }

    /// <summary>
    /// 如果进度报告属性不为空，则立即报告进度。
    /// </summary>
    /// <param name="processingStage">当前处理阶段</param>
    /// <param name="completed">当前任务是否完成</param>
    /// <param name="bytesCompleted">当前任务已经完成的字节数</param>
    /// <param name="bytesTotal">当前任务需处理的总字节数</param>
    /// <param name="exception">当前任务异常</param>
    /// <param name="entry">当前任务处理的<see cref="ZipEntry"/></param>
    /// <param name="entries">需处理的<see cref="ZipEntry"/>列表</param>
    private void ProgressReport(ProcessingStageEnum processingStage,
        bool completed,
        long? bytesCompleted = null,
        long? bytesTotal = null,
        Exception? exception = null,
        ZipEntry? entry = null,
        IReadOnlyCollection<ZipEntry>? entries = null)
    {
        Progress?.Report(new ProgressChangedArgs(processingStage, completed, bytesCompleted, bytesTotal, exception,
            entry, entries));
    }
}