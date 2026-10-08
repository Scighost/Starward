using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.GameManagement.Model;
using Starward.GameManagement.Service;
using System.Text.Json;

namespace Starward.GameManagement.Step;

internal class PackageRepairStep : PackageStepBase
{

    private readonly ILogger<PackageRepairStep> _logger;

    public PackageRepairStep(GameInstallContext context, IServiceProvider serviceProvider) : base(context, serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<PackageRepairStep>>();
    }



    private GamePackage _package;

    private string _urlPrefix;

    private List<PkgVersionItem> _pkgItems;

    private List<PkgVersionItem> _repairPkgItems;



    public override async Task PrepareAsync(CancellationToken cancellation = default)
    {
        if (_context.GameConfig.DefaultDownloadMode is not DownloadMode.DOWNLOAD_MODE_FILE)
        {
            _logger.LogWarning("Not support download mode DOWNLOAD_MODE_FILE.");
            throw new NotSupportedException($"Game ({_context.GameId.Id}, {_context.GameId.GameBiz}) not support download mode DOWNLOAD_MODE_FILE.");
        }
        _package ??= (await _hoYoPlayService.GetGamePackageAsync(_context.GameId, cancellation)).EnsureNotNull(_context.GameId);
        if (string.IsNullOrWhiteSpace(_package.Main.Major?.ResListUrl))
        {
            _logger.LogWarning("ResListUrl is null or empty.");
            throw new NotSupportedException($"ResListUrl of game ({_context.GameId.Id}, {_context.GameId.GameBiz}) is null or empty.");
        }
        _urlPrefix = _package.Main.Major.ResListUrl;
        _pkgItems = await DownloadAndParsePkgVersionAsync(_context.AudioLanguage, cancellation);
    }



    private async Task<List<PkgVersionItem>> DownloadAndParsePkgVersionAsync(AudioLanguage audio, CancellationToken cancellationToken = default)
    {
        List<string> pkgs = ["pkg_version"];
        if (audio.HasFlag(AudioLanguage.Chinese))
        {
            pkgs.Add("Audio_Chinese_pkg_version");
        }
        if (audio.HasFlag(AudioLanguage.English))
        {
            pkgs.Add("Audio_English(US)_pkg_version");
        }
        if (audio.HasFlag(AudioLanguage.Japanese))
        {
            pkgs.Add("Audio_Japanese_pkg_version");
        }
        if (audio.HasFlag(AudioLanguage.Korean))
        {
            pkgs.Add("Audio_Korean_pkg_version");
        }

        List<PkgVersionItem> list = new();
        _logger.LogInformation("Start downloading {count} package version files.", pkgs.Count);
        await _polly.ForEachAsync(pkgs, cancellationToken, async (item, token) =>
        {
            string url = $"{_urlPrefix}/{item}";
            string path = Path.Join(_context.InstallPath, item);
            using var fs = File.Create(path);
            await _downloadService.DownloadToStreamAsync(url, fs, null, token);
            fs.Position = 0;
            List<PkgVersionItem?> pkgVersionItems = await JsonSerializer.DeserializeAsyncEnumerable<PkgVersionItem>(fs, true, null, token).ToListAsync(token);
            lock (list)
            {
                list.AddRange(pkgVersionItems.Where(x => x is not null)!);
            }
        });
        return list;
    }



    protected override async Task ExecuteInternalAsync(CancellationToken cancellation = default)
    {
        await MoveCachedResourceAsync(cancellation);

        if (_repairPkgItems is null)
        {
            _context.ClearProgress();
            _context.Step = GameInstallStep.Verifying;
            _context.ProgressDisplay = ProgressDisplay.Progress;
            double increase = 1.0 / _pkgItems.Count;
            _logger.LogInformation("Start verifying {count} package files.", _pkgItems.Count);

            Lock _lock = new();
            List<PkgVersionItem> list = new();
            await _polly.ForEachAsync(_pkgItems, cancellation, async (item, token) =>
            {
                if (!item.IsFinished)
                {
                    string path = Path.Join(_context.InstallPath, item.RemoteName);
                    if (!await HashService.CheckFileMD5Async(path, item.FileSize, item.MD5, token))
                    {
                        lock (_lock)
                        {
                            list.Add(new PkgVersionItem { FileSize = item.FileSize, MD5 = item.MD5, RemoteName = item.RemoteName });
                        }
                    }
                }
                lock (_lock)
                {
                    _context.PercentProgress += increase;
                }
                item.IsFinished = true;
            });
            _repairPkgItems = list;
            _logger.LogInformation("{repairCount} of {total} files need repair.", _repairPkgItems.Count, _pkgItems.Count);
        }

        _context.ClearProgress();
        _context.Step = GameInstallStep.Downloading;
        _context.ProgressDisplay = ProgressDisplay.NetworkSpeed | ProgressDisplay.NetworkBytes;
        _context.DownloadTotalBytes = _repairPkgItems.Sum(x => x.FileSize);
        _logger.LogInformation("Start downloading {count} repair files, total: {totalSize} bytes.", _repairPkgItems.Count, _context.DownloadTotalBytes);
        await _polly.ForEachAsync(_repairPkgItems, cancellation, async (item, token) =>
        {
            if (item.IsFinished)
            {
                _context.AddNetworkBytes(item.FileSize);
                return;
            }
            string path = Path.Join(_context.InstallPath, item.RemoteName);
            string url = $"{_urlPrefix}/{item.RemoteName}";
            await DownloadFileAndVerifyAsync(path, url, item.FileSize, item.MD5, token);
            item.IsFinished = true;
        });
    }



    protected override Task PostAsync(CancellationToken cancellation = default)
    {
        if (_package.Main.Major is not null && Version.TryParse(_package.Main.Major.Version, out Version? version))
        {
            _context.ConfigIni.GameVersion = version;
            _logger.LogInformation("Set game version to {version}.", version);
        }
        return Task.CompletedTask;
    }

}
