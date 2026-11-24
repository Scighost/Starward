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


    private async Task PatchDiffFilesAsync(double percentRatio, CancellationToken cancellation = default)
    {
        Lock lockObj = new();

        string hdifffiles = Path.Combine(_context.InstallPath, "hdifffiles.txt");
        if (File.Exists(hdifffiles))
        {
            using FileStream fs = File.OpenRead(hdifffiles);
            List<HDiffFile?> hdiffFiles = await JsonSerializer.DeserializeAsyncEnumerable<HDiffFile>(fs, true, null, cancellation).ToListAsync(cancellation).ConfigureAwait(false);
            if (hdiffFiles.Count > 0)
            {
                _logger.LogInformation("Patch {count} files by hdifffiles.txt.", hdiffFiles.Count);
                double increase = percentRatio / hdiffFiles.Count;
                await Parallel.ForEachAsync(hdiffFiles, cancellation, async (item, token) =>
                {
                    if (item is null)
                    {
                        return;
                    }
                    string target = Path.GetFullPath(Path.Join(_context.InstallPath, item.RemoteName));
                    string diff = target + ".hdiff";
                    try
                    {
                        await PatchFileAsync(target, diff, target, token).ConfigureAwait(false);
                        if (File.Exists(diff))
                        {
                            File.Delete(diff);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Patch file failed, path: {path}.", target);
                    }
                    lock (lockObj)
                    {
                        _context.PercentProgress += increase;
                    }
                }).ConfigureAwait(false);
            }
            File.Delete(hdifffiles);
        }

        string hdiffmap = Path.Combine(_context.InstallPath, "hdiffmap.json");
        if (File.Exists(hdiffmap))
        {
            string content = await File.ReadAllTextAsync(hdiffmap, cancellation).ConfigureAwait(false);
            DiffMap? diffMap = JsonSerializer.Deserialize<DiffMap>(content);
            if (diffMap?.DiffMapItems is { Count: > 0 } diffMapItems)
            {
                _logger.LogInformation("Patch {count} files by hdiffmap.json.", diffMapItems.Count);
                double increase = percentRatio / diffMapItems.Count;
                await Parallel.ForEachAsync(diffMapItems, cancellation, async (item, token) =>
                {
                    string source = Path.GetFullPath(Path.Join(_context.InstallPath, item.SourceFileName));
                    string target = Path.GetFullPath(Path.Join(_context.InstallPath, item.TargetFileName));
                    string diff = Path.GetFullPath(Path.Join(_context.InstallPath, item.PatchFileName));
                    try
                    {
                        await PatchFileAsync(source, diff, target, token).ConfigureAwait(false);
                        if (File.Exists(diff))
                        {
                            File.Delete(diff);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Patch file failed, target: {target}.", target);
                    }
                    lock (lockObj)
                    {
                        _context.PercentProgress += increase;
                    }
                }).ConfigureAwait(false);
            }
            File.Delete(hdiffmap);
        }

        string delete = Path.Combine(_context.InstallPath, "deletefiles.txt");
        if (File.Exists(delete))
        {
            string[] lines = await File.ReadAllLinesAsync(delete, cancellation).ConfigureAwait(false);
            _logger.LogInformation("Delete {count} files by deletefiles.txt.", lines.Count(x => !string.IsNullOrWhiteSpace(x)));
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                string target = Path.Combine(_context.InstallPath, line);
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
                HardLinkService.DeleteGameFileItem(target);
            }
            File.Delete(delete);
        }
    }


    private async Task PatchFileAsync(string source, string diff, string target, CancellationToken cancellation)
    {
        if (!File.Exists(source) || !File.Exists(diff))
        {
            _logger.LogWarning("Patch file not found, source: {source}, diff: {diff}.", source, diff);
            return;
        }
        string tmp = target + ".tmp";
        try
        {
            using FileStream fs_source = File.OpenRead(source);
            using FileStream fs_diff = File.OpenRead(diff);
            using FileStream fs_target = File.Create(tmp);
            await HPatch.HPatch.PatchAsync(fs_source, fs_diff, fs_target, cancellationToken: cancellation);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Patch failed, target: {target}.", target);
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }
            return;
        }
        if (!string.Equals(source, target, StringComparison.OrdinalIgnoreCase))
        {
            File.Delete(source);
            HardLinkService.DeleteGameFileItem(source);
        }
        File.Move(tmp, target, true);
        if (await HashService.GetFileMD5Async(target, cancellation).ConfigureAwait(false) is string md5)
        {
            HardLinkService.InsertGameFileItem(_context.GameId, target, new FileInfo(target).Length, md5);
        }
    }


}
