using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.InstallGame;

internal class Honkai3rdInstallGameService : InstallGameService
{

    public override GameBiz CurrentGame => GameBiz.GenshinImpact;


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

                // BH3Base.dll 不在资源列表中，需要单独下载
                _logger.LogInformation("Download BH3Base.dll.");
                await DownloadBH3BaseAsync(cancellationTokenSource.Token).ConfigureAwait(false);
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

        // BH3Base.dll 不在资源列表中，需要单独下载
        string BH3Base = Path.Combine(InstallPath, "BH3Base.dll");
        if (File.Exists(BH3Base))
        {
            File.Delete(BH3Base);
        }
        long? length = await GetContentLengthAsync($"{separateUrlPrefix}/BH3Base.dll");
        downloadTasks.Add(new DownloadFileTask
        {
            FileName = "BH3Base.dll",
            MD5 = "",
            Size = length ?? 0,
        });

        TotalBytes = downloadTasks.Sum(x => x.Size);
        progressBytes = 0;
        TotalCount = downloadTasks.Count;
        progressCount = 0;
    }



    protected async Task DownloadBH3BaseAsync(CancellationToken cancellationToken)
    {
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
        await DownloadFileAsync(task, cancellationToken).ConfigureAwait(false);
        if (File.Exists(BH3Base_tmp))
        {
            File.Move(BH3Base_tmp, BH3Base, true);
        }
    }





}
