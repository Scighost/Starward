using SharpCompress.Compressors.ZStandard;
using Starward.Setup.Core;
using System.Buffers;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Starward.Setup.Services;

public class DownloadService
{

    private HttpClient? _httpClient;

    protected ReleaseClient _releaseClient;



    public DownloadService()
    {
        RecreateHttpClient();
    }



    protected void RecreateHttpClient()
    {
        string ver = typeof(DownloadService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        _httpClient?.Dispose();
        _httpClient = new HttpClient(new HttpClientHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher };
        _httpClient.DefaultRequestHeaders.Add("User-Agent", $"Starward.Setup/{ver}");
        _releaseClient = new ReleaseClient(_httpClient);
    }


    public async Task<ReleaseInfoDetail> GetReleaseInfoAsync(bool preview = false, CancellationToken cancellation = default)
    {
        return await RetryHelper.ExecuteAsync(async (token) => await _releaseClient.GetLatestReleaseInfoDetailAsync(preview, "setup", RuntimeInformation.OSArchitecture, InstallType.Setup, token).ConfigureAwait(false), cancellation).ConfigureAwait(false);
    }


    public async Task<ReleaseInfoDetail> GetReleaseInfoAsync(string version, CancellationToken cancellation = default)
    {
        return await RetryHelper.ExecuteAsync(async (token) =>
        {
            var info = await _releaseClient.GetReleaseInfoAsync(version, token).ConfigureAwait(false);
            if (info.TryGetReleaseInfoDetail(RuntimeInformation.OSArchitecture, InstallType.Setup, out var detail))
            {
                return detail;
            }
            else
            {
                throw new PlatformNotSupportedException("No suitable release found for this platform.");
            }
        }, cancellation).ConfigureAwait(false);
    }



    public async Task<ReleaseManifest> GetReleaseManifestAsync(string url, CancellationToken cancellation = default)
    {
        return await RetryHelper.ExecuteAsync(async (token) => await _releaseClient.GetReleaseManifestAsync(url, token).ConfigureAwait(false), cancellation).ConfigureAwait(false);
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
        if (fs.Length < size)
        {
            try
            {
                using var response = await _releaseClient.OpenAssetAsync(url, cancellation, fs.Length).ConfigureAwait(false);
                if (response.Content.Headers.ContentRange?.From is not null)
                {
                    fs.Position = response.Content.Headers.ContentRange.From.Value;
                    Interlocked.Add(ref _downloadBytes, fs.Position);
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
        else
        {
            Interlocked.Add(ref _downloadBytes, fs.Length);
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



    protected async Task DownloadZstdFileAsync(ReleaseManifest manifest, ReleaseFile file, string path, CancellationToken cancellation = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        using var fs = File.Open(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite | FileShare.Delete);
        if (fs.Length < file.Size)
        {
            fs.Position = 0;
            using var response = await _releaseClient.DownloadReleaseFileAsync(manifest, file, cancellation).ConfigureAwait(false);
            using var hs = await response.Content.ReadAsStreamAsync(cancellation).ConfigureAwait(false);
            using DecompressionStream ds = new(hs, 8192);
            byte[] buffer = ArrayPool<byte>.Shared.Rent(8192);
            try
            {
                int read = 0;
                while ((read = await ds.ReadAsync(buffer, cancellation).ConfigureAwait(false)) > 0)
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
        else
        {
            Interlocked.Add(ref _downloadBytes, fs.Length);
        }
    }



    protected static void CopySetupFile(string installFolder)
    {
        if (Path.Combine(installFolder, "Starward.Setup.exe") != Environment.ProcessPath)
        {
            Directory.CreateDirectory(installFolder);
            File.Copy(Environment.ProcessPath!, Path.Combine(installFolder, "Starward.Setup.exe"), true);
        }
    }


}
