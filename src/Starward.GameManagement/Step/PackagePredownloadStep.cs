using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;

namespace Starward.GameManagement.Step;

internal class PackagePredownloadStep : PackageStepBase
{

    private readonly ILogger<PackagePredownloadStep> _logger;

    public PackagePredownloadStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PackagePredownloadStep>>();
    }


    private GamePackage _package;

    private List<GamePackageFile> _packageFiles;


    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameConfig.DefaultDownloadMode is not DownloadMode.DOWNLOAD_MODE_FILE)
        {
            _logger.LogWarning("Not support download mode DOWNLOAD_MODE_FILE.");
            throw new NotSupportedException($"Game ({_context.GameId.Id}, {_context.GameId.GameBiz}) not support download mode DOWNLOAD_MODE_FILE.");
        }
        _package ??= (await _hoYoPlayService.GetGamePackageAsync(_context.GameId, cancellation)).EnsureNotNull(_context.GameId);
        if (_package.PreDownload is null)
        {
            _logger.LogWarning("Pre-download package resource is null.");
            throw new NotSupportedException($"Pre-download package resource of game ({_context.GameId.Id}, {_context.GameId.GameBiz}) is null.");
        }
        string? localVersion = _context.ConfigIni.GameVersion?.ToString();
        GamePackageResource resource = _package.PreDownload.Patches.FirstOrDefault(x => x.Version == localVersion) ?? _package.PreDownload.Major!;
        (var game, var audio) = GameResourceService.GetDownloadPackageFiles(resource, _context.AudioLanguage);
        _packageFiles = [.. game, .. audio];
        _logger.LogInformation("Pre-download package resource version: {version}, file count: {count}.", resource.Version, _packageFiles.Count);
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        _context.ClearProgress();
        _context.Step = GameInstallStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;
        _context.DownloadTotalBytes = _packageFiles.Sum(x => x.Size);
        _logger.LogInformation("Start downloading {count} package files, total: {totalSize} bytes.", _packageFiles.Count, _context.DownloadTotalBytes);

        await _polly.ForEachAsync(_packageFiles, cancellation, async (item, token) =>
        {
            string path = Path.Combine(_context.InstallPath, Helper.GetUrlFileName(item.Url));
            await DownloadFileAndVerifyAsync(path, item.Url, item.Size, item.MD5, token);
        });
    }



    protected override Task PostAsync(CancellationToken cancellation = default)
    {
        string localVersion = _context.ConfigIni.GameVersion?.ToString() ?? "";
        string? newVersion = _package.PreDownload.Major?.Version;
        _context.ConfigIni.Settings["predownload"] = $"{localVersion},{newVersion},{_context.AudioLanguage}";
        _logger.LogInformation("Set predownload info: {value}.", _context.ConfigIni.Settings["predownload"]);
        return Task.CompletedTask;
    }


}
