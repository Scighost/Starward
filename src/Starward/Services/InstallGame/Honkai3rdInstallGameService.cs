using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.InstallGame;

internal class Honkai3rdInstallGameService : InstallGameService
{

    public override GameBiz CurrentGame => GameBiz.Honkai3rd;


    public Honkai3rdInstallGameService(ILogger<Honkai3rdInstallGameService> logger, GameResourceService gameResourceService, LauncherClient launcherClient, HttpClient httpClient)
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
            await DownloadAsync(cancellationTokenSource.Token, IsRepairMode).ConfigureAwait(false);
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

            if (!IsPreInstall)
            {
                // BH3Base.dll 不在资源列表中，需要单独下载
                State = InstallGameState.Download;
                await DownloadBH3BaseAsync(cancellationTokenSource.Token).ConfigureAwait(false);
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



    protected async Task DownloadBH3BaseAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Download BH3Base.dll.");

        TotalBytes = 0;
        progressBytes = 0;
        TotalCount = 1;
        progressCount = 0;

        string BH3Base = Path.Combine(InstallPath, "BH3Base.dll");
        string BH3Base_tmp = BH3Base + "_tmp";
        if (File.Exists(BH3Base))
        {
            File.Delete(BH3Base);
        }
        if (File.Exists(BH3Base_tmp))
        {
            File.Delete(BH3Base_tmp);
        }

        string prefix = launcherGameResource.Game.Latest.DecompressedPath;
        string url = $"{prefix}/BH3Base.dll";
        TotalBytes = await GetContentLengthAsync(url).ConfigureAwait(false) ?? 0;

        var task = new DownloadFileTask
        {
            FileName = "BH3Base.dll",
            MD5 = "",
            Size = TotalBytes,
            DownloadSize = 0,
            Url = url,
            IsSegment = false,
        };
        await DownloadFileAsync(task, cancellationToken, noTmp: true).ConfigureAwait(false);
    }





}
