using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SharpSevenZip;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;
using Starward.GameManagement.Service;
using System.Text.Json;

namespace Starward.GameManagement.Step;

internal class GameChannelSdkStep : StepBase
{

    private readonly ILogger<GameChannelSdkStep> _logger;

    public GameChannelSdkStep(GameInstallContext context, IServiceProvider serviceProvider)
        : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GameChannelSdkStep>>();
    }



    private GameChannelSDK? _sdk;



    public override async Task PrepareAsync(CancellationToken cancellation)
    {
        _sdk ??= await _hoYoPlayService.GetGameChannelSDKAsync(_context.GameId, cancellation);
        if (_sdk is null)
        {
            _logger.LogInformation("SDK resource is null, skip.");
            SkipStep = true;
            return;
        }
        if (_sdk.Version == _context.ConfigIni.SdkVersion)
        {
            if (!string.IsNullOrWhiteSpace(_sdk.PkgVersionFileName))
            {
                string pkg_version = Path.Combine(_context.InstallPath, _sdk.PkgVersionFileName);
                if (File.Exists(pkg_version))
                {
                    using FileStream fs_pkg = File.Open(pkg_version, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    IAsyncEnumerable<PkgVersionItem?> items = JsonSerializer.DeserializeAsyncEnumerable<PkgVersionItem>(fs_pkg, true, cancellationToken: cancellation);
                    await foreach (PkgVersionItem? item in items)
                    {
                        if (item is not null)
                        {
                            string file = Path.Combine(_context.InstallPath, item.RemoteName);
                            if (!await HashService.CheckFileMD5Async(file, item.FileSize, item.MD5, cancellation))
                            {
                                _logger.LogInformation("SDK file verification failed, path: {file}.", file);
                                return;
                            }
                        }
                    }
                    _logger.LogInformation("SDK is up to date ({version}), skip.", _sdk.Version);
                    SkipStep = true;
                }
                else
                {
                    _logger.LogInformation("SDK package version file not found: {file}.", pkg_version);
                }
            }
        }
    }


    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        if (_sdk is not null)
        {
            long size = _sdk.ChannelSDKPackage.Size;
            string url = _sdk.ChannelSDKPackage.Url;
            string md5 = _sdk.ChannelSDKPackage.MD5;
            _context.ClearProgress();
            _context.DownloadTotalBytes = size;
            _context.WriteTotalBytes = size;
            _context.Step = GameInstallStep.Downloading;
            _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;
            _logger.LogInformation("Start downloading GameChannelSDK {version}, size: {size} bytes.", _sdk.Version, size);

            string path = Path.Combine(_context.InstallPath, Helper.GetUrlFileName(url));
            await _polly.ExecuteAsync(async token =>
            {
                await _downloadService.DownloadToFileAsync(path, url, size, _context, token);
                if (!await HashService.CheckFileMD5Async(path, size, md5, token))
                {
                    _context.AddNetworkBytes(-size);
                    _context.AddStorageBytes(-size);
                    File.Delete(path);
                    _logger.LogError("GameChannelSDK MD5 check failed, path: {path}, md5: {md5}.", path, md5);
                    throw new FileMD5FailedException(path, md5);
                }
            }, cancellation);

            _context.Step = GameInstallStep.Decompressing;
            _logger.LogInformation("Extracting GameChannelSDK.");
            using var archive = new SharpSevenZipExtractor(path);
            archive.ExtractArchive(_context.InstallPath);
            archive.Dispose();
            File.Delete(path);

            _context.ConfigIni.SdkVersion = _sdk.Version;
            _logger.LogInformation("Updated GameChannelSDK to version {version}.", _sdk.Version);
        }
    }

}
