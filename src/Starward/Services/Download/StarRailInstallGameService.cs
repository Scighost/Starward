using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Services.Launcher;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal class StarRailInstallGameService : InstallGameService
{


    public StarRailInstallGameService(ILogger<InstallGameService> logger, HttpClient httpClient, GameLauncherService launcherService, GamePackageService packageService, HoYoPlayService hoYoPlayService) : base(logger, httpClient, launcherService, packageService, hoYoPlayService)
    {
    }




    public override async Task StartHardLinkAsync(GameBiz linkGameBiz, CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        var linkPackage = await _packageService.GetGamePackageAsync(linkGameBiz);
        string? linkInstallPath = _launcherService.GetGameInstallPath(linkGameBiz);
        if (!Directory.Exists(linkInstallPath))
        {
            throw new DirectoryNotFoundException($"Cannot find installation path of game ({linkGameBiz}).");
        }
        if (Path.GetPathRoot(_installPath) != Path.GetPathRoot(linkInstallPath))
        {
            throw new NotSupportedException("Hard linking between different drives is not supported.");
        }
        _hardLinkPath = linkInstallPath;
        _hardLinkGameBiz = linkGameBiz;
        var prefix = _gamePackage.Main.Major!.ResListUrl;
        var linkPrefix = linkPackage.Main.Major!.ResListUrl;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new NotSupportedException($"Hard linking game ({CurrentGameBiz}) is not supported.");
        }
        _gameFileItems = await GetPkgVersionsAsync(prefix, "pkg_version");
        var linkGameFilesItem = await GetPkgVersionsAsync(linkPrefix, "pkg_version");
        var same = _gameFileItems.IntersectBy(linkGameFilesItem.Select(x => (x.Path, x.MD5)), x => (x.Path, x.MD5)).ToList();
        var diff = _gameFileItems.ExceptBy(linkGameFilesItem.Select(x => (x.Path, x.MD5)), x => (x.Path, x.MD5)).ToList();
        foreach (var item in same)
        {
            item.Type = InstallGameItemType.HardLink;
            item.HardLinkSource = Path.Combine(linkInstallPath, Path.GetRelativePath(_installPath, item.Path));
            _installItemQueue.Enqueue(item);
        }
        foreach (var item in diff)
        {
            item.Type = InstallGameItemType.Verify;
            _installItemQueue.Enqueue(item);
        }
        var audioFiles = Directory.GetFiles(Path.Combine(linkInstallPath, @"StarRail_Data\Persistent\Audio"), "*", SearchOption.AllDirectories);
        foreach (var audioFile in audioFiles)
        {
            var item = new InstallGameItem
            {
                Type = InstallGameItemType.HardLink,
                HardLinkSkipVerify = true,
                FileName = Path.GetFileName(audioFile),
                HardLinkSource = audioFile,
                Path = Path.Combine(_installPath, Path.GetRelativePath(linkInstallPath, audioFile)),
            };
            _installItemQueue.Enqueue(item);
        }
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Verify);
        }
        InstallTask = InstallGameTask.HardLink;
        StartTask(InstallGameState.Verify);
    }





}
