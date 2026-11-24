using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.GameInstall.Model;
using Starward.GameInstall.Sophon;
using Starward.GameInstall.Step;
using System.Text;

namespace Starward.GameInstall.HotUpdate;

internal class CutsceneManageStep : ChunkStepBase
{

    private readonly ILogger<CutsceneManageStep> _logger;



    public CutsceneManageStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<CutsceneManageStep>>();
    }



    private List<SophonChunkItem> _chunkItems;


    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameId.GameBiz.Game is not GameBiz.hk4e)
        {
            SkipStep = true;
            return;
        }
        string path = Path.Combine(_context.InstallPath, "CutsceneBlockList.jsonl");
        if (_context.CutsceneBlockList?.Count > 0)
        {
            StringBuilder sb = new();
            foreach (string item in _context.CutsceneBlockList)
            {
                sb.AppendLine($$"""{"fileName": "{{item}}"}""");
                string file = Path.Combine(_context.InstallPath, item);
                if (File.Exists(file))
                {
                    File.Delete(file);
                }
            }
            await File.WriteAllTextAsync(path, sb.ToString(), cancellation);
        }
        else if (File.Exists(path))
        {
            File.Delete(path);
        }

        var branch = (await _hoYoPlayService.GetGameBranchAsync(_context.GameId, cancellation).ConfigureAwait(false)).EnsureNotNull(_context.GameId);
        var chunkItems = await _gameResourceService.GetSophonChunkItemsAsync(_context.GameId, branch.Main, _context.InstallPath, _context.CategoryScenario, _context.AudioLanguage, null, cancellation).ConfigureAwait(false);
        var result = new List<SophonChunkItem>();
        foreach (var item in chunkItems)
        {
            if (item.ChunkFile.File.Contains("VideoAssets/StandaloneWindows64"))
            {
                if (!File.Exists(Path.Combine(_context.InstallPath, item.ChunkFile.File)))
                {
                    result.Add(item);
                }
            }
        }
        _chunkItems = result;
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        _context.ClearProgress();
        _context.NetworkTotalBytes = _chunkItems.Sum(x => x.ChunkFile.Chunks.Sum(long (SophonChunk y) => y.CompressedSize));
        _context.StorageTotalBytes = _chunkItems.Sum(x => x.ChunkFile.Size);
        _context.Step = RunnerStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes | ProgressDisplay.StorageBytes;
        _logger.LogInformation("Start downloading {count} chunk files, network: {networkBytes} bytes, storage: {storageBytes} bytes.", _chunkItems.Count, _context.NetworkTotalBytes, _context.StorageTotalBytes);

        await _polly.ForEachAsync(_chunkItems, cancellation, async (item, token) =>
        {
            await DownloadSophonChunkFileAsync(item, token).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }




}
