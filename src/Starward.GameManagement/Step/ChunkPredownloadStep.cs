using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;

namespace Starward.GameManagement.Step;

internal class ChunkPredownloadStep : ChunkStepBase
{

    private readonly ILogger<ChunkPredownloadStep> _logger;

    public ChunkPredownloadStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ChunkPredownloadStep>>();
    }


    private GameBranch _branch;

    private List<SophonPatchItem>? _patchItems;

    private List<SophonChunkItem>? _chunkItems;



    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameConfig.DefaultDownloadMode is not DownloadMode.DOWNLOAD_MODE_CHUNK)
        {
            _logger.LogWarning("Not support download mode DOWNLOAD_MODE_CHUNK.");
            throw new NotSupportedException($"Game ({_context.GameId.Id}, {_context.GameId.GameBiz}) not support download mode DOWNLOAD_MODE_CHUNK.");
        }

        string localVersion = _context.ConfigIni.GameVersion?.ToString() ?? "";
        _branch ??= (await _hoYoPlayService.GetGameBranchAsync(_context.GameId, cancellation)).EnsureNotNull(_context.GameId);
        if (_branch.PreDownload is null)
        {
            _logger.LogWarning("Pre-download resource is null.");
            throw new NotSupportedException($"Pre-download resource of game ({_context.GameId.Id}, {_context.GameId.GameBiz}) is null.");
        }

        if (_branch.PreDownload.DiffTags.Contains(localVersion))
        {
            (var patchItems, _) = await _gameResourceService.GetSophonPatchItemsAsync(_context.GameId, _branch.PreDownload, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, localVersion, cancellation);
            _patchItems = patchItems.DistinctBy(x => x.DiffFile.Id).ToList();
            _logger.LogInformation("Pre-download uses patch of version {version}, patch count: {count}.", localVersion, _patchItems.Count);
        }
        else
        {
            List<SophonChunkItem> oldChunkItems = await _gameResourceService.GetSophonChunkItemsAsync(_context.GameId, _branch.Main, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, localVersion, cancellation);
            List<SophonChunkItem> newChunkItems = await _gameResourceService.GetSophonChunkItemsAsync(_context.GameId, _branch.PreDownload, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, null, cancellation);
            HashSet<string> oldChunkIdSet = new();
            foreach (SophonChunkItem chunkItem in oldChunkItems)
            {
                foreach (SophonChunk? chunk in chunkItem.ChunkFile.Chunks)
                {
                    oldChunkIdSet.Add(chunk.Id);
                }
            }
            List<SophonChunkItem> chunkItems = new();
            foreach (SophonChunkItem chunkItem in newChunkItems)
            {
                foreach (SophonChunk? chunk in chunkItem.ChunkFile.Chunks)
                {
                    if (!oldChunkIdSet.Contains(chunk.Id))
                    {
                        chunkItems.Add(new SophonChunkItem(chunkItem.ManifestUrl, chunkItem.ChunkFile, chunk));
                    }
                }
            }
            _chunkItems = chunkItems.DistinctBy(x => x.Chunk!.Id).ToList();
            _logger.LogInformation("Pre-download uses chunk, chunk count: {count}.", _chunkItems.Count);
        }

    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation)
    {
        _context.ClearProgress();
        _context.Step = GameInstallStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;

        if (_patchItems is not null)
        {
            _context.DownloadTotalBytes = _patchItems.Sum(x => x.DiffFile.DiffFileSize);
            _logger.LogInformation("Start downloading pre-download patch files, count: {count}, network: {networkBytes} bytes.", _patchItems.Count, _context.DownloadTotalBytes);
            await _polly.ForEachAsync(_patchItems, cancellation, async (item, token) =>
            {
                await DownloadSophonDiffFileAsync(item, token);
            });
        }

        if (_chunkItems is not null)
        {
            _context.DownloadTotalBytes = _chunkItems.Sum(x => x.Chunk!.CompressedSize);
            _logger.LogInformation("Start downloading pre-download chunk files, count: {count}, network: {networkBytes} bytes.", _chunkItems.Count, _context.DownloadTotalBytes);
            await _polly.ForEachAsync(_chunkItems, cancellation, async (item, token) =>
            {
                await DownloadSophonChunkAsync(item, token);
            });
        }

    }



    protected override async Task PostAsync(CancellationToken cancellation = default)
    {
        string localVersion = _context.ConfigIni.GameVersion?.ToString() ?? "";
        string? newVersion = _branch.PreDownload?.Tag;
        _context.ConfigIni.Settings["predownload"] = $"{localVersion},{newVersion},{_context.AudioLanguage}";
    }


}
