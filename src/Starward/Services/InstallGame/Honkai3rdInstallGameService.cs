using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Launcher;
using System;
using System.Linq;
using System.Net;
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
            SetAllFileWriteable();

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
                await PrepareBilibiliServerGameSDKAsync();
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

            DecompressBilibiliServerGameSDK();

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

        string prefix = launcherGameResource.Main.Major.ResListUrl;
        string url = $"{prefix}/BH3Base.dll";

        var request = new HttpRequestMessage(HttpMethod.Get, url)
        {
            Version = HttpVersion.Version11,
        };
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        long? contentLength = response.Content.Headers.ContentLength;
        string md5 = "";
        if (response.Headers.TryGetValues("ETag", out var etags))
        {
            md5 = etags.FirstOrDefault() ?? "";
        }
        string contentMD5 = md5.Trim('"');
        var task = new DownloadFileTask
        {
            FileName = "BH3Base.dll",
            MD5 = contentMD5,
            Size = TotalBytes,
            DownloadSize = 0,
            Url = url,
            IsSegment = false,
        };

        bool success = await VerifyFileAsync(task, cancellationToken).ConfigureAwait(false);
        if (success)
        {
            return;
        }

        TotalBytes = contentLength ?? 0;
        await DownloadFileAsync(task, cancellationToken, noTmp: true).ConfigureAwait(false);
    }





}
