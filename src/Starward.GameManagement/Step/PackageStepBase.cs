using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpSevenZip;
using SharpSevenZip.Exceptions;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Service;

namespace Starward.GameManagement.Step;

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
        await _downloadService.DownloadToFileAsync(path_tmp, url, size, _context, cancellation);
        if (await HashService.CheckFileMD5Async(path_tmp, size, md5, cancellation))
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


    protected async Task ExtractPackagesAsync(List<GamePackageFile> packages, double percentRatio, CancellationToken cancellation = default)
    {
        string[] files = packages.Select(x => Path.Join(_context.InstallPath, Helper.GetUrlFileName(x.Url))).ToArray();
        using FileCombinedStream fs = new(files);
        using var archive = new SharpSevenZipExtractor(fs, leaveOpen: true);
        _logger.LogInformation("Start decompressing {count} package files.", files.Length);

        double lastPercent = _context.PercentProgress;
        archive.Extracting += (_, e) =>
        {
            _context.PercentProgress = lastPercent + e.FinishPercent * percentRatio;
        };
        try
        {
            Task extractTask = Task.Run(() => archive.ExtractArchive(_context.InstallPath), cancellation);
            while (!extractTask.IsCompleted)
            {
                await Task.Delay(50, CancellationToken.None);
                if (cancellation.IsCancellationRequested)
                {
                    fs.Dispose();
                }
            }
            await extractTask;
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


}