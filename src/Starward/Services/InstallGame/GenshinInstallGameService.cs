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

internal class GenshinInstallGameService : InstallGameService
{


    public override GameBiz CurrentGame => GameBiz.GenshinImpact;


    public GenshinInstallGameService(ILogger<GenshinInstallGameService> logger, GameResourceService gameResourceService, LauncherClient launcherClient, HttpClient httpClient)
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
            CanCancel = false;
            cancellationTokenSource?.Cancel();
            cancellationTokenSource = new CancellationTokenSource();

            if (_inState != InstallGameState.Download || skipVerify)
            {
                if (IsRepairMode)
                {
                    State = InstallGameState.Prepare;
                    await PrepareForRepairAsync().ConfigureAwait(false);

                    State = InstallGameState.Verify;
                    await VerifySeparateFilesAsync(CancellationToken.None).ConfigureAwait(false);
                }
                else
                {
                    State = InstallGameState.Prepare;
                    await PrepareForDownloadAsync().ConfigureAwait(false);
                }
            }

            if (_inState is InstallGameState.Download)
            {
                await UpdateDownloadTaskAsync();
            }

            CanCancel = true;
            State = InstallGameState.Download;
            await DownloadAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            CanCancel = false;

            if (skipVerify)
            {
                await SkipVerifyDownloadedFilesAsync().ConfigureAwait(false);
            }
            else
            {
                State = InstallGameState.Verify;
                await VerifyDownloadedFilesAsnyc(cancellationTokenSource.Token).ConfigureAwait(false);
            }

            if (!(IsPreInstall || IsRepairMode))
            {
                State = InstallGameState.Decompress;
                await DecompressAndApplyDiffPackagesAsync(cancellationTokenSource.Token).ConfigureAwait(false);
            }

            await ClearDeprecatedFilesAsync().ConfigureAwait(false);

            if (!IsPreInstall)
            {
                await WriteConfigFileAsync().ConfigureAwait(false);
            }

            State = InstallGameState.Finish;
            _logger.LogInformation("Install game finished.");
        }
        catch (TaskCanceledException)
        {
            State = InstallGameState.None;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install game error.");
            CanCancel = false;
            InvokeStateOrProgressChanged(true, ex);
        }
    }




    protected override async Task VerifySeparateFilesAsync(CancellationToken cancellationToken)
    {
        await base.VerifySeparateFilesAsync(cancellationToken).ConfigureAwait(false);

        // 清理不在资源列表中的文件
        string assetsFolder = CurrentGameBiz switch
        {
            GameBiz.hk4e_cn => Path.Combine(InstallPath, @"YuanShen_Data\StreamingAssets"),
            GameBiz.hk4e_global => Path.Combine(InstallPath, @"GenshinImpact_Data\StreamingAssets"),
            _ => "",
        };
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
                if (file.EndsWith("_tmp", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }
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
