using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Services.Launcher;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Download;

internal class GenshinInstallGameService : InstallGameService
{


    public GenshinInstallGameService(ILogger<GenshinInstallGameService> logger, HttpClient httpClient, GameLauncherService launcherService, GamePackageService packageService, HoYoPlayService hoYoPlayService) : base(logger, httpClient, launcherService, packageService, hoYoPlayService)
    {

    }




    public override async Task StartRepairGameAsync(CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        var prefix = _gamePackage.Main.Major!.ResListUrl;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new NotSupportedException($"Repairing game ({CurrentGameBiz}) is not supported.");
        }
        _gameFileItems = await GetPkgVersionsAsync(prefix, "pkg_version");
        _gameFileItems.AddRange(await GetAudioPkgVersionsAsync(prefix));
        foreach (var item in _gameFileItems)
        {
            _installItemQueue.Enqueue(item);
        }
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Verify);
        }
        await MoveAudioAssetsFromPersistentToStreamAssetsAsync();
        InstallTask = InstallGameTask.Repair;
        StartTask(InstallGameState.Verify);
    }



    public override async Task StartUpdateGameAsync(CancellationToken cancellationToken = default)
    {
        _gamePackage = await _packageService.GetGamePackageAsync(CurrentGameBiz);
        GamePackageResource resource;
        var localVersion = await _launcherService.GetLocalGameVersionAsync(CurrentGameBiz, _installPath);
        if (_gamePackage.Main.Patches.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource _resource_tmp)
        {
            resource = _resource_tmp;
        }
        else
        {
            resource = _gamePackage.Main.Major!;
        }
        await PrepareDownloadGamePackageResourceAsync(resource);
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Download);
        }
        await MoveAudioAssetsFromPersistentToStreamAssetsAsync();
        InstallTask = InstallGameTask.Update;
        StartTask(InstallGameState.Download);
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
        _hardLinkPath = linkInstallPath;
        _hardLinkGameBiz = linkGameBiz;
        var prefix = _gamePackage.Main.Major!.ResListUrl;
        var linkPrefix = linkPackage.Main.Major!.ResListUrl;
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new NotSupportedException($"Hard linking game ({CurrentGameBiz}) is not supported.");
        }
        if (Path.GetPathRoot(_installPath) != Path.GetPathRoot(linkInstallPath))
        {
            throw new NotSupportedException("Hard linking between different drives is not supported.");
        }
        _gameFileItems = await GetPkgVersionsAsync(prefix, "pkg_version");
        _gameFileItems.AddRange(await GetAudioPkgVersionsAsync(prefix));
        var linkGameFilesItem = await GetPkgVersionsAsync(linkPrefix, "pkg_version");
        linkGameFilesItem.AddRange(await GetAudioPkgVersionsAsync(linkPrefix));

        if (CurrentGameBiz.IsChinaServer() || CurrentGameBiz.IsBilibili())
        {
            foreach (var item in linkGameFilesItem)
            {
                item.Path = item.Path.Replace("GenshinImpact_Data", "YuanShen_Data");
            }
        }
        if (CurrentGameBiz.IsGlobalServer())
        {
            foreach (var item in linkGameFilesItem)
            {
                item.Path = item.Path.Replace("YuanShen_Data", "GenshinImpact_Data");
            }
        }
        var same = _gameFileItems.IntersectBy(linkGameFilesItem.Select(x => (x.Path, x.MD5)), x => (x.Path, x.MD5)).ToList();
        var diff = _gameFileItems.ExceptBy(linkGameFilesItem.Select(x => (x.Path, x.MD5)), x => (x.Path, x.MD5)).ToList();
        foreach (var item in same)
        {
            item.Type = InstallGameItemType.HardLink;
            item.HardLinkSource = Path.Combine(linkInstallPath, Path.GetRelativePath(_installPath, item.Path));
            if (linkGameBiz.IsChinaServer() || linkGameBiz.IsBilibili())
            {
                item.HardLinkSource = item.HardLinkSource.Replace("GenshinImpact_Data", "YuanShen_Data");
            }
            if (linkGameBiz.IsGlobalServer())
            {
                item.HardLinkSource = item.HardLinkSource.Replace("YuanShen_Data", "GenshinImpact_Data");
            }
            _installItemQueue.Enqueue(item);
        }
        foreach (var item in diff)
        {
            item.Type = InstallGameItemType.Verify;
            _installItemQueue.Enqueue(item);
        }
        if (CurrentGameBiz.IsBilibili())
        {
            await PrepareBilibiliChannelSDKAsync(InstallGameItemType.Verify);
        }
        InstallTask = InstallGameTask.HardLink;
        StartTask(InstallGameState.Verify);
    }



    private async Task<List<InstallGameItem>> GetAudioPkgVersionsAsync(string prefix)
    {
        string lang = await GetAudioLanguageAsync();
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = LanguageUtil.FilterAudioLanguage(CultureInfo.CurrentUICulture.Name);
            await SetAudioLanguageAsync(lang);
        }
        List<InstallGameItem> list = [];
        if (lang.Contains("zh-cn"))
        {
            list.AddRange(await GetPkgVersionsAsync(prefix, "Audio_Chinese_pkg_version"));
        }
        if (lang.Contains("en-us"))
        {
            list.AddRange(await GetPkgVersionsAsync(prefix, "Audio_English(US)_pkg_version"));
        }
        if (lang.Contains("ja-jp"))
        {
            list.AddRange(await GetPkgVersionsAsync(prefix, "Audio_Japanese_pkg_version"));
        }
        if (lang.Contains("ko-kr"))
        {
            list.AddRange(await GetPkgVersionsAsync(prefix, "Audio_Korean_pkg_version"));
        }
        return list;
    }




    protected async Task MoveAudioAssetsFromPersistentToStreamAssetsAsync()
    {
        string dataName = CurrentGameBiz.Value switch
        {
            GameBiz.hk4e_cn => "YuanShen_Data",
            GameBiz.hk4e_global => "GenshinImpact_Data",
            _ => "",
        };
        if (!string.IsNullOrWhiteSpace(dataName))
        {
            var source = Path.Combine(_installPath, $@"{dataName}\Persistent\AudioAssets");
            var target = Path.Combine(_installPath, $@"{dataName}\StreamingAssets\AudioAssets");
            if (Directory.Exists(source))
            {
                await Task.Run(() =>
                {
                    var files = Directory.GetFiles(source, "*", SearchOption.AllDirectories);
                    foreach (var file in files)
                    {
                        var relative = Path.GetRelativePath(source, file);
                        var dest = Path.Combine(target, relative);
                        Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
                        File.Move(file, dest, true);
                    }
                });
            }
        }
    }


}
