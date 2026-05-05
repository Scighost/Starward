using SharpCompress.Compressors.ZStandard;
using Starward.Setup.Core;
using System.Buffers;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Starward.Setup.Services;

public class DownloadService
{


    protected HttpClient _httpClient;

    protected ReleaseClient _releaseClient;



    public DownloadService()
    {
        RecreateHttpClient();
    }



    protected void RecreateHttpClient()
    {
        string ver = typeof(DownloadService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All });
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"Starward.Setup/{ver}");
        _releaseClient = new ReleaseClient(_httpClient);
    }


    public async Task<ReleaseInfoDetail> GetReleaseInfoAsync(bool preview = false, CancellationToken cancellation = default)
    {
        return await RetryHelper.ExecuteAsync(async (token) => await _releaseClient.GetLatestReleaseInfoDetailAsync(preview, "setup", RuntimeInformation.OSArchitecture, InstallType.Setup, token), cancellation);
    }


    public async Task<ReleaseInfoDetail> GetReleaseInfoAsync(string version, CancellationToken cancellation = default)
    {
        return await RetryHelper.ExecuteAsync(async (token) =>
        {
            var info = await _releaseClient.GetReleaseInfoAsync(version, token);
            if (info.TryGetReleaseInfoDetail(RuntimeInformation.OSArchitecture, InstallType.Setup, out var detail))
            {
                return detail;
            }
            else
            {
                throw new PlatformNotSupportedException("No suitable release found for this platform.");
            }
        }, cancellation);
    }



    public async Task<ReleaseManifest> GetReleaseManifestAsync(string url, CancellationToken cancellation = default)
    {
        return await RetryHelper.ExecuteAsync(async (token) => await _releaseClient.GetReleaseManifestAsync(url, token), cancellation);
    }



    public long TotalBytes { get => _totalBytes; protected set => _totalBytes = value; }
    protected long _totalBytes;


    public long DownloadBytes { get => _downloadBytes; protected set => _downloadBytes = value; }
    protected long _downloadBytes;



    protected static async Task<bool> CheckFileHashAsync(string path, string hash, long size = 0, CancellationToken cancellation = default)
    {
        if (!File.Exists(path))
        {
            return false;
        }
        using var fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        if (size > 0 && fs.Length != size)
        {
            return false;
        }
        byte[] sha256 = await SHA256.HashDataAsync(fs, cancellation).ConfigureAwait(false);
        return string.Equals(Convert.ToHexStringLower(sha256), hash, StringComparison.OrdinalIgnoreCase);
    }




    protected async Task DownloadFileAsync(string url, string path, long size, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        Interlocked.Add(ref _downloadBytes, fs.Length);
        if (fs.Length < size)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new RangeHeaderValue(fs.Length, null);
                var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();
                if (response.Content.Headers.ContentRange?.From is not null)
                {
                    fs.Position = response.Content.Headers.ContentRange.From.Value;
                }
                else
                {
                    Interlocked.Add(ref _downloadBytes, -fs.Length);
                }
                using var hs = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
                byte[] buffer = ArrayPool<byte>.Shared.Rent(1 << 16);
                try
                {
                    int read;
                    while ((read = await hs.ReadAsync(buffer, cancellation).ConfigureAwait(false)) != 0)
                    {
                        await fs.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                        Interlocked.Add(ref _downloadBytes, read);
                    }
                    await fs.FlushAsync(cancellation).ConfigureAwait(false);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(buffer);
                }
            }
            catch
            {
                Interlocked.Add(ref _downloadBytes, -fs.Length);
                throw;
            }
        }
    }




    protected async Task DownloadFileAndCheckFileHashAsync(string url, string path, long size, string hash, CancellationToken cancellation = default)
    {
        if (await CheckFileHashAsync(path, hash, size, cancellation).ConfigureAwait(false))
        {
            Interlocked.Add(ref _downloadBytes, size);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            string path_tmp = path + ".tmp";
            await DownloadFileAsync(url, path_tmp, size, cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            if (await CheckFileHashAsync(path_tmp, hash, size, cancellation).ConfigureAwait(false))
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                File.Move(path_tmp, path);
            }
            else
            {
                File.Delete(path_tmp);
                throw new Exception($"Checksum failed: {path}");
            }
        }
    }



    protected async Task DownloadZstdFileAsync(string url, string path, long size, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        Interlocked.Add(ref _downloadBytes, fs.Length);
        if (fs.Length < size)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
            using DecompressionStream ds = new(hs, 8192);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                int read = 0;
                while ((read = await ds.ReadAsync(buffer, cancellation).ConfigureAwait(false)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, read), cancellation).ConfigureAwait(false);
                    long p = fs.Position;
                    Interlocked.Add(ref _downloadBytes, read);
                }
                await fs.FlushAsync(cancellation).ConfigureAwait(false);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
    }



    protected void CopySetupFile(string installFolder)
    {
        if (Path.Combine(installFolder, "Starward.Setup.exe") != Environment.ProcessPath)
        {
            Directory.CreateDirectory(installFolder);
            File.Copy(Environment.ProcessPath!, Path.Combine(installFolder, "Starward.Setup.exe"), true);
        }
    }


}
