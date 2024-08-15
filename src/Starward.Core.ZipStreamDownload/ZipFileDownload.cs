using System.Text;
using ICSharpCode.SharpZipLib.BZip2;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.Zip.Compression;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using Starward.Core.ZipStreamDownload.Exceptions;
using Starward.Core.ZipStreamDownload.Extensions;
using Starward.Core.ZipStreamDownload.Http;

namespace Starward.Core.ZipStreamDownload;

/// <summary>
/// 创建HTTP分段下载流的委托
/// </summary>
/// <param name="startBytes">开始的字节索引（包含）</param>
/// <param name="endBytes">结束的字节索引（不包含）</param>
public delegate Task<HttpPartialDownloadStream> HttpPartialDownloadStreamBuilder(long? startBytes, long? endBytes);

/// <summary>
/// ZIP文件下载类
/// </summary>
/// <param name="httpPartialDownloadStreamBuilder">创建HTTP分段下载流的委托</param>
/// <param name="downloadBytesRateLimiterOptionBuilder">一个返回<see cref="RateLimiterOption"/>实例的委托，表示按字节下载限速的限速器的选项</param>
public class ZipFileDownload(
    HttpPartialDownloadStreamBuilder httpPartialDownloadStreamBuilder,
    Func<RateLimiterOption>? downloadBytesRateLimiterOptionBuilder = null)
{
    /// <summary>
    /// ZIP文件的MediaType
    /// </summary>
    internal const string MediaType = "application/zip";

    /// <summary>
    /// <see cref="StringCodec"/>的实例
    /// </summary>
    private readonly StringCodec _stringCodec = ZipStrings.GetStringCodec();

    /// <summary>
    /// Skip the verification of the local header when reading an archive entry. Set this to attempt to read the
    /// entries even if the headers should indicate that doing so would fail or produce an unexpected output.
    /// </summary>
    public bool SkipLocalEntryTestsOnLocate { get; set; } = false;

    /// <summary>
    /// 进度报告参数
    /// </summary>
    /// <param name="downloadBytesCompleted">已经下载的字节数</param>
    /// <param name="downloadBytesTotal">需要下载的字节总数</param>
    public class ProgressChangedArgs(long downloadBytesCompleted, long? downloadBytesTotal = null)
    {
        /// <summary>
        /// 已经下载的字节数
        /// </summary>
        public long DownloadBytesCompleted { get; } = downloadBytesCompleted;

        /// <summary>
        /// 需要下载的字节总数
        /// </summary>
        public long? DownloadBytesTotal { get; } = downloadBytesTotal;
    }

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="writeStream">用于写入中央目录信息的流</param>
    /// <param name="progress">进度改变报告接口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public async Task GetCentralDirectoryDataAsync(Stream writeStream,
        IProgress<ProgressChangedArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        long position = 0;
        long downloadCount = 0;
        long? length = null;

        long locatedEndOfCentralDir = -1;

        IProgress<long>? innerProgress = null;

        var httpPartialDownloadStream = await httpPartialDownloadStreamBuilder(null, 1024).ConfigureAwait(false);
        await using var _ = httpPartialDownloadStream.ConfigureAwait(false);

        var rateLimitStream = GetRateLimitStream(httpPartialDownloadStream);

        var firstDownload = true;
        var stream = new MemoryStream(1024);
        await using var __ = stream.ConfigureAwait(false);

        if (progress != null) innerProgress = new Progress<long>(bytes =>
        {
            progress.Report(new ProgressChangedArgs(position + bytes, length));
        });

        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            await AddPositionAndDownloadAsync().ConfigureAwait(false);
            if (downloadCount == 0) break;
            locatedEndOfCentralDir = await ZipFormat.LocateBlockWithSignatureAsync(stream,
                    ZipConstants.EndOfCentralDirectorySignature, Math.Max(position - 3, 0),
                    ZipConstants.EndOfCentralRecordBaseSize, 0, reverse: true, cancellationToken)
                .ConfigureAwait(false);
        } while (position < 0xffff && locatedEndOfCentralDir < 0);

        if (locatedEndOfCentralDir < 0)
            ZipFileTestFailedException.ThrowByReasonCentralDirectory("Cannot find central directory");

        // Read end of central directory record
        stream.SkipBytes(
            sizeof(ushort) + //ushort thisDiskNumber
            sizeof(ushort) + //ushort startCentralDirDisk
            sizeof(ushort) + //ushort entriesForThisDisk
            sizeof(ushort) //ushort entriesForWholeCentralDir
            , reverse: true);
        ulong centralDirSize = stream.ReadUint(reverse: true);
        ulong offsetOfCentralDir = stream.ReadUint(reverse: true);
        //ushort commentSize

        // Check if zip64 header information is required.
        var zip64 = centralDirSize == 0xffffffff || offsetOfCentralDir == 0xffffffff;

        long locatedZip64EndOfCentralDirLocator = -1;
        long offset64Reverse = -1;
        if (zip64)
        {
            // #357 - always check for the existence of the Zip64 central directory.
            // #403 - Take account of the fixed size of the locator when searching.
            //    Subtract from locatedEndOfCentralDir so that the endLocation is the location of EndOfCentralDirectorySignature,
            //    rather than the data following the signature.
            locatedZip64EndOfCentralDirLocator = await ZipFormat.LocateBlockWithSignatureAsync(stream,
                    ZipConstants.Zip64CentralDirLocatorSignature, locatedEndOfCentralDir + 4,
                    ZipConstants.Zip64EndOfCentralDirectoryLocatorSize, 0, reverse: true, cancellationToken)
                .ConfigureAwait(false);
            if (locatedZip64EndOfCentralDirLocator < 0)
            {
                do
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await AddPositionAndDownloadAsync().ConfigureAwait(false);
                    if (downloadCount == 0) break;
                    locatedZip64EndOfCentralDirLocator = await ZipFormat.LocateBlockWithSignatureAsync(stream,
                            ZipConstants.Zip64CentralDirLocatorSignature, Math.Max(position - 3, 0),
                            ZipConstants.Zip64EndOfCentralDirectoryLocatorSize, 0, reverse: true, cancellationToken)
                        .ConfigureAwait(false);
                } while (position < 0x1fffe && locatedZip64EndOfCentralDirLocator < 0);

                if (locatedZip64EndOfCentralDirLocator < 0)
                    ZipFileTestFailedException.ThrowByReasonCentralDirectory("Cannot find Zip64 locator");
            }

            // number of the disk with the start of the zip64 end of central directory 4 bytes
            // relative offset of the zip64 end of central directory record 8 bytes
            // total number of disks 4 bytes
            stream.SkipBytes(sizeof(uint), reverse: true); //uint startDisk64 is not currently used
            var offset64 = stream.ReadUlong(reverse: true);
            if (offset64 > long.MaxValue)
                ZipFileTestFailedException.ThrowByReasonCentralDirectory("The offset of Zip64 is too large");
            offset64Reverse = httpPartialDownloadStream.FileLength - (long)offset64;
            //stream.SkipBytes(sizeof(uint), reverse: true); //uint totalDisks

            if (offset64Reverse > position)
                await AddPositionAndDownloadAsync(offset64Reverse - position)
                    .ConfigureAwait(false);

            stream.Seek(offset64Reverse, SeekOrigin.Begin);
            long sig64 = stream.ReadUint(reverse: true);

            if (sig64 != ZipConstants.Zip64CentralFileHeaderSignature)
                ZipFileTestFailedException
                    .ThrowByReasonCentralDirectory($"Invalid Zip64 Central directory signature at {offset64:X}");

            // NOTE: Record size = SizeOfFixedFields + SizeOfVariableData - 12.
            stream.SkipBytes(
                sizeof(ulong) + //ulong recordSize
                sizeof(ushort) + //ushort versionMadeBy
                sizeof(ushort) + //ushort versionToExtract
                sizeof(uint) + //uint thisDisk
                sizeof(uint) + //uint centralDirDisk
                sizeof(ulong) + //ulong entriesForThisDisk
                sizeof(ulong) //ulong entriesForWholeCentralDir
                , reverse: true);
            centralDirSize = stream.ReadUlong(reverse: true);
            if (centralDirSize > long.MaxValue)
                ZipFileTestFailedException.ThrowByReasonCentralDirectory("The size of Zip64 Central directory is too large");
            offsetOfCentralDir = stream.ReadUlong(reverse: true);
            if (offsetOfCentralDir > long.MaxValue)
                ZipFileTestFailedException.ThrowByReasonCentralDirectory("The size of Zip64 Central directory is too large");
            // NOTE: zip64 extensible data sector (variable size) is ignored.
        }

        var centralDirectoryFileSize = httpPartialDownloadStream.FileLength - (long)offsetOfCentralDir;
        if (centralDirectoryFileSize < 0)
            ZipFileTestFailedException.ThrowByReasonCentralDirectory("The size of the central directory exceeds the compressed file size");

        if (zip64)
        {
            stream.Seek(offset64Reverse - 48, SeekOrigin.Begin);
            stream.WriteNumber(0UL, reverse: true);
            stream.Seek(locatedZip64EndOfCentralDirLocator - 4, SeekOrigin.Begin);
            stream.WriteNumber((ulong)centralDirectoryFileSize - (ulong)offset64Reverse, reverse: true);
        }
        else
        {
            stream.Seek(locatedEndOfCentralDir - 12, SeekOrigin.Begin);
            stream.WriteNumber(0U, reverse: true);
        }

        if (stream.Length > centralDirectoryFileSize) stream.SetLength(centralDirectoryFileSize);
        stream.Seek(0, SeekOrigin.Begin);
        var bufferLength = stream.Length;
        writeStream.SetLength(centralDirectoryFileSize);
        writeStream.Seek(0, SeekOrigin.End);
        await stream.CopyToReverseAsync(writeStream, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        //writeStream.Seek(Math.Min(centralDirectoryFileSize, bufferLength), SeekOrigin.Current);
        stream.Close();

        if (centralDirectoryFileSize <= bufferLength) return;

        await httpPartialDownloadStream.ResetRangeAsync((long)offsetOfCentralDir,
            httpPartialDownloadStream.FileLength - bufferLength, cancellationToken).ConfigureAwait(false);
        position = bufferLength;
        length = httpPartialDownloadStream.Length + bufferLength;
        writeStream.Seek(0, SeekOrigin.Begin);
        await rateLimitStream.CopyToAsync(writeStream,
            progress: innerProgress, cancellationToken: cancellationToken).ConfigureAwait(false);
        return;

        async Task AddPositionAndDownloadAsync(long count = 1024)
        {
            if (!firstDownload)
            {
                position += downloadCount;
                if (position == httpPartialDownloadStream.FileLength)
                {
                    downloadCount = 0;
                    return;
                }

                var startBytes = httpPartialDownloadStream.StartBytes - count;
                var endBytes = httpPartialDownloadStream.StartBytes;
                if (startBytes < 0) startBytes = 0;
                await httpPartialDownloadStream.ResetRangeAsync(startBytes, endBytes, cancellationToken)
                    .ConfigureAwait(false);
            }
            else firstDownload = false;
            downloadCount = httpPartialDownloadStream.Length;
            stream.SetLength(position + downloadCount);
            stream.Seek(0, SeekOrigin.End);
            await rateLimitStream.CopyToReverseAsync(stream, progress: innerProgress,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="writeStream">用于写入中央目录信息的流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetCentralDirectoryDataAsync(Stream writeStream, CancellationToken cancellationToken) =>
        GetCentralDirectoryDataAsync(writeStream, null, cancellationToken);

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息。
    /// </summary>
    /// <param name="writeStream">用于写入中央目录信息的流</param>
    /// <param name="progress">进度改变报告接口</param>
    public void GetCentralDirectoryData(Stream writeStream,
        IProgress<ProgressChangedArgs>? progress = null) =>
        GetCentralDirectoryDataAsync(writeStream, progress).GetAwaiter().GetResult();

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="fileInfo">用于写入中央目录信息的文件的文件信息</param>
    /// <param name="progress">进度改变报告接口</param>
    public void GetCentralDirectoryData(FileInfo fileInfo,
        IProgress<ProgressChangedArgs>? progress = null)
    {
        using var fileStream = fileInfo.Open(FileMode.Create, FileAccess.Write);
        GetCentralDirectoryData(fileStream, progress);
    }

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="fileInfo">用于写入中央目录信息的文件的文件信息</param>
    /// <param name="progress">进度改变报告接口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public async Task GetCentralDirectoryDataAsync(FileInfo fileInfo,
        IProgress<ProgressChangedArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var fileStream = fileInfo.Open(FileMode.Create, FileAccess.Write);
            await using var _ = fileStream.ConfigureAwait(false);
            await GetCentralDirectoryDataAsync(fileStream, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (fileInfo.Exists) fileInfo.Delete();
            throw;
        }
    }

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="fileInfo">用于写入中央目录信息的文件的文件信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetCentralDirectoryDataAsync(FileInfo fileInfo, CancellationToken cancellationToken) =>
        GetCentralDirectoryDataAsync(fileInfo, null, cancellationToken);

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="path">用于写入中央目录信息的文件的文件路径</param>
    /// <param name="progress">进度改变报告接口</param>
    public void GetCentralDirectoryData(string path, IProgress<ProgressChangedArgs>? progress = null)
        => GetCentralDirectoryData(new FileInfo(path), progress);

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="path">用于写入中央目录信息的文件的文件路径</param>
    /// <param name="progress">进度改变报告接口</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetCentralDirectoryDataAsync(string path, IProgress<ProgressChangedArgs>? progress = null,
        CancellationToken cancellationToken = default)
        => GetCentralDirectoryDataAsync(new FileInfo(path), progress, cancellationToken);

    /// <summary>
    /// 从网络上指定的ZIP文件获取中央目录信息（异步）。
    /// </summary>
    /// <param name="path">用于写入中央目录信息的文件的文件路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetCentralDirectoryDataAsync(string path, CancellationToken cancellationToken)
        => GetCentralDirectoryDataAsync(path, null, cancellationToken);


    /// <summary>
    /// 获取通过实体创建的ZIP文件（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="writeStream">要写入数据的流</param>
    /// <param name="progress">进度报告对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public async Task GetEntryZipFileAsync(ZipEntry entry, Stream writeStream,
        IProgress<ProgressChangedArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowException.ThrowArgumentExceptionIfZipEntryNotIsFile(entry);

        writeStream.Seek(0, SeekOrigin.Begin);
        var (compressedDataStart, nameData) =
            await LocateEntryAsync(entry, writeStream, cancellationToken).ConfigureAwait(false);
        var compressedDataEnd = compressedDataStart + entry.CompressedSize;

        if ((entry.Flags & (int)GeneralBitFlags.Descriptor) != 0)
        {
            if (entry.LocalHeaderRequiresZip64) compressedDataEnd += 24;
            else compressedDataEnd += 16;
        }

        var dataFileSize = compressedDataEnd - entry.Offset;

        if (writeStream.Length < dataFileSize)
        {
            var httpPartialDownloadStream = await httpPartialDownloadStreamBuilder(entry.Offset, compressedDataEnd)
                .ConfigureAwait(false);
            await using var _ = httpPartialDownloadStream.ConfigureAwait(false);

            var rateLimitStream = GetRateLimitStream(httpPartialDownloadStream);

            var startLength = httpPartialDownloadStream.Position = writeStream.Length;

            IProgress<long>? innerProgress = null;
            if (progress != null) innerProgress = new Progress<long>(bytes =>
            {
                ProgressReport(Math.Min(startLength + bytes -
                        ZipConstants.LocalHeaderBaseSize - compressedDataStart + entry.Offset, entry.CompressedSize),
                    entry.CompressedSize);
            });

            await rateLimitStream.CopyToAsync(writeStream, innerProgress, cancellationToken)
                .ConfigureAwait(false);
        }

        var newEntry = (ZipEntry)entry.Clone();
        newEntry.Offset = 0;
        writeStream.Seek(dataFileSize, SeekOrigin.Begin);
        var centralDirectorySize = WriteCentralDirectoryHeader(writeStream, newEntry, nameData);
        ZipFormat.WriteEndOfCentralDirectory(writeStream, 1,
            centralDirectorySize, dataFileSize,
            newEntry.Comment == null ? null : _stringCodec.ZipArchiveCommentEncoding.GetBytes(newEntry.Comment));

        return;

        void ProgressReport(long downloadBytesCompleted, long? downloadBytesCount = null)
        {
            progress.Report(new ProgressChangedArgs(downloadBytesCompleted, downloadBytesCount));
        }
    }

    /// <summary>
    /// 获取通过实体创建的ZIP文件（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="writeStream">要写入数据的流</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetEntryZipFileAsync(ZipEntry entry, Stream writeStream,
        CancellationToken cancellationToken) =>
        GetEntryZipFileAsync(entry, writeStream, null, cancellationToken);

    /// <summary>
    /// 获取通过实体创建的ZIP文件。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="writeStream">要写入数据的流</param>
    /// <param name="progress">进度报告对象</param>
    public void GetEntryZipFile(ZipEntry entry, Stream writeStream,
        IProgress<ProgressChangedArgs>? progress = null) =>
        GetEntryZipFileAsync(entry, writeStream, progress).GetAwaiter().GetResult();

    /// <summary>
    /// 获取通过实体创建的ZIP文件（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="fileInfo">要写入的文件的文件信息</param>
    /// <param name="progress">进度报告对象</param>
    public void GetEntryZipFile(ZipEntry entry, FileInfo fileInfo,
        IProgress<ProgressChangedArgs>? progress = null)
    {
        ThrowException.ThrowArgumentExceptionIfZipEntryNotIsFile(entry);
        using var fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);
        GetEntryZipFile(entry, fileStream, progress);
    }

    /// <summary>
    /// 获取通过实体创建的ZIP文件（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="fileInfo">要写入的文件的文件信息</param>
    /// <param name="progress">进度报告对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public async Task GetEntryZipFileAsync(ZipEntry entry, FileInfo fileInfo,
        IProgress<ProgressChangedArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowException.ThrowArgumentExceptionIfZipEntryNotIsFile(entry);
        try
        {
            var fileStream = fileInfo.Open(FileMode.OpenOrCreate, FileAccess.Write);
            await using var _ = fileStream.ConfigureAwait(false);
            await GetEntryZipFileAsync(entry, fileStream, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            if (fileInfo is { Exists: true, Length: 0 }) fileInfo.Delete();
            throw;
        }
    }

    /// <summary>
    /// 获取通过实体创建的ZIP文件（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="fileInfo">要写入的文件的文件信息</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetEntryZipFileAsync(ZipEntry entry, FileInfo fileInfo, CancellationToken cancellationToken) =>
        GetEntryZipFileAsync(entry, fileInfo, null, cancellationToken);

    /// <summary>
    /// 获取通过实体创建的ZIP文件。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="path">要写入的文件的路径</param>
    /// <param name="progress">进度报告对象</param>
    public void GetEntryZipFile(ZipEntry entry, string path,
        IProgress<ProgressChangedArgs>? progress = null)
    {
        ThrowException.ThrowArgumentExceptionIfZipEntryNotIsFile(entry);
        GetEntryZipFile(entry, new FileInfo(path), progress);
    }

    /// <summary>
    /// 获取通过实体创建的ZIP文件（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="path">要写入的文件的路径</param>
    /// <param name="progress">进度报告对象</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetEntryZipFileAsync(ZipEntry entry, string path,
        IProgress<ProgressChangedArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowException.ThrowArgumentExceptionIfZipEntryNotIsFile(entry);
        return GetEntryZipFileAsync(entry, new FileInfo(path), progress, cancellationToken);
    }

    /// <summary>
    /// 获取通过实体创建的ZIP文件（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例</param>
    /// <param name="path">要写入的文件的路径</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务。</returns>
    public Task GetEntryZipFileAsync(ZipEntry entry, string path, CancellationToken cancellationToken) =>
        GetEntryZipFileAsync(entry, path, null, cancellationToken);


    /// <summary>
    /// 获取读取解压文件的只读流（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例，要获取的文件的ZIP实体（必须为文件类型实体）。</param>
    /// <param name="progress">进度报告</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取解压文件的只读流。</returns>
    public async Task<Stream> GetInputStreamAsync(ZipEntry entry,
        IProgress<ProgressChangedArgs>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowException.ThrowArgumentExceptionIfZipEntryNotIsFile(entry);

        var compressedDataStart =
            (await LocateEntryAsync(entry, null, cancellationToken).ConfigureAwait(false)).Item1;
        var compressedDataEnd = compressedDataStart + entry.CompressedSize;

        var httpPartialDownloadStream = await httpPartialDownloadStreamBuilder(compressedDataStart, compressedDataEnd)
            .ConfigureAwait(false);

        var rateLimitStream = GetRateLimitStream(httpPartialDownloadStream);

        try
        {
            var progressReportReadStream = new ProgressReportReadStream(rateLimitStream,
                new Progress<long>(count =>
                {
                    progress?.Report(new ProgressChangedArgs(count, httpPartialDownloadStream.Length));
                }));

            switch (entry.CompressionMethod)
            {
                case CompressionMethod.Stored:
                    return progressReportReadStream;
                case CompressionMethod.Deflated:
                    // No need to worry about ownership and closing as underlying stream close does nothing.
                    return new InflaterInputStream(progressReportReadStream, new Inflater(true));
                case CompressionMethod.BZip2:
                    return new BZip2InputStream(progressReportReadStream);
                case CompressionMethod.Deflate64:
                case CompressionMethod.LZMA:
                case CompressionMethod.PPMd:
                case CompressionMethod.WinZipAES:
                default:
                    FeatureNotSupportedException.ThrowByReason("Unsupported compression method " +
                                                               entry.CompressionMethod);
                    return null!;
            }
        }
        catch
        {
            await httpPartialDownloadStream.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// 获取读取解压文件的只读流（异步）。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例，要获取的文件的ZIP实体（必须为文件类型实体）。</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>一个任务，可以获取解压文件的只读流。</returns>
    public Task<Stream> GetInputStreamAsync(ZipEntry entry,
        CancellationToken cancellationToken) =>
        GetInputStreamAsync(entry, null, cancellationToken);

    /// <summary>
    /// 获取读取解压文件的只读流。
    /// </summary>
    /// <param name="entry"><see cref="ZipEntry"/>的实例，要获取的文件的ZIP实体（必须为文件类型实体）。</param>
    /// <returns>解压文件的只读流</returns>
    public Stream GetInputStream(ZipEntry entry)
        => GetInputStreamAsync(entry).GetAwaiter().GetResult();

    /// <summary>
    /// Locate the data for a given entry.
    /// </summary>
    /// <returns>
    /// The start offset of the data.
    /// </returns>
    /// <exception cref="System.IO.EndOfStreamException">
    /// The stream ends prematurely
    /// </exception>
    /// <exception cref="ICSharpCode.SharpZipLib.Zip.ZipException">
    /// The local header signature is invalid, the entry and central header file name lengths are different
    /// or the local and entry compression methods dont match
    /// </exception>
    private async Task<(long, byte[])> LocateEntryAsync(ZipEntry entry, Stream? writeStream = null,
        CancellationToken cancellationToken = default)
    {
        Stream stream;
        var httpPartialDownloadStream = await httpPartialDownloadStreamBuilder(entry.Offset,
            entry.Offset + ZipConstants.LocalHeaderBaseSize).ConfigureAwait(false);
        await using var _ = httpPartialDownloadStream.ConfigureAwait(false);

        var rateLimitStream = GetRateLimitStream(httpPartialDownloadStream);

        if (writeStream != null)
        {
            stream = new MemoryStream();
            stream.SetLength(httpPartialDownloadStream.Length);
            await rateLimitStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
            stream.Seek(0, SeekOrigin.Begin);
        }
        else stream = rateLimitStream;

        var dataOffset = await TestLocalHeaderAsync(stream, async (startBytes, endBytes) =>
        {
            await httpPartialDownloadStream.ResetRangeAsync(startBytes, endBytes, cancellationToken)
                .ConfigureAwait(false);
            if (writeStream != null)
            {
                stream.SetLength(stream.Length + httpPartialDownloadStream.Length);
                stream.Seek(-httpPartialDownloadStream.Length, SeekOrigin.End);
                await rateLimitStream.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
                stream.Seek(-httpPartialDownloadStream.Length, SeekOrigin.End);
            }
        }, entry, cancellationToken).ConfigureAwait(false);

        if (writeStream != null)
        {
            stream.Seek(0, SeekOrigin.Begin);
            await stream.CopyToAsync(writeStream, cancellationToken).ConfigureAwait(false);
        }
        return dataOffset;
    }

    /// <summary>
    /// Test a local header against that provided from the central directory
    /// </summary>
    /// <param name="stream">The stream from which data needs to be read</param>
    /// <param name="resetRangeCallbackAsync"></param>
    /// <param name="entry">The entry to test against</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The offset of the entries data in the file</returns>
    private async Task<(long, byte[])> TestLocalHeaderAsync(Stream stream,
        Func<long?, long?, Task> resetRangeCallbackAsync, ZipEntry entry, CancellationToken cancellationToken = default)
    {
        var signature = stream.ReadInt();

        if (signature != ZipConstants.LocalHeaderSignature)
            ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                $"Wrong local header signature at 0x{entry.Offset:x}, expected 0x{ZipConstants.LocalHeaderSignature:x8}, actual 0x{signature:x8}");

        var extractVersion = (short)(stream.ReadUshort() & 0x00ff);
        var localFlags = (GeneralBitFlags)stream.ReadUshort();
        var compressionMethod = (CompressionMethod)stream.ReadUshort();
        var fileTime = stream.ReadShort();
        var fileDate = stream.ReadShort();
        var crcValue = stream.ReadUint();
        long compressedSize = stream.ReadUint();
        long size = stream.ReadUint();
        int storedNameLength = stream.ReadUshort();
        int extraDataLength = stream.ReadUshort();
        var extraLength = storedNameLength + extraDataLength;
        await resetRangeCallbackAsync(entry.Offset + ZipConstants.LocalHeaderBaseSize,
            entry.Offset + ZipConstants.LocalHeaderBaseSize + extraLength).ConfigureAwait(false);

        var nameData = new byte[storedNameLength];
        await stream.ReadExactlyAsync(nameData, cancellationToken).ConfigureAwait(false);

        var extraData = new byte[extraDataLength];
        await stream.ReadExactlyAsync(extraData, cancellationToken).ConfigureAwait(false);

        var localExtraData = new ZipExtraData(extraData);

        // Extra data / zip64 checks
        if (localExtraData.Find(headerID: 1))
        {
            // 2010-03-04 Forum 10512: removed checks for version >= ZipConstants.VersionZip64
            // and size or compressedSize = MaxValue, due to rogue creators.
            size = localExtraData.ReadLong();
            compressedSize = localExtraData.ReadLong();

            if (localFlags.HasAny(GeneralBitFlags.Descriptor))
            {
                // These may be valid if patched later
                if (size != 0 && size != entry.Size)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Size invalid for descriptor");
                if (compressedSize != 0 && compressedSize != entry.CompressedSize)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Compressed size invalid for descriptor");
            }
        }
        else
        {
            // No zip64 extra data but entry requires it.
            if (extractVersion >= ZipConstants.VersionZip64 &&
                ((uint)size == uint.MaxValue || (uint)compressedSize == uint.MaxValue))
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    "Required Zip64 extended information missing");
        }

        if (!SkipLocalEntryTestsOnLocate)
        {
            if (entry.IsFile)
            {
                if (!entry.IsCompressionMethodSupported())
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Compression method not supported");

                if (extractVersion is > ZipConstants.VersionMadeBy or > 20 and < ZipConstants.VersionZip64)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        $"Version required to extract this entry not supported ({extractVersion})");

                const GeneralBitFlags notSupportedFlags = GeneralBitFlags.Patched
                                                          | GeneralBitFlags.StrongEncryption
                                                          | GeneralBitFlags.EnhancedCompress
                                                          | GeneralBitFlags.HeaderMasked
                                                          | GeneralBitFlags.Encrypted;
                if (localFlags.HasAny(notSupportedFlags))
                    FeatureNotSupportedException.ThrowByReason(
                        $"The library does not support the zip features required to extract this entry ({localFlags & notSupportedFlags:F})");
            }

            if (extractVersion <= 63 && // Ignore later versions as we dont know about them..
                extractVersion != 10 &&
                extractVersion != 11 &&
                extractVersion != 20 &&
                extractVersion != 21 &&
                extractVersion != 25 &&
                extractVersion != 27 &&
                extractVersion != 45 &&
                extractVersion != 46 &&
                extractVersion != 50 &&
                extractVersion != 51 &&
                extractVersion != 52 &&
                extractVersion != 61 &&
                extractVersion != 62 &&
                extractVersion != 63
               )
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    $"Version required to extract this entry is invalid ({extractVersion})");

            var localEncoding = _stringCodec.ZipInputEncoding(localFlags);

            // Local entry flags dont have reserved bit set on.
            if (localFlags.HasAny(GeneralBitFlags.ReservedPKware4 | GeneralBitFlags.ReservedPkware14 |
                                  GeneralBitFlags.ReservedPkware15))
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    "Reserved bit flags cannot be set.");

            // Encryption requires extract version >= 20
            if (localFlags.HasAny(GeneralBitFlags.Encrypted) && extractVersion < 20)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    $"Version required to extract this entry is too low for encryption ({extractVersion})");

            // Strong encryption requires encryption flag to be set and extract version >= 50.
            if (localFlags.HasAny(GeneralBitFlags.StrongEncryption))
            {
                if (!localFlags.HasAny(GeneralBitFlags.Encrypted))
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Strong encryption flag set but encryption flag is not set");

                if (extractVersion < 50)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        $"Version required to extract this entry is too low for encryption ({extractVersion})");
            }

            // Patched entries require extract version >= 27
            if (localFlags.HasAny(GeneralBitFlags.Patched) && extractVersion < 27)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    $"Patched data requires higher version than ({extractVersion})");

            // Central header flags match local entry flags.
            if ((int)localFlags != entry.Flags)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    $"Central header/local header flags mismatch ({(GeneralBitFlags)entry.Flags:F} vs {localFlags:F})");

            // Central header compression method matches local entry
            var compressionMethodForHeader =
                entry.AESKeySize > 0 ? CompressionMethod.WinZipAES : entry.CompressionMethod;
            if (compressionMethodForHeader != compressionMethod)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    $"Central header/local header compression method mismatch ({compressionMethodForHeader:G} vs {compressionMethod:G})");

            //if (entry.Version != extractVersion)
            //    throw new ZipException("Extract version mismatch");

            // Strong encryption and extract version match
            if (localFlags.HasAny(GeneralBitFlags.StrongEncryption))
            {
                if (extractVersion < 62)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Strong encryption flag set but version not high enough");
            }

            if (localFlags.HasAny(GeneralBitFlags.HeaderMasked))
            {
                if (fileTime != 0 || fileDate != 0)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Header masked set but date/time values non-zero");
            }

            if (!localFlags.HasAny(GeneralBitFlags.Descriptor))
            {
                if (crcValue != (uint)entry.Crc)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Central header/local header crc mismatch");
            }

            // Crc valid for empty entry.
            // This will also apply to streamed entries where size isn't known and the header cant be patched
            if (size == 0 && compressedSize == 0)
            {
                if (crcValue != 0)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Invalid CRC for empty entry");
            }

            // TODO: make test more correct...  can't compare lengths as was done originally as this can fail for MBCS strings
            // Assuming a code page at this point is not valid?  Best is to store the name length in the ZipEntry probably
            if (entry.Name.Length > storedNameLength)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    "File name length mismatch");

            // Name data has already been read convert it and compare.
            var localName = localEncoding.GetString(nameData);

            // Central directory and local entry name match
            if (localName != entry.Name)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    "Central header and local header file name mismatch");

            // Directories have zero actual size but can have compressed size
            if (entry.IsDirectory)
            {
                if (size > 0)
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Directory cannot have size");

                // There may be other cases where the compressed size can be greater than this?
                // If so until details are known we will be strict.
                if (entry.IsCrypted)
                    FeatureNotSupportedException.ThrowByReason("The library does not support the directory crypted");
                if (compressedSize > 2)
                    // When not compressed the directory size can validly be 2 bytes
                    // if the true size wasn't known when data was originally being written.
                    // NOTE: Versions of the library 0.85.4 and earlier always added 2 bytes
                    ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                        "Directory compressed size invalid");
            }

            if (!ZipNameTransform.IsValidName(localName, true))
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    "Name is invalid");
        }

        // Tests that apply to both data and header.

        // Size can be verified only if it is known in the local header.
        // it will always be known in the central header.
        if (!localFlags.HasAny(GeneralBitFlags.Descriptor) ||
            ((size > 0 || compressedSize > 0) && entry.Size > 0))
        {
            if (size != 0 && size != entry.Size)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    $"Size mismatch between central header ({entry.Size}) and local header ({size})");

            if (compressedSize != 0
                && compressedSize != entry.CompressedSize && compressedSize != 0xFFFFFFFF && compressedSize != -1)
                ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                    $"Compressed size mismatch between central header({entry.CompressedSize}) and local header({compressedSize})");
        }
        return (entry.Offset + ZipConstants.LocalHeaderBaseSize + extraLength, nameData);
    }

    /// <summary>
    /// 写入中央目录头
    /// </summary>
    /// <param name="stream">要写入数据的流。</param>
    /// <param name="entry"><see cref="ZipEntry"/>的实例。</param>
    /// <param name="nameData">名称数据</param>
    /// <remarks>为防止重新编码导致名称数据不一致，此处引用未经解码的名称数据。</remarks>
    /// <returns>写入的总字节数。</returns>
    private static int WriteCentralDirectoryHeader(Stream stream, ZipEntry entry, byte[] nameData)
    {
        if (entry.CompressedSize < 0)
            ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                "Attempt to write central directory entry with unknown csize");
        if (entry.Size < 0)
            ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                "Attempt to write central directory entry with unknown size");
        if (entry.Crc < 0)
            ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
                "Attempt to write central directory entry with unknown crc");

        // Write the central file header
        stream.WriteNumber((uint)ZipConstants.CentralHeaderSignature);
        // Version made by
        stream.WriteNumber((ushort)((entry.HostSystem << 8) | entry.VersionMadeBy));
        // Version required to extract
        stream.WriteNumber((ushort)entry.Version);
        stream.WriteNumber((ushort)entry.Flags);
        unchecked
        {
            stream.WriteNumber((ushort)
                (entry.AESKeySize > 0 ? CompressionMethod.WinZipAES : entry.CompressionMethod));
            stream.WriteNumber((uint)entry.DosTime);
            stream.WriteNumber((uint)entry.Crc);
        }

        var useExtraCompressedSize = false; //Do we want to store the compressed size in the extra data?
        if (entry.IsZip64Forced() || entry.CompressedSize >= 0xffffffff)
        {
            useExtraCompressedSize = true;
            stream.WriteNumber(-1);
        }
        else stream.WriteNumber((uint)(entry.CompressedSize & 0xffffffff));

        var useExtraUncompressedSize = false; //Do we want to store the uncompressed size in the extra data?
        if (entry.IsZip64Forced() || entry.Size >= 0xffffffff)
        {
            useExtraUncompressedSize = true;
            stream.WriteNumber(-1);
        }
        else stream.WriteNumber((uint)entry.Size);

        //var entryEncoding = _stringCodec.ZipInputEncoding(entry.Flags);
        //var name = entryEncoding.GetBytes(entry.Name);

        if (nameData.Length > 0xFFFF) ZipFileTestFailedException.ThrowByZipEntryNameAndReason(entry.Name,
            "Entry name is too long.");
        stream.WriteNumber((ushort)nameData.Length);

        // Central header extra data is different to local header version so regenerate.
        var ed = new ZipExtraData(entry.ExtraData);
        if (entry.CentralHeaderRequiresZip64)
        {
            ed.StartNewEntry();
            if (useExtraUncompressedSize) ed.AddLeLong(entry.Size);
            if (useExtraCompressedSize) ed.AddLeLong(entry.CompressedSize);
            if (entry.Offset >= 0xffffffff) ed.AddLeLong(entry.Offset);
            // Number of disk on which this file starts isnt supported and is never written here.
            ed.AddNewEntry(1);
        }
        else ed.Delete(1); // Should have already be done when local header was added.

        var centralExtraData = ed.GetEntryData();

        stream.WriteNumber((ushort)centralExtraData.Length);
        stream.WriteNumber((ushort)(entry.Comment?.Length ?? 0));

        stream.WriteNumber((short)0); // disk number
        stream.WriteNumber((short)0); // internal file attributes

        // External file attributes...
        if (entry.ExternalFileAttributes != -1)
            stream.WriteNumber((uint)entry.ExternalFileAttributes);
        else
        {
            stream.WriteNumber(entry.IsDirectory ? 16U : 0U);
        }

        if (entry.Offset >= 0xffffffff) stream.WriteNumber(0xffffffffU);
        else stream.WriteNumber((uint)(int)entry.Offset);

        if (nameData.Length > 0)
            stream.WriteLittleEndianBytes(nameData);
        if (centralExtraData.Length > 0)
            stream.WriteLittleEndianBytes(centralExtraData);

        var rawComment = entry.Comment != null ? Encoding.ASCII.GetBytes(entry.Comment) : [];
        if (rawComment.Length > 0) stream.WriteLittleEndianBytes(rawComment);

        return ZipConstants.CentralHeaderBaseSize + nameData.Length + centralExtraData.Length + rawComment.Length;
    }

    private Stream GetRateLimitStream(Stream stream)
        => downloadBytesRateLimiterOptionBuilder == null ?
            stream : new RateLimitReadStream(stream, downloadBytesRateLimiterOptionBuilder);
}