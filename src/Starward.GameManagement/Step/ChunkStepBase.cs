using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.GameManagement.Model;
using Starward.GameManagement.Service;

namespace Starward.GameManagement.Step;

internal class ChunkStepBase : StepBase
{

    private readonly ILogger<ChunkStepBase> _logger;

    public ChunkStepBase(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ChunkStepBase>>();
    }



    public bool AddBytesIfFinished(SophonFileItem item)
    {
        if (item.IsFinished)
        {
            _context.AddNetworkBytes(item.NetworkSize);
            _context.AddStorageBytes(item.StorageSize);
            if (item.IsHardlinked)
            {
                _context.AddHardLinkBytes(item.StorageSize);
            }
            return true;
        }
        return false;
    }



    public async Task DownloadSophonChunkFileAsync(SophonChunkItem item, CancellationToken cancellation = default)
    {
        if (item.ChunkFile.IsFolder)
        {
            return;
        }
        if (AddBytesIfFinished(item))
        {
            return;
        }
        long storageSize = item.ChunkFile.Size;
        long networkSize = item.ChunkFile.Chunks.Sum(x => x.CompressedSize);
        string fullPath = Path.Combine(_context.InstallPath, item.ChunkFile.File);
        if (await TryHardLinkAsync(item, fullPath, networkSize, storageSize, item.ChunkFile.Md5, cancellation))
        {
            return;
        }
        if (await CheckFileMD5Async(item, fullPath, networkSize, storageSize, item.ChunkFile.Md5, true, cancellation))
        {
            HardLinkService.InsertGameFileItem(_context.GameId, fullPath, storageSize, item.ChunkFile.Md5);
            return;
        }

        string? folder = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string path_tmp = fullPath + ".tmp";
        long networkBytes = 0, storageBytes = 0;
        try
        {
            using FileStream fs = File.Open(path_tmp, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            foreach (var chunk in item.ChunkFile.Chunks)
            {
                fs.Position = chunk.Offset;
                if (await HashService.CheckStreamMD5Async(fs, chunk.UncompressedSize, chunk.UncompressedMd5, cancellation))
                {
                    _context.AddNetworkBytes(chunk.CompressedSize);
                    _context.AddStorageBytes(chunk.UncompressedSize);
                    networkBytes += chunk.CompressedSize;
                    storageBytes += chunk.UncompressedSize;
                }
                else
                {
                    fs.Position = chunk.Offset;
                    string url = item.ManifestUrl.BuildUrl(chunk.Id);
                    await _downloadService.DownloadAndDecompressZstdToStreamAsync(url, fs, _context, cancellation);
                    networkBytes += chunk.CompressedSize;
                    storageBytes += chunk.UncompressedSize;
                }
            }
        }
        catch
        {
            _context.AddNetworkBytes(-networkBytes);
            _context.AddStorageBytes(-storageBytes);
            throw;
        }

        if (await CheckFileMD5Async(item, path_tmp, networkSize, storageSize, item.ChunkFile.Md5, false, cancellation))
        {
            File.Move(path_tmp, fullPath, true);
            HardLinkService.InsertGameFileItem(_context.GameId, fullPath, storageSize, item.ChunkFile.Md5);
            return;
        }
        else
        {
            File.Delete(path_tmp);
            _context.AddNetworkBytes(-networkBytes);
            _context.AddStorageBytes(-storageBytes);
            _logger.LogError("File MD5 check failed, path: {path}, md5: {md5}.", fullPath, item.ChunkFile.Md5);
            throw new FileMD5FailedException(fullPath, item.ChunkFile.Md5);
        }
    }



    public async Task DownloadSophonChunkAsync(SophonChunkItem item, CancellationToken cancellation = default)
    {
        if (item.Chunk is null)
        {
            return;
        }
        if (AddBytesIfFinished(item))
        {
            return;
        }
        long size = item.Chunk.CompressedSize;
        string md5 = item.Chunk.CompressedMd5;
        string fullPath = Path.Combine(_context.InstallPath, "chunk", item.Chunk.Id);


        if (await TryHardLinkAsync(item, fullPath, size, size, md5, cancellation))
        {
            return;
        }

        if (await CheckFileMD5Async(item, fullPath, size, size, md5, true, cancellation))
        {
            HardLinkService.InsertGameFileItem(_context.GameId, fullPath, size, md5);
            return;
        }

        string? folder = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string path_tmp = fullPath + ".tmp";
        string url = item.ManifestUrl.BuildUrl(item.Chunk.Id);
        await _downloadService.DownloadToFileAsync(path_tmp, url, size, _context, cancellation);
        if (await CheckFileMD5Async(item, path_tmp, size, size, md5, false, cancellation))
        {
            File.Move(path_tmp, fullPath, true);
            HardLinkService.InsertGameFileItem(_context.GameId, fullPath, size, md5);
            return;
        }
        else
        {
            File.Delete(path_tmp);
            _context.AddNetworkBytes(-size);
            _context.AddStorageBytes(-size);
            _logger.LogError("File MD5 check failed, path: {path}, md5: {md5}.", fullPath, md5);
            throw new FileMD5FailedException(fullPath, md5);
        }
    }



    public async Task DownloadSophonDiffFileAsync(SophonPatchItem item, CancellationToken cancellation = default)
    {
        if (item.DiffFile is null)
        {
            return;
        }
        if (AddBytesIfFinished(item))
        {
            return;
        }
        long size = item.DiffFile.DiffFileSize;
        string fullPath = Path.Combine(_context.InstallPath, "ldiff", item.DiffFile.Id);

        if (await TryHardLinkAsync(item, fullPath, size, size, item.DiffFile.DiffFileMd5, cancellation))
        {
            return;
        }

        if (await CheckFileMD5Async(item, fullPath, size, size, item.DiffFile.DiffFileMd5, true, cancellation))
        {
            HardLinkService.InsertGameFileItem(_context.GameId, fullPath, size, item.DiffFile.DiffFileMd5);
            return;
        }

        string? folder = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(folder))
        {
            Directory.CreateDirectory(folder);
        }
        string path_tmp = fullPath + ".tmp";
        string url = item.ManifestUrl.BuildUrl(item.DiffFile.Id);
        await _downloadService.DownloadToFileAsync(path_tmp, url, size, _context, cancellation);
        if (await CheckFileMD5Async(item, path_tmp, size, size, item.DiffFile.DiffFileMd5, false, cancellation))
        {
            File.Move(path_tmp, fullPath, true);
            HardLinkService.InsertGameFileItem(_context.GameId, fullPath, size, item.DiffFile.DiffFileMd5);
            return;
        }
        else
        {
            File.Delete(path_tmp);
            _context.AddNetworkBytes(-size);
            _context.AddStorageBytes(-size);
            _logger.LogError("File MD5 check failed, path: {path}, md5: {md5}.", fullPath, item.DiffFile.DiffFileMd5);
            throw new FileMD5FailedException(fullPath, item.DiffFile.DiffFileMd5);
        }
    }



    protected async Task<bool> TryHardLinkAsync(SophonFileItem item, string path, long networkSize, long storageSize, string md5, CancellationToken cancellation = default)
    {
        if (await HardLinkService.TryHardLinkAsync(_context.GameId, path, storageSize, md5, cancellation))
        {
            _context.AddNetworkBytes(networkSize);
            _context.AddStorageBytes(storageSize);
            _context.AddHardLinkBytes(storageSize);
            item.NetworkSize = networkSize;
            item.StorageSize = storageSize;
            item.IsFinished = true;
            item.IsHardlinked = true;
            return true;
        }
        return false;
    }



    protected async Task<bool> CheckFileMD5Async(SophonFileItem item, string path, long networkSize, long storageSize, string md5, bool addContextBytes, CancellationToken cancellation = default)
    {
        if (await HashService.CheckFileMD5Async(path, storageSize, md5, cancellation))
        {
            if (addContextBytes)
            {
                _context.AddNetworkBytes(networkSize);
                _context.AddStorageBytes(storageSize);
            }
            item.NetworkSize = networkSize;
            item.StorageSize = storageSize;
            item.IsFinished = true;
            return true;
        }
        return false;
    }


}