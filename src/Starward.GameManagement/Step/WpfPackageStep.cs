using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpSevenZip;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Service;

namespace Starward.GameManagement.Step;

internal class WpfPackageStep : StepBase
{

    private readonly ILogger<WpfPackageStep> _logger;

    public WpfPackageStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<WpfPackageStep>>();
    }


    private WPFPackageInfo? _wpf;



    public override async Task PrepareAsync(CancellationToken cancellation)
    {
        _wpf ??= await _hoYoPlayService.GetWPFPackageAsync(_context.GameId, cancellation);
        if (_wpf is null)
        {
            _logger.LogInformation("WPF package resource is null, skip.");
            SkipStep = true;
        }
        else if (_wpf.WPFPackage.Version == _context.ConfigIni.WpfVersion)
        {
            _logger.LogInformation("WPF package is up to date ({version}), skip.", _wpf.WPFPackage.Version);
            SkipStep = true;
        }
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation)
    {
        if (_wpf is not null)
        {
            long size = _wpf.WPFPackage.Size;
            string url = _wpf.WPFPackage.Url;
            string md5 = _wpf.WPFPackage.MD5;
            _context.ClearProgress();
            _context.Step = GameInstallStep.Downloading;
            _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;
            _context.DownloadTotalBytes = size;
            _logger.LogInformation("Start downloading WPF package {version}, size: {size} bytes.", _wpf.WPFPackage.Version, size);



            string path = Path.Combine(_context.InstallPath, Helper.GetUrlFileName(url));

            await _polly.ExecuteAsync(async token =>
            {
                await _downloadService.DownloadToFileAsync(path, url, size, _context, token);
                if (!await HashService.CheckFileMD5Async(path, size, md5, token))
                {
                    File.Delete(path);
                    _context.AddNetworkBytes(-size);
                    _logger.LogError("WPF package MD5 check failed, path: {path}, md5: {md5}.", path, md5);
                    throw new FileMD5FailedException(path, md5);
                }
            }, cancellation);

            _context.Step = GameInstallStep.Decompressing;
            _logger.LogInformation("Extracting WPF package.");
            using var archive = new SharpSevenZipExtractor(path);
            archive.ExtractArchive(_context.InstallPath);
            archive.Dispose();
            File.Delete(path);

            _context.ConfigIni.WpfVersion = _wpf.WPFPackage.Version;
            _logger.LogInformation("Updated WPF package to version {version}.", _wpf.WPFPackage.Version);
        }
    }

}
