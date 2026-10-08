using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;

namespace Starward.GameManagement.Step;

internal class PackageInstallStep : PackageStepBase
{

    private readonly ILogger<PackageInstallStep> _logger;

    public PackageInstallStep(GameInstallContext context, IServiceProvider serviceProvider)
        : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PackageInstallStep>>();
    }


    private GamePackage _package;


    private List<GamePackageFile> _gamePackages;

    private List<GamePackageFile> _audioPackages;

    private List<GamePackageFile> _packageFiles;


    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameConfig.DefaultDownloadMode is not DownloadMode.DOWNLOAD_MODE_FILE)
        {
            _logger.LogWarning("Not support download mode DOWNLOAD_MODE_FILE.");
            throw new NotSupportedException($"Game ({_context.GameId.Id}, {_context.GameId.GameBiz}) not support download mode DOWNLOAD_MODE_FILE.");
        }
        _package ??= (await _hoYoPlayService.GetGamePackageAsync(_context.GameId, cancellation)).EnsureNotNull(_context.GameId);
        if (_package.Main.Major is null)
        {
            _logger.LogWarning("Main major package resource is null.");
            throw new NotSupportedException($"Main major package resource of game ({_context.GameId.Id}, {_context.GameId.GameBiz}) is null.");
        }
        (_gamePackages, _audioPackages) = GameResourceService.GetDownloadPackageFiles(_package.Main.Major, _context.AudioLanguage);
        _packageFiles = [.. _gamePackages, .. _audioPackages];
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        long totalSize = _packageFiles.Sum(x => x.Size);
        _context.ClearProgress();
        _context.Step = GameInstallStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;
        _context.DownloadTotalBytes = totalSize;
        _logger.LogInformation("Start downloading {count} package files, total: {totalSize} bytes.", _packageFiles.Count, totalSize);

        await _polly.ForEachAsync(_packageFiles, cancellation, async (item, token) =>
        {
            string path = Path.Combine(_context.InstallPath, Helper.GetUrlFileName(item.Url));
            await DownloadFileAndVerifyAsync(path, item.Url, item.Size, item.MD5, token);
        });

        _context.ClearProgress();
        _context.Step = GameInstallStep.Decompressing;
        _context.ProgressDisplay = ProgressDisplay.Progress;
        double ratio = _gamePackages.Sum(x => x.Size) / (double)totalSize;
        await ExtractPackagesAsync(_gamePackages, ratio, cancellation);

        foreach (GamePackageFile package in _audioPackages)
        {
            ratio = package.Size / (double)totalSize;
            await ExtractPackagesAsync([package], ratio, cancellation);
        }

        _context.ClearProgress();
        _context.Step = GameInstallStep.Cleaning;
        DeletePackageFiles(_packageFiles);
    }


    protected override Task PostAsync(CancellationToken cancellation = default)
    {
        if (_package.Main.Major is not null && Version.TryParse(_package.Main.Major.Version, out Version? version))
        {
            _context.ConfigIni.GameVersion = version;
            _logger.LogInformation("Set game version to {version}.", version);
        }
        return Task.CompletedTask;
    }


}
