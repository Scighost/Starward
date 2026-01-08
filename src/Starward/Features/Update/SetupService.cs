using Microsoft.Extensions.Logging;
using Snap.HPatch;
using Starward.Helpers;
using Starward.Setup.Core;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Update;

internal class SetupService
{

    private readonly ILogger<SetupService> _logger;

    private readonly HttpClient _httpClient;

    private readonly ReleaseClient _releaseClient;


    public SetupService(ILogger<SetupService> logger, HttpClient httpClient, ReleaseClient releaseClient)
    {
        _logger = logger;
        _httpClient = httpClient;
        _releaseClient = releaseClient;
    }


    public long SetupTotalBytes { get; private set; }

    public long SetupDownloadBytes { get; private set; }


    private ReleaseInfoDetail? _detail;



    private async Task<ReleaseInfoDetail> GetReleaseInfoDetailAsync(CancellationToken cancellationToken = default)
    {
        return await _releaseClient.GetLatestReleaseInfoDetailAsync(AppConfig.EnablePreviewRelease, AppConfig.AppVersion, RuntimeInformation.ProcessArchitecture, (InstallType)(AppConfig.IsPortable ? 1 : 0), cancellationToken);
    }




    public async Task<string?> DownloadSetupAsync(ReleaseInfoDetail? detail, CancellationToken cancellationToken = default)
    {
        _detail = detail ??= await GetReleaseInfoDetailAsync(cancellationToken);

        if (detail?.Setup is null)
        {
            return null;
        }

        string setupPath = Path.Combine(AppConfig.CacheFolder, detail.Setup.FileName);
        string url = detail.Setup.Url;
        long size = detail.Setup.Size;
        string hash = detail.Setup.Hash;

        if (File.Exists(setupPath))
        {
            if (await FileHashHelper.CheckSHA256Async(setupPath, size, hash, cancellationToken))
            {
                SetupTotalBytes = size;
                SetupDownloadBytes = size;
                return setupPath;
            }

            if (detail.Diffs.TryGetValue(AppConfig.AppVersion, out var diff) && diff.SetupDiff is ReleaseSetupDiff setupDiff)
            {
                if (await FileHashHelper.CheckSHA256Async(setupPath, setupDiff.OldFileHash, cancellationToken))
                {
                    SetupTotalBytes = setupDiff.Size;
                    using var diffStream = new MemoryStream();
                    await DownloadFileAsync(diffStream, setupDiff.Url, setupDiff.Size, setupDiff.Hash, cancellationToken);
                    byte[] oldFileBytes = await File.ReadAllBytesAsync(setupPath, cancellationToken);
                    using var fs = File.Open(setupPath, FileMode.Create, FileAccess.ReadWrite);
                    HPatch.PatchZstandard(new MemoryStream(oldFileBytes), diffStream, fs);
                    fs.Position = 0;
                    if (await FileHashHelper.CheckSHA256Async(fs, size, hash, cancellationToken))
                    {
                        return setupPath;
                    }
                }
            }
        }

        SetupTotalBytes = detail.Setup.Size;
        await DownloadFileAsync(setupPath, url, size, hash, cancellationToken);
        return setupPath;
    }


    private async Task DownloadFileAsync(string path, string url, long size, string hash, CancellationToken cancellationToken = default)
    {
        using var fs = File.Open(path, FileMode.OpenOrCreate);
        await DownloadFileAsync(fs, url, size, hash, cancellationToken);
    }


    private async Task DownloadFileAsync(Stream stream, string url, long size, string hash, CancellationToken cancellationToken = default)
    {
        bool success = false;
        for (int i = 0; i < 3; i++)
        {
            SetupDownloadBytes = stream.Length;
            if (stream.Length < SetupTotalBytes)
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.VersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(stream.Length, null);
                using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentRange?.From is not null)
                {
                    stream.Position = response.Content.Headers.ContentRange.From.Value;
                    SetupDownloadBytes = stream.Position;
                }
                using var hs = await response.Content.ReadAsStreamAsync(cancellationToken);
                int read = 0;
                Memory<byte> buffer = new byte[8192];
                while ((read = await hs.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await stream.WriteAsync(buffer[..read], cancellationToken);
                    SetupDownloadBytes += read;
                }
                SetupDownloadBytes = stream.Length;
            }
            stream.Position = 0;
            if (await FileHashHelper.CheckSHA256Async(stream, hash, cancellationToken))
            {
                success = true;
                break;
            }
            stream.SetLength(0);
        }
        if (!success)
        {
            throw new Exception("Setup file checksum mismatched.");
        }
    }




    public async Task MigrateToSetupAsync(ReleaseInfoDetail detail, CancellationToken cancellationToken = default)
    {
        string? setupPath = await DownloadSetupAsync(detail, cancellationToken);
        if (!File.Exists(setupPath))
        {
            throw new NotSupportedException("Migrate to setup is not supported.");
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = setupPath,
            UseShellExecute = true,
            Verb = "runas",
            Arguments = $"""
                migrate --Version "{AppConfig.AppVersion}" --Path "{AppContext.BaseDirectory.TrimEnd('\\')}" --UserDataFolder "{AppConfig.UserDataFolder?.TrimEnd('\\')}"
                """,
        });
    }



    public async Task UpdateAsync(ReleaseInfoDetail detail, CancellationToken cancellationToken = default)
    {
        string? setupPath = await DownloadSetupAsync(detail, cancellationToken);
        if (!File.Exists(setupPath))
        {
            throw new NotSupportedException("Update is not supported.");
        }
        Process.Start(new ProcessStartInfo
        {
            FileName = setupPath,
            UseShellExecute = true,
            Verb = "runas",
            Arguments = $"""
                update --Version "{AppConfig.AppVersion}" --Path "{AppContext.BaseDirectory.TrimEnd('\\')}"
                """,
        });
    }




}
