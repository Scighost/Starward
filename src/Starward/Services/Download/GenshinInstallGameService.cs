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
        _installTask = InstallGameTask.Repair;
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
        _installTask = InstallGameTask.Update;
        StartTask(InstallGameState.Download);
    }



    private async Task<List<InstallGameItem>> GetAudioPkgVersionsAsync(string prefix)
    {
        string lang = await GetAudioLanguageAsync();
        if (string.IsNullOrWhiteSpace(lang))
        {
            lang = LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name);
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
        string dataName = CurrentGameBiz switch
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
