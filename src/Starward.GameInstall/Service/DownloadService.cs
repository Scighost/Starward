using Microsoft.Extensions.Logging;
using System.Buffers;
using System.IO.Compression;
using System.IO.Pipelines;
using System.Net.Http.Headers;

namespace Starward.GameInstall.Service;


internal class DownloadService
{

    public const int BUFFER_SIZE = 8192;


    private readonly ILogger<DownloadService> _logger;

    private readonly HttpService _httpService;



    public DownloadService(ILogger<DownloadService> logger, HttpService httpService)
    {
        _logger = logger;
        _httpService = httpService;
    }




    /// <summary>
    /// 下载文件到指定路径，支持断点续传，不含 MD5 校验
    /// </summary>
    public async Task DownloadToFileAsync(string path, string url, long size, GameInstallContext? context = null, CancellationToken cancellation = default)
    {
        long progressBytes = 0;
        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
        using FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        if (fs.Length >= size)
        {
            fs.SetLength(size);
            context?.AddNetworkBytes(size);
            context?.AddStorageBytes(size);
            progressBytes = size;
        }
        else if (fs.Length < size)
        {
            try
            {
                RangeHeaderValue? range = null;
                if (fs.Length > 0)
                {
                    range = new RangeHeaderValue(fs.Length, null);
                }
                using HttpResponseMessage response = await _httpService.GetAsync(url, range, cancellation).ConfigureAwait(false);
                if (response.StatusCode is System.Net.HttpStatusCode.PartialContent)
                {
                    fs.Position = response.Content.Headers.ContentRange?.From ?? fs.Length;
                    context?.AddNetworkBytes(fs.Position);
                    context?.AddStorageBytes(fs.Position);
                    progressBytes += fs.Position;
                }
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
                try
                {
                    int read;
                    using Stream hs = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
                    while ((read = await hs.ReadAsync(buffer, cancellation).ConfigureAwait(false)) > 0)
                    {
                        await RateLimiter.AcquireAsync(read, cancellation).ConfigureAwait(false);
                        await fs.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                        context?.AddDownloadBytes(read);
                        context?.AddNetworkBytes(read);
                        context?.AddStorageBytes(read);
                        progressBytes += read;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch
            {
                context?.AddNetworkBytes(-progressBytes);
                context?.AddStorageBytes(-progressBytes);
                throw;
            }
        }
    }



    /// <summary>
    /// 下载文件到指定路径，支持断点续传，不含 MD5 校验
    /// </summary>
    public async Task DownloadRangeToFileAsync(string path, string url, long offset, long size, GameInstallContext? context = null, CancellationToken cancellation = default)
    {
        long progressBytes = 0;
        string? folder = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
        using FileStream fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
        if (fs.Length >= size)
        {
            fs.SetLength(size);
            context?.AddNetworkBytes(size);
            context?.AddStorageBytes(size);
            progressBytes = size;
        }
        else if (fs.Length < size)
        {
            try
            {
                RangeHeaderValue range = new(offset + fs.Length, offset + size - 1);
                using HttpResponseMessage response = await _httpService.GetAsync(url, range, cancellation).ConfigureAwait(false);
                if (response.StatusCode is System.Net.HttpStatusCode.PartialContent)
                {
                    fs.Position = response.Content.Headers.ContentRange?.From ?? fs.Length;
                    context?.AddNetworkBytes(fs.Position);
                    context?.AddStorageBytes(fs.Position);
                    progressBytes += fs.Position;
                }
                byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
                try
                {
                    int read;
                    using Stream hs = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
                    while ((read = await hs.ReadAsync(buffer, cancellation).ConfigureAwait(false)) > 0)
                    {
                        await RateLimiter.AcquireAsync(read, cancellation).ConfigureAwait(false);
                        await fs.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                        context?.AddDownloadBytes(read);
                        context?.AddNetworkBytes(read);
                        context?.AddStorageBytes(read);
                        progressBytes += read;
                    }
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch
            {
                context?.AddNetworkBytes(-progressBytes);
                context?.AddStorageBytes(-progressBytes);
                throw;
            }
        }
    }



    /// <summary>
    /// 下载到流
    /// </summary>
    public async Task DownloadToStreamAsync(string url, Stream targetStream, GameInstallContext? context = null, CancellationToken cancellation = default)
    {
        long progressBytes = 0;
        using HttpResponseMessage response = await _httpService.GetAsync(url, cancellation).ConfigureAwait(false);
        using Stream hs = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
        try
        {
            int read;
            while ((read = await hs.ReadAsync(buffer, cancellation).ConfigureAwait(false)) > 0)
            {
                await RateLimiter.AcquireAsync(read, cancellation).ConfigureAwait(false);
                await targetStream.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                context?.AddDownloadBytes(read);
                context?.AddNetworkBytes(read);
                context?.AddStorageBytes(read);
                progressBytes += read;
            }
            await targetStream.FlushAsync(cancellation).ConfigureAwait(false);
        }
        catch
        {
            context?.AddNetworkBytes(-progressBytes);
            context?.AddStorageBytes(-progressBytes);
            throw;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }



    /// <summary>
    /// 下载并 zstd 解压到流
    /// </summary>
    public async Task DownloadAndDecompressZstdToStreamAsync(string url, Stream targetStream, GameInstallContext? context = null, CancellationToken cancellation = default)
    {
        long networkBytes = 0, storageBytes = 0;
        using HttpResponseMessage response = await _httpService.GetAsync(url, cancellation).ConfigureAwait(false);
        using Stream hs = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
        Pipe pipe = new();
        using ZstandardStream ds = new(pipe.Reader.AsStream(), CompressionMode.Decompress);

        async Task ProduceAsync()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
            try
            {
                int read;
                while ((read = await hs.ReadAsync(buffer, cancellation).ConfigureAwait(false)) > 0)
                {
                    await RateLimiter.AcquireAsync(read, cancellation).ConfigureAwait(false);
                    await pipe.Writer.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                    context?.AddDownloadBytes(read);
                    context?.AddNetworkBytes(read);
                    networkBytes += read;
                }
                await pipe.Writer.FlushAsync(cancellation).ConfigureAwait(false);
                await pipe.Writer.CompleteAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pipe.Writer.CompleteAsync(ex).ConfigureAwait(false);
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        async Task ConsumeAsync()
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(BUFFER_SIZE);
            try
            {
                int read;
                while ((read = await ds.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellation).ConfigureAwait(false)) > 0)
                {
                    await targetStream.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                    context?.AddStorageBytes(read);
                    storageBytes += read;
                }
                await targetStream.FlushAsync(cancellation).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                await pipe.Reader.CompleteAsync(ex).ConfigureAwait(false);
                throw;
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        try
        {
            await Task.WhenAll(ProduceAsync(), ConsumeAsync()).ConfigureAwait(false);
        }
        catch
        {
            context?.AddNetworkBytes(-networkBytes);
            context?.AddStorageBytes(-storageBytes);
            throw;
        }
    }


}


