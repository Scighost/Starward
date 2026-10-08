using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;

namespace Starward.GameManagement.Step;

internal class ChunkInstallStep : ChunkStepBase
{

    private readonly ILogger<ChunkInstallStep> _logger;

    public ChunkInstallStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<ChunkInstallStep>>();
    }


    private GameBranch _branch;

    private List<SophonChunkItem> _chunkItems;

    private long _totalNetworkBytes;

    private long _totalStorageBytes;



    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameConfig.DefaultDownloadMode is not DownloadMode.DOWNLOAD_MODE_CHUNK)
        {
            _logger.LogWarning("Not support download mode DOWNLOAD_MODE_CHUNK.");
            throw new NotSupportedException($"Game ({_context.GameId.Id}, {_context.GameId.GameBiz}) not support download mode DOWNLOAD_MODE_CHUNK.");
        }
        _branch ??= (await _hoYoPlayService.GetGameBranchAsync(_context.GameId, cancellation)).EnsureNotNull(_context.GameId);
        _chunkItems ??= await _gameResourceService.GetSophonChunkItemsAsync(_context.GameId, _branch.Main, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, null, cancellation);
        _totalNetworkBytes = _chunkItems.Sum(x => x.ChunkFile.Chunks.Sum(long (SophonChunk y) => y.CompressedSize));
        _totalStorageBytes = _chunkItems.Sum(x => x.ChunkFile.Size);
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation)
    {
        _context.ClearProgress();
        _context.DownloadTotalBytes = _totalNetworkBytes;
        _context.WriteTotalBytes = _totalStorageBytes;
        _context.Step = GameInstallStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes | ProgressDisplay.StorageBytes;
        _logger.LogInformation("Start downloading {count} chunk files, network: {networkBytes} bytes, storage: {storageBytes} bytes.", _chunkItems.Count, _totalNetworkBytes, _totalStorageBytes);

        await _polly.ForEachAsync(_chunkItems, cancellation, async (item, token) =>
        {
            await DownloadSophonChunkFileAsync(item, token);
        });
    }


    protected override async Task PostAsync(CancellationToken cancellation = default)
    {
        _context.ConfigIni.GameVersion = new Version(_branch.Main.Tag);
        _logger.LogInformation("Set game version to {version}.", _context.ConfigIni.GameVersion);
    }


}
