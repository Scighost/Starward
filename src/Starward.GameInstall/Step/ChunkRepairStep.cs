using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Model;
using Starward.GameInstall.Service;
using Starward.GameInstall.Sophon;

namespace Starward.GameInstall.Step;

internal class ChunkRepairStep : ChunkStepBase
{

    private readonly ILogger<ChunkRepairStep> _logger;

    public ChunkRepairStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ChunkRepairStep>>();
    }


    private GameBranch _branch;

    private List<SophonChunkItem> _chunkItems;

    private List<SophonChunkItem> _repairItems;



    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameConfig.DefaultDownloadMode is not DownloadMode.DOWNLOAD_MODE_CHUNK)
        {
            _logger.LogWarning("Not support download mode DOWNLOAD_MODE_CHUNK.");
            throw new NotSupportedException($"Game ({_context.GameId.Id}, {_context.GameId.GameBiz}) not support download mode DOWNLOAD_MODE_CHUNK.");
        }
        _branch ??= (await _hoYoPlayService.GetGameBranchAsync(_context.GameId, cancellation).ConfigureAwait(false)).EnsureNotNull(_context.GameId);
        _chunkItems ??= await _gameResourceService.GetSophonChunkItemsAsync(_context.GameId, _branch.Main, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, null, cancellation).ConfigureAwait(false);
    }


    protected override async Task ExecuteInternalAsync(CancellationToken cancellation)
    {
        await MoveCachedResourceAsync(cancellation).ConfigureAwait(false);

        if (_repairItems is null)
        {
            var repairItems = new List<SophonChunkItem>();

            _context.ClearProgress();
            _context.Step = RunnerStep.Verifying;
            _context.ProgressDisplay = ProgressDisplay.StorageBytes;
            _context.StorageTotalBytes = _chunkItems.Sum(x => x.ChunkFile.Size);
            _logger.LogInformation("Start verifying {count} chunk files.", _chunkItems.Count);

            Lock _lock = new();
            await _polly.ForEachAsync(_chunkItems, cancellation, async (item, token) =>
            {
                if (AddBytesIfFinished(item))
                {
                    return;
                }
                string path = Path.Combine(_context.InstallPath, item.ChunkFile.File);

                long networkSize = item.ChunkFile.Chunks.Sum(x => x.CompressedSize);
                long storageSize = item.ChunkFile.Size;
                string md5 = item.ChunkFile.Md5;
                bool failed = false;
                if (!await TryHardLinkAsync(item, path, networkSize, storageSize, md5, token).ConfigureAwait(false))
                {
                    if (await CheckFileMD5Async(item, path, networkSize, storageSize, md5, true, token).ConfigureAwait(false))
                    {
                        HardLinkService.InsertGameFileItem(_context.GameId, path, storageSize, md5);
                    }
                    else
                    {
                        failed = true;
                    }
                }
                lock (_lock)
                {
                    SophonChunkItem copy = item.Copy();
                    copy.IsFinished = !failed;
                    repairItems.Add(copy);
                }
                item.IsFinished = true;
            }).ConfigureAwait(false);
            _repairItems = repairItems.OrderByDescending(x => x.IsFinished).ToList();
            _logger.LogInformation("{itemCount} of {total} files need repair.", _repairItems.Count(x => !x.IsFinished), _chunkItems.Count);
        }

        _context.ClearProgress();
        _context.Step = RunnerStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes | ProgressDisplay.StorageBytes;
        _context.NetworkTotalBytes = _repairItems.Sum(x => x.ChunkFile.Chunks.Sum(long (SophonChunk y) => y.CompressedSize));
        _context.StorageTotalBytes = _repairItems.Sum(x => x.ChunkFile.Size);
        _logger.LogInformation("Start downloading {count} repair files, network: {networkBytes} bytes, storage: {storageBytes} bytes.", _repairItems.Count, _context.NetworkTotalBytes, _context.StorageTotalBytes);
        await _polly.ForEachAsync(_repairItems, cancellation, async (item, token) =>
        {
            if (AddBytesIfFinished(item))
            {
                return;
            }
            await DownloadSophonChunkFileAsync(item, token).ConfigureAwait(false);
            item.IsFinished = true;
        }).ConfigureAwait(false);

    }



    protected override async Task PostAsync(CancellationToken cancellation = default)
    {
        _context.ConfigIni.GameVersion = new Version(_branch.Main.Tag);
        _logger.LogInformation("Set game version to {version}.", _context.ConfigIni.GameVersion);
    }


}
