using System.Text.Json;
using Starward.GameInstall.Model;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpSevenZip;
using SharpSevenZip.Exceptions;
using Starward.Core.HoYoPlay;
using Starward.GameInstall.Service;

namespace Starward.GameInstall.Step;

internal class PackageStepBase : StepBase
{

    private readonly ILogger<PackageStepBase> _logger;

    public PackageStepBase(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PackageStepBase>>();
    }


    protected async Task DownloadFileAndVerifyAsync(string path, string url, long size, string md5, CancellationToken cancellation = default)
    {
        if (File.Exists(path))
        {
            _context.AddNetworkBytes(size);
            _context.AddStorageBytes(size);
            return;
        }
        string path_tmp = path + ".tmp";
        await _downloadService.DownloadToFileAsync(path_tmp, url, size, _context, cancellation).ConfigureAwait(false);
        if (await HashService.CheckFileMD5Async(path_tmp, size, md5, cancellation).ConfigureAwait(false))
        {
            File.Move(path_tmp, path, true);
        }
        else
        {
            File.Delete(path_tmp);
            _logger.LogError("File MD5 check failed, path: {path}, md5: {md5}.", path_tmp, md5);
            throw new FileMD5FailedException(path_tmp, md5);
        }
    }


    protected Task ExtractPackagesAsync(List<GamePackageFile> packages, double percentRatio, CancellationToken cancellation = default)
    {
        string[] files = packages.Select(x => Path.Join(_context.InstallPath, Helper.GetUrlFileName(x.Url))).ToArray();
        return ExtractPackagesAsync(files, _context.InstallPath, null, percentRatio, cancellation);
    }


    protected async Task ExtractPackagesAsync(IReadOnlyList<string> files, string destination, string? password, double percentRatio, CancellationToken cancellation)
    {
        using FileCombinedStream fs = new(files);
        using var archive = string.IsNullOrEmpty(password) ? new SharpSevenZipExtractor(fs, leaveOpen: true) : new SharpSevenZipExtractor(fs, password);
        _logger.LogInformation("Start decompressing {count} package files.", files.Count);

        double lastPercent = _context.PercentProgress;
        archive.Extracting += (_, e) =>
        {
            _context.PercentProgress = lastPercent + e.PercentDone * percentRatio / 100;
        };
        try
        {
            Task extractTask = Task.Run(() => archive.ExtractArchive(destination), cancellation);
            while (!extractTask.IsCompleted)
            {
                await Task.Delay(50, CancellationToken.None).ConfigureAwait(false);
                if (cancellation.IsCancellationRequested)
                {
                    fs.Dispose();
                }
            }
            await extractTask.ConfigureAwait(false);
        }
        catch (SharpSevenZipException ex) when (ex.HResult is -2146233088)
        {
            // ObjectDisposedException
            _logger.LogWarning(ex, "Decompress operation canceled.");
            throw new OperationCanceledException("Decompress operation canceled.", ex, cancellation);
        }
        _context.PercentProgress = lastPercent + percentRatio;
    }


    protected void DeletePackageFiles(List<GamePackageFile> packages)
    {
        int count = 0;
        foreach (var package in packages)
        {
            string path = Path.Combine(_context.InstallPath, Helper.GetUrlFileName(package.Url));
            if (File.Exists(path))
            {
                File.Delete(path);
                count++;
            }
        }
        if (count > 0)
        {
            _logger.LogInformation("Deleted {count} package files.", count);
        }
    }


    protected async Task PatchDiffFilesAsync(double percentRatio, CancellationToken cancellation = default)
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
