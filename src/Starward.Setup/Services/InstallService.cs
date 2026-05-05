using SharpCompress.Archives;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Common;
using SharpCompress.Readers;
using Starward.Setup.Core;
using System.Diagnostics;
using System.Reflection;

namespace Starward.Setup.Services;

public class InstallService : DownloadService
{


    public bool FullPackage { get; private set; }

    public long TotalSize { get; private set; }

    public int TotalCount { get; private set; }

    public string AppVersion { get; set; }




    public ReleaseInfoDetail ReleaseInfo { get; protected set; }

    public ReleaseManifest ReleaseManifest { get; protected set; }




    public async Task PrepareManifestAsync(CancellationToken cancellation = default)
    {
        using Stream? stream = typeof(InstallService).Assembly.GetManifestResourceStream("Starward.Setup.Assets.Starward.7z");
        if (stream is not null)
        {
            using var archive = SevenZipArchive.OpenArchive(stream);
            TotalSize = archive.TotalUncompressedSize;
            TotalCount = archive.Entries.Count(x => !x.IsDirectory);
            FullPackage = true;
            AppVersion = typeof(InstallService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "";
        }
        else if (ReleaseInfo is null || ReleaseManifest is null)
        {
            ReleaseInfo = await GetReleaseInfoAsync(false, cancellation);
            ReleaseManifest = await GetReleaseManifestAsync(ReleaseInfo.ManifestUrl, cancellation);
            TotalSize = ReleaseManifest.Size + new FileInfo(Environment.ProcessPath!).Length;
            AppVersion = ReleaseManifest.Version;
        }
    }




    public async Task StartInstallAsync(string installFolder, CancellationToken cancellation = default)
    {
        try
        {
            RecreateHttpClient();
            await PrepareManifestAsync(cancellation);

            TotalBytes = ReleaseManifest.Size;
            DownloadBytes = 0;

            Directory.CreateDirectory(installFolder);
            string[] files = Directory.GetFiles(installFolder, "*", SearchOption.AllDirectories);
            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            await Parallel.ForEachAsync(ReleaseManifest.Files, cancellation, async (releaseFile, token) =>
            {
                await RetryHelper.ExecuteAsync(async (ct) => await DownloadManifestFileAsync(installFolder, releaseFile, ct).ConfigureAwait(false), token);
            }).ConfigureAwait(false);

            CopySetupFile(installFolder);
            RegistryHelper.WriteUninstallInfo(installFolder, ReleaseInfo.Version, ReleaseManifest.Size + new FileInfo(Environment.ProcessPath!).Length);
            RegistryHelper.WriteUrlProtocol(installFolder);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            throw;
        }
    }




    private async Task DownloadManifestFileAsync(string installFolder, ReleaseFile releaseFile, CancellationToken cancellation = default)
    {
        string filePath = Path.Combine(installFolder, releaseFile.Path);
        if (await CheckFileHashAsync(filePath, releaseFile.Hash, releaseFile.Size, cancellation).ConfigureAwait(false))
        {
            Interlocked.Add(ref _downloadBytes, releaseFile.Size);
        }
        else
        {
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)!);
            string path_tmp = filePath + ".tmp";
            string url = ReleaseManifest.UrlPrefix + releaseFile.Id;
            await DownloadZstdFileAsync(url, path_tmp, releaseFile.CompressedSize, cancellation).ConfigureAwait(false);
            cancellation.ThrowIfCancellationRequested();
            if (await CheckFileHashAsync(path_tmp, releaseFile.Hash, releaseFile.Size, cancellation).ConfigureAwait(false))
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
                File.Move(path_tmp, filePath);
            }
            else
            {
                File.Delete(path_tmp);
                Interlocked.Add(ref _downloadBytes, -releaseFile.Size);
                throw new Exception($"Checksum failed: {filePath}");
            }
        }
    }



    public async Task ExtractAsync(string installFolder, IProgress<ProgressReport> progress, CancellationToken cancellation = default)
    {
        using Stream? stream = typeof(InstallService).Assembly.GetManifestResourceStream("Starward.Setup.Assets.Starward.7z");
        if (stream is null)
        {
            throw new NotSupportedException("Extracting is only supported for the full package.");
        }

        Directory.CreateDirectory(installFolder);
        string[] files = Directory.GetFiles(installFolder, "*", SearchOption.AllDirectories);
        foreach (string file in files)
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        using var archive = SevenZipArchive.OpenArchive(stream, new ReaderOptions { Progress = progress });
        await Task.Run(() => archive.WriteToDirectory(installFolder), cancellation);

        RegistryHelper.WriteUninstallInfo(installFolder, AppVersion, TotalSize);
        RegistryHelper.WriteUrlProtocol(installFolder);
    }



}
