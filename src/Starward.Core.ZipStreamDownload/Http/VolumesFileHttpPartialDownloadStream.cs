using System.Collections.Concurrent;
using System.Net.Http.Headers;

namespace Starward.Core.ZipStreamDownload.Http;

internal class VolumesFileHttpPartialDownloadStream : HttpPartialDownloadStream
{
    public class SingleFileInit(Uri fileUri, long? fileLength = null, DateTimeOffset? fileLastModifiedTime = null)
    {

        public Uri FileUri { get; } = fileUri;

        public long? FileLength { get; } = fileLength;

        public DateTimeOffset? FileLastModifiedTime { get; } = fileLastModifiedTime;
    }

    public class SingleFile(SingleFileHttpPartialDownloadStream singleDownloadStream)
    {

        public Uri FileUri { get; } = singleDownloadStream.FileUri;

        long? FileLength { get; } = singleDownloadStream.FileLength;

        private DateTimeOffset? FileLastModifiedTime { get; } = singleDownloadStream.FileLastModifiedTime;

        public HttpContentHeaders HttpContentHeaders { get; } = singleDownloadStream.HttpContentHeaders;

        public HttpResponseHeaders HttpResponseHeaders { get; } = singleDownloadStream.HttpResponseHeaders;

        public HttpResponseHeaders HttpTrailingHeaders { get; } = singleDownloadStream.HttpTrailingHeaders;

        public Version HttpVersion { get; } = singleDownloadStream.HttpVersion;

        public HttpRequestMessage? HttpRequestMessage { get; } = singleDownloadStream.HttpRequestMessage;
    }

    public IReadOnlyCollection<SingleFile> Files { get; }

    private readonly SingleFileHttpPartialDownloadStream[] _singleDownloadStreamArray;

    private SingleFileHttpPartialDownloadStream[] _singleDownloadStreamStartToEndArray = null!;

