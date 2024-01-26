using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.InstallGame;

internal class StarRailInstallGameService : InstallGameService
{


    public override GameBiz CurrentGame => GameBiz.StarRail;


    public StarRailInstallGameService(ILogger<StarRailInstallGameService> logger, GameResourceService gameResourceService, LauncherClient launcherClient, HttpClient httpClient)
        : base(logger, gameResourceService, launcherClient, httpClient)
    {

    }




    public override async Task StartAsync(bool skipVerify = false)
    {
        if (!initialized)
        {
            throw new Exception("Not initialized");
        }
        try
        {
            _logger.LogInformation("Start install game, skipVerify: {skip}", skipVerify);
            _timer.Start();
            CanCancel = false;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            if (State != InstallGameState.Download || skipVerify)
            {
                if (IsRepairMode)
                {
                    _logger.LogInformation("Repair mode, prepare for repair.");
                    State = InstallGameState.Prepare;
                    await PrepareForRepairAsync().ConfigureAwait(false);

                    _logger.LogInformation("Repair mode, verify files.");
                    State = InstallGameState.Verify;
                    await VerifySeparateFilesAsync(CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    _logger.LogInformation("Prepare for download.");
                    State = InstallGameState.Prepare;
                    await PrepareForDownloadAsync().ConfigureAwait(false);
                }
            }

            _logger.LogInformation("Start download files.");
            CanCancel = true;
            State = InstallGameState.Download;
            await DownloadAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            CanCancel = false;

            if (skipVerify)
            {
                _logger.LogInformation("Skip verify downloaded files.");
                await SkipVerifyDownloadedFilesAsync().ConfigureAwait(false);
            }
            else
            {
                _logger.LogInformation("Verify downloaded files.");
                State = InstallGameState.Verify;
                await VerifyDownloadedFilesAsnyc(cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (!(IsPreInstall || IsRepairMode))
            {
                _logger.LogInformation("Decompress downloaded files.");
                State = InstallGameState.Decompress;
                await DecompressAndApplyDiffPackagesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            }

            _logger.LogInformation("Clear deprecated files.");
            await ClearDeprecatedFilesAsync().ConfigureAwait(false);

            _timer.Stop();
            State = InstallGameState.Finish;
            _logger.LogInformation("Install game finished.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install game error.");
            _timer.Stop();
            InvokeStateOrProgressChanged(ex);
        }
    }




    protected override async Task VerifySeparateFilesAsync(CancellationToken cancellationToken)
    {
        await base.VerifySeparateFilesAsync(cancellationToken).ConfigureAwait(false);

        // 删除不在资源列表中的文件
        string assetsFolder = @"StarRail_Data\StreamingAssets";
        List<string>? existFiles = null;
        if (Directory.Exists(assetsFolder))
        {
            existFiles = Directory.GetFiles(assetsFolder, "*", SearchOption.AllDirectories).ToList();
        }
        var packageFiles = separateResources.Select(x => Path.GetFullPath(Path.Combine(InstallPath, x.FileName))).ToList();
        if (existFiles != null)
        {
            var deleteFiles = existFiles.Except(packageFiles).ToList();
            foreach (var file in deleteFiles)
            {
                if (File.Exists(file))
                {
                    _logger.LogInformation("Delete deprecated file: {file}", file);
                    File.SetAttributes(file, FileAttributes.Normal);
                    File.Delete(file);
                }
            }
        }

        TotalBytes = downloadTasks.Sum(x => x.Size);
        progressBytes = 0;
        TotalCount = downloadTasks.Count;
        progressCount = 0;
    }




}
