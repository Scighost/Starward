using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Model;
using Starward.GameInstall.Sophon;

namespace Starward.GameInstall.Step;

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
        _branch ??= (await _hoYoPlayService.GetGameBranchAsync(_context.GameId, cancellation).ConfigureAwait(false)).EnsureNotNull(_context.GameId);
        if (_branch.PreDownload is null)
        {
            _logger.LogWarning("Pre-download resource is null.");
            throw new NotSupportedException($"Pre-download resource of game ({_context.GameId.Id}, {_context.GameId.GameBiz}) is null.");
        }

        if (_branch.PreDownload.DiffTags.Contains(localVersion))
        {
            (var patchItems, _) = await _gameResourceService.GetSophonPatchItemsAsync(_context.GameId, _branch.PreDownload, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, localVersion, cancellation).ConfigureAwait(false);
            _patchItems = patchItems.DistinctBy(x => x.DiffFile.Id).ToList();
            _logger.LogInformation("Pre-download uses patch of version {version}, patch count: {count}.", localVersion, _patchItems.Count);
        }
        else
        {
            _chunkItems = await _gameResourceService.GetPredownloadChunkItemsAsync(_context.GameId, _branch, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, localVersion, cancellation).ConfigureAwait(false);
            _logger.LogInformation("Pre-download uses chunk, chunk count: {count}.", _chunkItems.Count);
        }

    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation)
    {
        _context.ClearProgress();
        _context.Step = RunnerStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;

        if (_patchItems is not null)
        {
            _context.NetworkTotalBytes = _patchItems.Sum(x => x.DiffFile.DiffFileSize);
            _logger.LogInformation("Start downloading pre-download patch files, count: {count}, network: {networkBytes} bytes.", _patchItems.Count, _context.NetworkTotalBytes);
            await _polly.ForEachAsync(_patchItems, cancellation, async (item, token) =>
            {
                await DownloadSophonDiffFileAsync(item, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

        if (_chunkItems is not null)
        {
            _context.NetworkTotalBytes = _chunkItems.Sum(x => x.Chunk!.CompressedSize);
            _logger.LogInformation("Start downloading pre-download chunk files, count: {count}, network: {networkBytes} bytes.", _chunkItems.Count, _context.NetworkTotalBytes);
            await _polly.ForEachAsync(_chunkItems, cancellation, async (item, token) =>
            {
                await DownloadSophonChunkAsync(item, token).ConfigureAwait(false);
            }).ConfigureAwait(false);
        }

    }



    protected override async Task PostAsync(CancellationToken cancellation = default)
    {
        int count = (_chunkItems?.Count ?? 0) + (_patchItems?.Count ?? 0);
        if (count == 0)
        {
            // 一片都没算出来就不要写「预下载已完成」标记。
            // 否则 GameLauncherService.CheckPreDownloadFinishedAsync 会判定已完成，
            // 启动页的预下载按钮直接变灰显示「已完成」（PreDownloadButton.IsButtonEnabled = !IsPredownloadFinished），
            // 用户再也点不动、无法重试。
            _logger.LogWarning("No pre-download resource planned, skip writing predownload info.");
            return;
        }
        string localVersion = _context.ConfigIni.GameVersion?.ToString() ?? "";
        string? newVersion = _branch.PreDownload?.Tag;
        _context.ConfigIni.Settings["predownload"] = $"{localVersion},{newVersion},{_context.AudioLanguage}";
    }


}