    private VolumesFileHttpPartialDownloadStream(
        SingleFileHttpPartialDownloadStream[] singleDownloadStreamArray,
        long? startBytes, long? endBytes)
    {
        _singleDownloadStreamArray = singleDownloadStreamArray;
        Files = singleDownloadStreamArray.Select(s => new SingleFile(s)).ToList();
        FileLength = singleDownloadStreamArray.Sum(s => s.FileLength);

        (StartBytes, EndBytes) = ValidateAndGetStartBytesAndEndBytes(startBytes, endBytes);
    }

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<SingleFileInit> files, long? startBytes, long? endBytes, string? mediaType)
    {
        var singleDownloadStreamArray = files.AsParallel()
            .WithDegreeOfParallelism(10)
            .Select(f => SingleFileHttpPartialDownloadStream
                .GetInstance(httpClient, f.FileUri, 0, 1, mediaType, f.FileLength, f.FileLastModifiedTime))
            .AsOrdered()
            .ToArray();
        var instance = new VolumesFileHttpPartialDownloadStream(singleDownloadStreamArray, startBytes, endBytes);
        instance._singleDownloadStreamStartToEndArray =
            instance.SeekFilesAndGetStreamSlice(instance.StartBytes, instance.EndBytes).ToArray();
        return instance;
    }

    public static async Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<SingleFileInit> files, long? startBytes, long? endBytes, string? mediaType,
        CancellationToken cancellationToken = default)
    {
        var fileArray = files.ToArray();
        var streamWithIndex =
            new ConcurrentQueue<(int, SingleFileHttpPartialDownloadStream)>();
        await Parallel.ForAsync(0, fileArray.Length, new ParallelOptions
        {
            CancellationToken = cancellationToken,
            MaxDegreeOfParallelism = 10
        }, async (index, token) =>
        {
            var file = fileArray[index];
            var stream = await SingleFileHttpPartialDownloadStream
                .GetInstanceAsync(httpClient, file.FileUri, 0, 1, mediaType, file.FileLength,
                    file.FileLastModifiedTime, token).ConfigureAwait(false);
            streamWithIndex.Enqueue((index, stream));
        }).ConfigureAwait(false);
        var singleDownloadStreamList = streamWithIndex
            .OrderBy(s => s.Item1)
            .Select(s => s.Item2)
            .ToArray();
        var instance = new VolumesFileHttpPartialDownloadStream(singleDownloadStreamList, startBytes, endBytes);
        instance._singleDownloadStreamStartToEndArray = (await instance.SeekFilesAndGetStreamSliceAsync
                (instance.StartBytes, instance.EndBytes, cancellationToken).ConfigureAwait(false)).ToArray();
        return instance;
    }

    protected override bool ResetRangeCore(long? startBytes = null, long? endBytes = null)
    {
        var (newStartBytes, newEndBytes) = ValidateAndGetStartBytesAndEndBytes(startBytes, endBytes);
        if (StartBytes == newStartBytes && EndBytes == newEndBytes) return false;
        _singleDownloadStreamStartToEndArray = SeekFilesAndGetStreamSlice(newStartBytes, newEndBytes).ToArray();
        StartBytes = newStartBytes;
        EndBytes = newEndBytes;
        return true;
    }

    protected override async Task<bool> ResetRangeAsyncCore(long? startBytes = null, long? endBytes = null,
        CancellationToken cancellationToken = default)
    {
        var (newStartBytes, newEndBytes) = ValidateAndGetStartBytesAndEndBytes(startBytes, endBytes);
        if (StartBytes == newStartBytes && EndBytes == newEndBytes) return false;
        _singleDownloadStreamStartToEndArray = (await SeekFilesAndGetStreamSliceAsync(newStartBytes, newEndBytes,
                cancellationToken).ConfigureAwait(false)).ToArray();
        StartBytes = newStartBytes;
        EndBytes = newEndBytes;
        return true;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        ThrowIfThisIsDisposed();
        ValidateBufferArguments(buffer, offset, count);
        SeekActually();
        var needCount = GetReadCount(count);
        var readCount = 0;
        long endPosition = 0;
        foreach (var stream in _singleDownloadStreamStartToEndArray)
        {
            var startPosition = endPosition;
            endPosition += stream.Length;
            if (Position < startPosition || Position >= endPosition) continue;
            var singleCount = stream.Read(buffer, readCount, needCount - readCount);
            AddPositionActually(singleCount);
            if ((readCount += singleCount) == needCount) break;
        }
        return readCount;
    }

    public override int Read(Span<byte> buffer)
    {
        ThrowIfThisIsDisposed();
        SeekActually();
        var needCount = GetReadCount(buffer.Length);
        var readCount = 0;
        long endPosition = 0;
        foreach (var stream in _singleDownloadStreamStartToEndArray)
        {
            var startPosition = endPosition;
            endPosition += stream.Length;
            if (Position < startPosition || Position >= endPosition) continue;
            var singleCount = stream.Read(buffer.Slice(readCount, needCount - readCount));
            AddPositionActually(singleCount);
            if ((readCount += singleCount) == needCount) break;
        }
        return readCount;
    }

    public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
    {
        ThrowIfThisIsDisposed();
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);
        var needCount = GetReadCount(buffer.Length);
        var readCount = 0;
        long endPosition = 0;
        foreach (var stream in _singleDownloadStreamStartToEndArray)
        {
            var startPosition = endPosition;
            endPosition += stream.Length;
            if (Position < startPosition || Position >= endPosition) continue;
            var singleCount = await stream.ReadAsync(buffer.Slice(readCount, needCount - readCount),
                cancellationToken).ConfigureAwait(false);
            AddPositionActually(singleCount);
            if ((readCount += singleCount) == needCount) break;
        }
        return readCount;
    }

    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ThrowIfThisIsDisposed();
        ValidateBufferArguments(buffer, offset, count);
        await SeekActuallyAsync(cancellationToken).ConfigureAwait(false);
        var needCount = GetReadCount(buffer.Length);
        var readCount = 0;
        long endPosition = 0;
        foreach (var stream in _singleDownloadStreamStartToEndArray)
        {
            var startPosition = endPosition;
            endPosition += stream.Length;
            if (Position < startPosition || Position >= endPosition) continue;
            var singleCount = await stream.ReadAsync(buffer, readCount, needCount - readCount, cancellationToken)
                .ConfigureAwait(false);
            AddPositionActually(singleCount);
            if ((readCount += singleCount) == needCount) break;
        }
        return readCount;
    }

    protected override void Dispose(bool disposing)
    {
        SetDisposed();
        if (!disposing) return;
        Parallel.ForEach(_singleDownloadStreamArray, new ParallelOptions
        {
            MaxDegreeOfParallelism = 10
        }, stream => stream.Dispose());
    }

    public override async ValueTask DisposeAsync()
    {
        SetDisposed();
        await Parallel.ForEachAsync(_singleDownloadStreamArray, new ParallelOptions
            {
                MaxDegreeOfParallelism = 10
            }, async (stream, _) => await stream.DisposeAsync().ConfigureAwait(false))
            .ConfigureAwait(false);
    }

    protected override void SeekActuallyCore(long fakePosition)
    {
        long endPosition = 0;
        foreach (var stream in _singleDownloadStreamStartToEndArray)
        {
            var startPosition = endPosition;
            endPosition += stream.Length;
            if (fakePosition < startPosition || fakePosition >= endPosition) continue;
            stream.Position = fakePosition - startPosition;
            stream.Flush();
        }
    }

    protected override async Task SeekActuallyAsyncCore(long fakePosition,
        CancellationToken cancellationToken = default)
    {
        long endPosition = 0;
        foreach (var stream in _singleDownloadStreamStartToEndArray)
        {
            var startPosition = endPosition;
            endPosition += stream.Length;
            if (fakePosition < startPosition || fakePosition >= endPosition) continue;
            stream.Position = fakePosition - startPosition;
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    private Span<SingleFileHttpPartialDownloadStream> SeekFilesAndGetStreamSlice(long startBytes, long endBytes)
    {
        long endPosition = 0;
        int firstFileIndex = -1, lastFileIndex = -1;
        for (var index = 0; index < _singleDownloadStreamArray.Length; index++)
        {
            var singleDownloadStream = _singleDownloadStreamArray[index];
            var startPosition = endPosition;
            endPosition += singleDownloadStream.FileLength;

            if (startBytes >= startPosition && endBytes <= endPosition)
            {
                singleDownloadStream.ResetRange(startBytes - startPosition, endBytes - startPosition);
                firstFileIndex = index;
                lastFileIndex = index;
                break;
            }
            if (endBytes >= startPosition && endBytes <= endPosition)
            {
                singleDownloadStream.ResetRange(0, endBytes - startPosition);
                lastFileIndex = index;
                break;
            }
            if (startBytes >= startPosition && startBytes <= endPosition)
            {
                singleDownloadStream.ResetRange(startBytes - startPosition);
                firstFileIndex = index;
            }
            else if (startBytes < startPosition && endBytes > endPosition)
            {
                singleDownloadStream.ResetRange();
            }
        }
        return _singleDownloadStreamArray.AsSpan()[firstFileIndex..(lastFileIndex + 1)];
    }

    private async Task<Memory<SingleFileHttpPartialDownloadStream>> SeekFilesAndGetStreamSliceAsync(
        long startBytes, long endBytes, CancellationToken cancellationToken = default)
    {
        long endPosition = 0;
        int firstFileIndex = -1, lastFileIndex = -1;
        for (var index = 0; index < _singleDownloadStreamArray.Length; index++)
        {
            var singleDownloadStream = _singleDownloadStreamArray[index];
            var startPosition = endPosition;
            endPosition += singleDownloadStream.FileLength;

            if (startBytes >= startPosition && endBytes <= endPosition)
            {
                await singleDownloadStream.ResetRangeAsync(startBytes - startPosition, endBytes - startPosition,
                    cancellationToken).ConfigureAwait(false);
                firstFileIndex = index;
                lastFileIndex = index;
                break;
            }
            if (endBytes > startPosition && endBytes <= endPosition)
            {
                await singleDownloadStream.ResetRangeAsync(0, endBytes - startPosition, cancellationToken)
                    .ConfigureAwait(false);
                lastFileIndex = index;
                break;
            }
            if (startBytes >= startPosition && startBytes < endPosition)
            {
                await singleDownloadStream.ResetRangeAsync(startBytes - startPosition,
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                firstFileIndex = index;
            }
            else if (startBytes < startPosition && endBytes > endPosition)
            {
                await singleDownloadStream.ResetRangeAsync(cancellationToken: cancellationToken).ConfigureAwait(false);
            }
        }
        return _singleDownloadStreamArray.AsMemory()[firstFileIndex..(lastFileIndex + 1)];
    }

    #region 重载方法

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<SingleFileInit> files, long? startBytes, long? endBytes)
        => GetInstance(httpClient, files, startBytes, endBytes, null);

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<SingleFileInit> files, string? mediaType)
        => GetInstance(httpClient, files, null, null, mediaType);

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<SingleFileInit> files)
        => GetInstance(httpClient, files, null, null, null);

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<Uri> fileUris, long? startBytes, long? endBytes, string? mediaType)
        => GetInstance(httpClient, fileUris.Select(f => new SingleFileInit(f)), startBytes, endBytes, mediaType);

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<Uri> fileUris, long? startBytes, long? endBytes)
        => GetInstance(httpClient, fileUris, startBytes, endBytes, null);

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<Uri> fileUris, string? mediaType)
        => GetInstance(httpClient, fileUris, null, null, mediaType);

    public static VolumesFileHttpPartialDownloadStream GetInstance(HttpClient httpClient,
        IEnumerable<Uri> fileUris)
        => GetInstance(httpClient, fileUris, null, null, null);



    public static Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<SingleFileInit> files, long? startBytes, long? endBytes,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, files, startBytes, endBytes, null, cancellationToken);

    public static Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<SingleFileInit> files, string? mediaType, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, files, null, null, mediaType, cancellationToken);

    public static Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<SingleFileInit> files, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, files, null, null, null, cancellationToken);

    public static Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<Uri> fileUris, long? startBytes, long? endBytes, string? mediaType,
        CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUris.Select(f => new SingleFileInit(f)), startBytes, endBytes, mediaType,
            cancellationToken);

    public static Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<Uri> fileUris, long? startBytes, long? endBytes, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUris, startBytes, endBytes, null, cancellationToken);

    public static Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<Uri> fileUris, string? mediaType, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUris, null, null, mediaType, cancellationToken);

    public static Task<VolumesFileHttpPartialDownloadStream> GetInstanceAsync(HttpClient httpClient,
        IEnumerable<Uri> fileUris, CancellationToken cancellationToken = default)
        => GetInstanceAsync(httpClient, fileUris, null, null, null, cancellationToken);

    #endregion
}