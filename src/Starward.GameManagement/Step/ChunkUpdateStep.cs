using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Snap.HPatch;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;
using Starward.GameManagement.Service;
using ZstdSharp;

namespace Starward.GameManagement.Step;


internal class ChunkUpdateStep : ChunkStepBase
{

    private readonly ILogger<ChunkUpdateStep> _logger;

    public ChunkUpdateStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ChunkUpdateStep>>();
    }


    private GameBranch _branch;

    private List<SophonPatchItem>? _patchItems;

    private List<SophonChunkItem>? _chunkItems;

    private Dictionary<string, SophonChunkItem>? _oldFileDict;

    private Dictionary<string, SophonChunkItem>? _oldChunkDict;

    private List<string>? _deleteFiles;



    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameConfig.DefaultDownloadMode is not DownloadMode.DOWNLOAD_MODE_CHUNK)
        {
            _logger.LogWarning("Not support download mode DOWNLOAD_MODE_CHUNK.");
            throw new NotSupportedException($"Game ({_context.GameId.Id}, {_context.GameId.GameBiz}) not support download mode DOWNLOAD_MODE_CHUNK.");
        }

        string localVersion = _context.ConfigIni.GameVersion?.ToString() ?? "";
        _branch ??= (await _hoYoPlayService.GetGameBranchAsync(_context.GameId, cancellation)).EnsureNotNull(_context.GameId);

        if (_branch.Main.DiffTags.Contains(localVersion))
        {
            (_patchItems, _deleteFiles) = await _gameResourceService.GetSophonPatchItemsAsync(_context.GameId, _branch.Main, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, localVersion, cancellation);
            _logger.LogInformation("Chunk update uses patch of version {version}, patch count: {count}, delete count: {deleteCount}.", localVersion, _patchItems.Count, _deleteFiles.Count);
        }
        else
        {
            List<SophonChunkItem> oldChunkItems = await _gameResourceService.GetSophonChunkItemsAsync(_context.GameId, _branch.Main, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, localVersion, cancellation);
            Dictionary<string, SophonChunkItem> oldFileDict = new();
            Dictionary<string, SophonChunkItem> oldChunkDict = new();
            foreach (SophonChunkItem chunkItem in oldChunkItems)
            {
                oldFileDict.TryAdd(chunkItem.ChunkFile.File, chunkItem);
                foreach (SophonChunk? chunk in chunkItem.ChunkFile.Chunks)
                {
                    oldChunkDict.TryAdd(chunk.Id, new SophonChunkItem(chunkItem.ManifestUrl, chunkItem.ChunkFile, chunk));
                }
            }
            _oldFileDict = oldFileDict;
            _oldChunkDict = oldChunkDict;
            var chunkItems = await _gameResourceService.GetSophonChunkItemsAsync(_context.GameId, _branch.Main, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, null, cancellation);
            _chunkItems = new();
            foreach (SophonChunkItem item in chunkItems)
            {
                if (!(_oldFileDict?.ContainsKey(item.ChunkFile.File) ?? false))
                {
                    _chunkItems.Add(item);
                }
            }
            _deleteFiles = oldChunkItems.Select(x => x.ChunkFile.File).Except(chunkItems.Select(x => x.ChunkFile.File)).ToList();
            _logger.LogInformation("Chunk update uses chunk, new file count: {count}, delete count: {deleteCount}.", _chunkItems.Count, _deleteFiles.Count);
        }
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        await MoveCachedResourceAsync(cancellation);

        if (_patchItems is not null)
        {
            await PatchUpdateAsync(cancellation);
        }
        if (_chunkItems is not null)
        {
            await ChunkUpdateAsync(cancellation);
        }
    }



    private async Task PatchUpdateAsync(CancellationToken cancellation = default)
    {
        if (_patchItems is null)
        {
            return;
        }

        _context.ClearProgress();
        _context.Step = GameInstallStep.Patching;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes | ProgressDisplay.StorageBytes;
        _context.DownloadTotalBytes = _patchItems.Sum(x => x.DiffFile.DiffLength);
        _context.WriteTotalBytes = _patchItems.Sum(x => x.PatchFile.Size);
        _logger.LogInformation("Start patching {count} files, network: {networkBytes} bytes, storage: {storageBytes} bytes.", _patchItems.Count, _context.DownloadTotalBytes, _context.WriteTotalBytes);

        await _polly.ForEachAsync(_patchItems, cancellation, async (item, token) =>
        {
            if (AddBytesIfFinished(item))
            {
                return;
            }
            string path_target = Path.Join(_context.InstallPath, item.PatchFile.File);
            long networkSize = item.DiffFile.DiffLength, storageSize = item.PatchFile.Size;
            string md5 = item.PatchFile.Md5;
            if (await TryHardLinkAsync(item, path_target, networkSize, storageSize, md5, cancellation))
            {
                if (!string.IsNullOrWhiteSpace(item.DiffFile.OriginalFileName) && item.PatchFile.File != item.DiffFile.OriginalFileName)
                {
                    string path_original = Path.Combine(_context.InstallPath, item.DiffFile.OriginalFileName);
                    if (File.Exists(path_original))
                    {
                        File.Delete(path_original);
                    }
                    HardLinkService.DeleteGameFileItem(path_original);
                }
                return;
            }
            if (await CheckFileMD5Async(item, path_target, networkSize, storageSize, md5, true, cancellation))
            {
                HardLinkService.InsertGameFileItem(_context.GameId, path_target, storageSize, md5);
                return;
            }

            string? path_source = null;
            if (!string.IsNullOrWhiteSpace(item.DiffFile.OriginalFileName))
            {
                path_source = Path.Join(_context.InstallPath, item.DiffFile.OriginalFileName);
                if (!await HashService.CheckFileMD5Async(path_source, item.DiffFile.OriginalFileSize, item.DiffFile.OriginalFileMd5, cancellation))
                {
                    // 原文件错误，跳过，后续由修复步骤兜底
                    if (item.PatchFile.File != item.DiffFile.OriginalFileName)
                    {
                        string path_original = Path.Combine(_context.InstallPath, item.DiffFile.OriginalFileName);
                        if (File.Exists(path_original))
                        {
                            File.Delete(path_original);
                        }
                        HardLinkService.DeleteGameFileItem(path_original);
                    }
                    _context.AddNetworkBytes(networkSize);
                    _context.AddStorageBytes(storageSize);
                    item.NetworkSize = networkSize;
                    item.StorageSize = storageSize;
                    item.IsFinished = true;
                    return;
                }
            }

            string path_diff = Path.Join(_context.InstallPath, "ldiff", item.DiffFile.Id);
            string? path_diff_offset = null;
            if (!await HashService.CheckFileMD5Async(path_diff, item.DiffFile.DiffFileSize, item.DiffFile.DiffFileMd5, cancellation))
            {
                path_diff_offset = Path.Join(_context.InstallPath, "ldiff", $"{item.DiffFile.Id}_{item.DiffFile.DiffOffset}");
                string url = item.ManifestUrl.BuildUrl(item.DiffFile.Id);
                await _polly.ExecuteAsync(async token => await _downloadService.DownloadRangeToFileAsync(path_diff_offset, url, item.DiffFile.DiffOffset, item.DiffFile.DiffLength, _context, token), cancellation);
            }

            CreateDirectoryForFile(path_target);
            string path_target_temp = path_target + ".tmp";
            {
                using Stream fs_diff = path_diff_offset is null ? new FileSliceStream(path_diff, item.DiffFile.DiffOffset, item.DiffFile.DiffLength) : File.OpenRead(path_diff_offset);
                using var fs_target_temp = File.Create(path_target_temp);
                if (path_source is null)
                {
                    if (item.ManifestUrl.Compression is 0)
                    {
                        await fs_diff.CopyToAsync(fs_target_temp, cancellation);
                    }
                    else
                    {
                        using var ds = new DecompressionStream(fs_diff);
                        await ds.CopyToAsync(fs_target_temp, cancellation);
                    }
                }
                else
                {
                    using FileStream fs_source = File.OpenRead(path_source);
                    HPatch.PatchZstandard(fs_source, fs_diff, fs_target_temp);
                }
            }

            if (File.Exists(path_source))
            {
                File.Delete(path_source);
            }
            if (await HashService.CheckFileMD5Async(path_target_temp, item.PatchFile.Size, item.PatchFile.Md5, cancellation))
            {
                File.Move(path_target_temp, path_target, true);
                HardLinkService.InsertGameFileItem(_context.GameId, path_target, item.PatchFile.Size, item.PatchFile.Md5);
            }
            else
            {
                File.Delete(path_target_temp);
                _logger.LogWarning("Patch file MD5 check failed, path: {path}.", path_target);
            }
            item.NetworkSize = networkSize;
            item.StorageSize = storageSize;
            item.IsFinished = true;
        });

    }



    private async Task ChunkUpdateAsync(CancellationToken cancellation = default)
    {
        if (_chunkItems is null)
        {
            return;
        }

        _context.ClearProgress();
        _context.Step = GameInstallStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes | ProgressDisplay.StorageBytes;

        long totalNetworkSize = 0, totalStorageSize = 0;
        foreach (SophonChunkItem item in _chunkItems)
        {
            totalStorageSize += item.ChunkFile.Size;
            foreach (SophonChunk? chunk in item.ChunkFile.Chunks)
            {
                if (!(_oldChunkDict?.ContainsKey(chunk.Id) ?? false))
                {
                    totalNetworkSize += chunk.CompressedSize;
                }
            }
        }
        _context.DownloadTotalBytes = totalNetworkSize;
        _context.WriteTotalBytes = totalStorageSize;
        _logger.LogInformation("Start chunk update download, file count: {count}, network: {networkBytes} bytes, storage: {storageBytes} bytes.", _chunkItems.Count, totalNetworkSize, totalStorageSize);

        await _polly.ForEachAsync(_chunkItems, cancellation, async (item, token) =>
        {
            if (AddBytesIfFinished(item))
            {
                return;
            }

            string fullPath = Path.Join(_context.InstallPath, item.ChunkFile.File);
            long networkSize = 0, storageSize = item.ChunkFile.Size;
            foreach (var chunk in item.ChunkFile.Chunks)
            {
                if (!(_oldChunkDict?.ContainsKey(chunk.Id) ?? false))
                {
                    networkSize += chunk.CompressedSize;
                }
            }
            string md5 = item.ChunkFile.Md5;

            if (await TryHardLinkAsync(item, fullPath, networkSize, storageSize, md5, token))
            {
                return;
            }

            if (await CheckFileMD5Async(item, fullPath, networkSize, storageSize, md5, true, token))
            {
                HardLinkService.InsertGameFileItem(_context.GameId, fullPath, storageSize, md5);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            string path_tmp = fullPath + ".tmp";

            long networkBytes = 0, storageBytes = 0;
            try
            {
                using FileStream fs = File.Open(path_tmp, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
                foreach (SophonChunk? chunk in item.ChunkFile.Chunks)
                {
                    fs.Position = chunk.Offset;
                    if (await HashService.CheckStreamMD5Async(fs, chunk.UncompressedSize, chunk.UncompressedMd5, token))
                    {
                        _context.AddNetworkBytes(chunk.CompressedSize);
                        _context.AddStorageBytes(chunk.UncompressedSize);
                        networkBytes += chunk.CompressedSize;
                        storageBytes += chunk.UncompressedSize;
                    }
                    else
                    {
                        fs.Position = chunk.Offset;
                        if (_oldChunkDict?.TryGetValue(chunk.Id, out SophonChunkItem? oldChunk) ?? false)
                        {
                            string path_source = Path.Join(_context.InstallPath, oldChunk.ChunkFile.File);
                            if (File.Exists(path_source) && oldChunk.Chunk is not null)
                            {
                                if (await HashService.CheckFileMD5Async(path_source, oldChunk.ChunkFile.Size, oldChunk.ChunkFile.Md5, token))
                                {
                                    var fs_source = new FileSliceStream(path_source, oldChunk.Chunk.Offset, oldChunk.Chunk.UncompressedSize);
                                    await fs_source.CopyToAsync(fs, token);
                                    _context.AddNetworkBytes(chunk.CompressedSize);
                                    _context.AddStorageBytes(chunk.UncompressedSize);
                                    networkBytes += chunk.CompressedSize;
                                    storageBytes += chunk.UncompressedSize;
                                    continue;
                                }
                            }
                        }
                        string path_chunk = Path.Join(_context.InstallPath, "chunk", chunk.Id);
                        if (await HashService.CheckFileMD5Async(path_chunk, chunk.CompressedSize, chunk.CompressedMd5, token))
                        {
                            using var fs_chunk = File.OpenRead(path_chunk);
                            using var ds = new DecompressionStream(fs_chunk);
                            await ds.CopyToAsync(fs, token);
                            _context.AddNetworkBytes(chunk.CompressedSize);
                            _context.AddStorageBytes(chunk.UncompressedSize);
                            networkBytes += chunk.CompressedSize;
                            storageBytes += chunk.UncompressedSize;
                            continue;
                        }
                        string url = item.ManifestUrl.BuildUrl(chunk.Id);
                        await _downloadService.DownloadAndDecompressZstdToStreamAsync(url, fs, _context, token);
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

            if (await CheckFileMD5Async(item, path_tmp, networkSize, storageSize, md5, false, token))
            {
                File.Move(path_tmp, fullPath, true);
                HardLinkService.InsertGameFileItem(_context.GameId, fullPath, storageSize, md5);
            }
            else
            {
                File.Delete(path_tmp);
                _context.AddNetworkBytes(-networkBytes);
                _context.AddStorageBytes(-storageBytes);
                throw new FileMD5FailedException(fullPath, md5);
            }
        });

    }



    protected override async Task PostAsync(CancellationToken cancellation = default)
    {
        _context.ClearProgress();
        _context.Step = GameInstallStep.Cleaning;
        try
        {
            if (_deleteFiles is not null)
            {
                if (_deleteFiles.Count > 0)
                {
                    _logger.LogInformation("Delete {count} outdated files.", _deleteFiles.Count);
                }
                foreach (string item in _deleteFiles)
                {
                    string path = Path.Join(_context.InstallPath, item);
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                    }
                    HardLinkService.DeleteGameFileItem(path);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete outdated files.");
        }
        try
        {
            string chunkFolder = Path.Join(_context.InstallPath, "chunk");
            if (Directory.Exists(chunkFolder))
            {
                Directory.Delete(chunkFolder, true);
                HardLinkService.DeleteGameFolder(chunkFolder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete chunk folder.");
        }
        try
        {
            string ldiffFolder = Path.Join(_context.InstallPath, "ldiff");
            if (Directory.Exists(ldiffFolder))
            {
                Directory.Delete(ldiffFolder, true);
                HardLinkService.DeleteGameFolder(ldiffFolder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to delete ldiff folder.");
        }
        _context.ConfigIni.Settings.Remove("predownload");
    }


}
