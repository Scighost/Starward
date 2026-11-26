using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Model;
using Starward.GameInstall.Service;
using System.Text.Json;

namespace Starward.GameInstall.Step;

internal class PackageUpdateStep : PackageStepBase
{

    private readonly ILogger<PackageUpdateStep> _logger;

    public PackageUpdateStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PackageUpdateStep>>();
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
        _package ??= (await _hoYoPlayService.GetGamePackageAsync(_context.GameId, cancellation).ConfigureAwait(false)).EnsureNotNull(_context.GameId);
        if (_package.Main is null)
        {
            _logger.LogWarning("Main package resource is null.");
            throw new NotSupportedException($"Main package resource of game ({_context.GameId.Id}, {_context.GameId.GameBiz}) is null.");
        }
        string? localVersion = _context.ConfigIni.GameVersion?.ToString();
        GamePackageResource resource = _package.Main.Patches.FirstOrDefault(x => x.Version == localVersion) ?? _package.Main.Major!;
        (_gamePackages, _audioPackages) = GameResourceService.GetDownloadPackageFiles(resource, _context.AudioLanguage);
        _packageFiles = [.. _gamePackages, .. _audioPackages];
        _logger.LogInformation("Update package resource version: {version}, file count: {count}.", resource.Version, _packageFiles.Count);
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        await MoveCachedResourceAsync(cancellation).ConfigureAwait(false);

        long totalSize = _packageFiles.Sum(x => x.Size);

        _context.ClearProgress();
        _context.Step = RunnerStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;
        _context.NetworkTotalBytes = totalSize;
        _logger.LogInformation("Start downloading {count} package files, total: {totalSize} bytes.", _packageFiles.Count, totalSize);

        await _polly.ForEachAsync(_packageFiles, cancellation, async (item, token) =>
        {
            string path = Path.Combine(_context.InstallPath, Helper.GetUrlFileName(item.Url));
            await DownloadFileAndVerifyAsync(path, item.Url, item.Size, item.MD5, token).ConfigureAwait(false);
        }).ConfigureAwait(false);

        _context.ClearProgress();
        _context.Step = RunnerStep.Patching;
        _context.ProgressDisplay = ProgressDisplay.Progress;
        double ratio = _gamePackages.Sum(x => x.Size) / (double)totalSize / 2;
        await ExtractPackagesAsync(_gamePackages, ratio, cancellation).ConfigureAwait(false);
        await PatchDiffFilesAsync(ratio, cancellation).ConfigureAwait(false);

        foreach (GamePackageFile package in _audioPackages)
        {
            ratio = package.Size / (double)totalSize / 2;
            await ExtractPackagesAsync([package], ratio, cancellation).ConfigureAwait(false);
            await PatchDiffFilesAsync(ratio, cancellation).ConfigureAwait(false);
        }

        _context.ClearProgress();
        _context.Step = RunnerStep.Cleaning;
        _context.ProgressDisplay = ProgressDisplay.None;
        DeletePackageFiles(_packageFiles);
    }



    protected override Task PostAsync(CancellationToken cancellation = default)
    {
        if (_package.Main.Major is not null && Version.TryParse(_package.Main.Major.Version, out Version? version))
        {
            _context.ConfigIni.GameVersion = version;
            _logger.LogInformation("Set game version to {version}.", version);
        }
        _context.ConfigIni.Settings.Remove("predownload");
        return Task.CompletedTask;
    }


}
