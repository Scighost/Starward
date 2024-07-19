using CommunityToolkit.WinUI.UI.Triggers;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Services.InstallGame;
using Starward.Services.Launcher;
using Starward.SevenZip;
using Starward.Starward_XamlTypeInfo;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal abstract class InstallGameService
{


    protected readonly ILogger<InstallGameService> _logger;

    protected readonly HttpClient _httpClient;

    protected readonly GameLauncherService _launcherService;

    protected readonly GamePackageService _packageService;

    protected readonly HoYoPlayService _hoYoPlayService;






    public abstract bool CanRepairGameFiles { get; }




    public abstract bool CanRepairAudioFiles { get; }




    public InstallGameState State { get; protected set; }



    protected InstallGameState _currentTask;






    public virtual async Task StartInstallGameAsync(GameBiz gameBiz, string installPath, CancellationToken cancellationToken = default)
    {
        var package = await _packageService.GetGamePackageAsync(gameBiz);
        var files = await GetGamePackageFilesFromGameResourceAsync(gameBiz, installPath, package.Main.Major!);
        List<InstallGameItem> list = new();
        foreach (var item in files)
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
    }







    public virtual Task StartRepairGameAsync(GameBiz gameBiz, string installPath, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException();

    }




    public virtual async Task StartPredownloadAsync(GameBiz gameBiz, string installPath, CancellationToken cancellationToken = default)
    {
        var package = await _packageService.GetGamePackageAsync(gameBiz);
        List<InstallGameItem> list = new();
        GamePackageResource resource;
        var localVersion = await _launcherService.GetLocalGameVersionAsync(gameBiz, installPath);
        if (package.PreDownload is not null)
        {
            if (package.PreDownload.Patches.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource resource1)
            {
                resource = resource1;
            }
            else
            {
                resource = package.PreDownload.Major!;
            }
            var files = await GetGamePackageFilesFromGameResourceAsync(gameBiz, installPath, resource);
            foreach (var item in files)
            {
                list.Add(new InstallGameItem
                {
                    Type = InstallGameItemType.Download,
                    FileName = Path.GetFileName(item.Url),
                    Path = Path.Combine(installPath, Path.GetFileName(item.Url)),
                    Url = item.Url,
                    MD5 = item.MD5,
                    Size = item.Size,
                    DecompressedSize = item.DecompressedSize,
                    WriteAsTempFile = true,
                });
            }
        }
    }




    public virtual async Task StartUpdateGameAsync(GameBiz gameBiz, string installPath, CancellationToken cancellationToken = default)
    {
        var package = await _packageService.GetGamePackageAsync(gameBiz);
        List<InstallGameItem> list = new();
        GamePackageResource resource;
        var localVersion = await _launcherService.GetLocalGameVersionAsync(gameBiz, installPath);
        if (package.Main.Patches.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource resource1)
        {
            resource = resource1;
        }
        else
        {
            resource = package.Main.Major!;
        }
        var files = await GetGamePackageFilesFromGameResourceAsync(gameBiz, installPath, resource);
        foreach (var item in files)
        {
            list.Add(new InstallGameItem
            {
                Type = InstallGameItemType.Download,
                FileName = Path.GetFileName(item.Url),
                Path = Path.Combine(installPath, Path.GetFileName(item.Url)),
                Url = item.Url,
                MD5 = item.MD5,
                Size = item.Size,
                DecompressedSize = item.DecompressedSize,
                WriteAsTempFile = true,
            });
        }
    }






    protected async Task<List<GamePackageFile>> GetGamePackageFilesFromGameResourceAsync(GameBiz gameBiz, string installPath, GamePackageResource resource)
    {
        string lang = await GetAudioLanguageAsync(gameBiz, installPath);
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name);
            await SetAudioLanguageAsync(gameBiz, installPath, lang);
        }
        List<GamePackageFile> list = resource.GamePackages.ToList();
        foreach (var item in resource.AudioPackages ?? [])
        {
            if (!string.IsNullOrWhiteSpace(item.Language) && lang.Contains(item.Language))
            {
                list.Add(item);
            }
        }
        return list;
    }






    protected async Task<string> GetAudioLanguageAsync(GameBiz biz, string installPath)
    {
        var sb = new StringBuilder();
        var config = await _hoYoPlayService.GetGameConfigAsync(biz);
        if (config is not null && !string.IsNullOrWhiteSpace(config.AudioPackageScanDir))
        {
            string file = Path.Join(installPath, config.AudioPackageScanDir);
            if (File.Exists(file))
            {
                var lines = await File.ReadAllLinesAsync(file);
                if (lines.Any(x => x.Contains("Chinese"))) { sb.Append("zh-cn"); }
                if (lines.Any(x => x.Contains("English(US)"))) { sb.Append("en-us"); }
                if (lines.Any(x => x.Contains("Japanese"))) { sb.Append("ja-jp"); }
                if (lines.Any(x => x.Contains("Korean"))) { sb.Append("ko-kr"); }
            }
        }
        return sb.ToString();
    }




    protected async Task SetAudioLanguageAsync(GameBiz biz, string installPath, string lang)
    {
        var config = await _hoYoPlayService.GetGameConfigAsync(biz);
        if (config is not null && !string.IsNullOrWhiteSpace(config.AudioPackageScanDir))
        {
            string file = Path.Join(installPath, config.AudioPackageScanDir);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var lines = new List<string>(4);
            if (lang.Contains("zh-cn") || lang.Contains("zh-tw")) { lines.Add("Chinese"); }
            if (lang.Contains("en-us")) { lines.Add("English(US)"); }
            if (lang.Contains("ja-jp")) { lines.Add("Japanese"); }
            if (lang.Contains("ko-kr")) { lines.Add("Korean"); }
            await File.WriteAllLinesAsync(file, lines);
        }
    }







    public async Task Continue()
    {

    }




    public async Task Pause()
    {

    }




    public void ClearState()
    {

    }





    private void CurrentTaskFinished()
    {

    }





    #region Install Internal





    protected ConcurrentQueue<InstallGameItem> _installItemQueue;




    protected int _totalCount;
    public int TotalCount => _totalCount;



    protected int _finishCount;
    public int FinishCount => _finishCount;



    protected long _totalBytes;
    public long TotalBytes => _totalBytes;



    protected long _finishBytes;
    public long FinishBytes => _finishBytes;



    protected int _concurrentExecuteThreadCount;
    public int ConcurrentExecuteThreadCount => _concurrentExecuteThreadCount;







    protected async Task ExecuteTaskAsync()
    {
        try
        {
            Interlocked.Increment(ref _concurrentExecuteThreadCount);

            while (_installItemQueue.TryDequeue(out InstallGameItem? item))
            {
                try
                {
                    switch (item.Type)
                    {
                        case InstallGameItemType.None:
                            break;
                        case InstallGameItemType.Download:
                            await DownloadItemAsync(item);
                            break;
                        case InstallGameItemType.Verify:
                            await VerifyItemAsync(item);
                            break;
                        case InstallGameItemType.Decompress:
                            await DecompressItemAsync(item);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception ex)
                {

                }

            }
        }
        catch (Exception ex)
        {

        }
        finally
        {
            Interlocked.Decrement(ref _concurrentExecuteThreadCount);
        }
    }




    protected async Task DownloadItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 14;
        string file = item.WriteAsTempFile ? item.Path + "_tmp" : item.Path;
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        using var fs = File.Open(file, FileMode.OpenOrCreate);
        if (fs.Length < item.Size)
        {
            _logger.LogInformation("Download: FileName {name}, Url {url}", item.FileName, item.Url);
            fs.Position = fs.Length;
            var request = new HttpRequestMessage(HttpMethod.Get, item.Url) { Version = HttpVersion.Version11 };
            request.Headers.Range = new RangeHeaderValue(fs.Length, null);
            using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            using var hs = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var buffer = new byte[BUFFER_SIZE];
            int length;
            while ((length = await hs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await fs.WriteAsync(buffer.AsMemory(0, length), cancellationToken).ConfigureAwait(false);
                Interlocked.Add(ref _finishBytes, length);
            }
            _logger.LogInformation("Download Successfully: FileName {name}", item.FileName);
        }
    }






    protected async Task<bool> VerifyItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        const int BUFFER_SIZE = 1 << 20;
        string file = item.WriteAsTempFile ? item.Path + "_tmp" : item.Path;
        using var fs = File.OpenRead(file);
        if (fs.Length != item.Size)
        {
            return false;
        }
        int length = 0;
        var hashProvider = MD5.Create();
        var buffer = new byte[BUFFER_SIZE];
        while ((length = await fs.ReadAsync(buffer, cancellationToken).ConfigureAwait(false)) != 0)
        {
            hashProvider.TransformBlock(buffer, 0, length, buffer, 0);
            Interlocked.Add(ref _finishBytes, length);
        }
        hashProvider.TransformFinalBlock(buffer, 0, length);
        var hash = hashProvider.Hash;
        fs.Dispose();
        if (string.Equals(Convert.ToHexString(hash!), item.MD5, StringComparison.OrdinalIgnoreCase))
        {
            if (item.WriteAsTempFile)
            {
                File.Move(file, item.Path, true);
            }
            return true;
        }
        else
        {
            return false;
        }
    }





    protected async Task DecompressItemAsync(InstallGameItem item, CancellationToken cancellationToken = default)
    {
        using var fs = new FileSliceStream(item.PackageFiles);
        if (item.PackageFiles[0].Contains(".7z", StringComparison.CurrentCultureIgnoreCase))
        {
            await Task.Run(() =>
            {
                using var extra = new ArchiveFile(fs);
                double ratio = (double)fs.Length / extra.Entries.Sum(x => (long)x.Size);
                long sum = 0;
                extra.ExtractProgress += (_, e) =>
                {
                    long size = (long)(e.Read * ratio);
                    _finishBytes += size;
                    sum += size;
                };
                extra.Extract(item.TargetPath, true);
                _finishBytes += fs.Length - sum;
            }).ConfigureAwait(false);
        }
        else
        {
            await Task.Run(async () =>
            {
                long sum = 0;
                using var zip = new ZipArchive(fs, ZipArchiveMode.Read, true);
                foreach (var entry in zip.Entries)
                {
                    if ((entry.ExternalAttributes & 0x10) > 0)
                    {
                        var target = Path.Combine(item.TargetPath, entry.FullName);
                        Directory.CreateDirectory(target);
                    }
                    else
                    {
                        var target = Path.Combine(item.TargetPath, entry.FullName);
                        Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                        entry.ExtractToFile(target, true);
                        _finishBytes += entry.CompressedLength;
                        sum += entry.CompressedLength;
                    }
                }
                _finishBytes += fs.Length - sum;
                await ApplyDiffFilesAsync(item.TargetPath);
            }).ConfigureAwait(false);
        }
        fs.Dispose();
        foreach (var file in item.PackageFiles)
        {
            File.Delete(file);
        }
    }





    protected async Task ApplyDiffFilesAsync(string installPath)
    {
        var delete = Path.Combine(installPath, "deletefiles.txt");
        if (File.Exists(delete))
        {
            var deleteFiles = await File.ReadAllLinesAsync(delete).ConfigureAwait(false);
            foreach (var file in deleteFiles)
            {
                var target = Path.Combine(installPath, file);
                if (File.Exists(target))
                {
                    File.Delete(target);
                }
            }
            File.Delete(delete);
        }

        var hdifffiles = Path.Combine(installPath, "hdifffiles.txt");
        if (File.Exists(hdifffiles))
        {
            var hpatch = Path.Combine(AppContext.BaseDirectory, "hpatchz.exe");
            var lines = await File.ReadAllLinesAsync(hdifffiles).ConfigureAwait(false);
            foreach (var line in lines)
            {
                var json = JsonNode.Parse(line);
                var name = json?["remoteName"]?.ToString();
                var target = Path.Join(installPath, name);
                var diff = $"{target}.hdiff";
                if (File.Exists(target) && File.Exists(diff))
                {
                    File.SetAttributes(target, FileAttributes.Archive);
                    using var process = Process.Start(new ProcessStartInfo
                    {
                        FileName = hpatch,
                        Arguments = $"""-f "{target}" "{diff}" "{target}"  """,
                        CreateNoWindow = true,
                    });
                    if (process != null)
                    {
                        await process.WaitForExitAsync().ConfigureAwait(false);
                    }
                    if (File.Exists(diff))
                    {
                        File.Delete(diff);
                    }
                }
            }
            File.Delete(hdifffiles);
        }
    }




    #endregion





}
