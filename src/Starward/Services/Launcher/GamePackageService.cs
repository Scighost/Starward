using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Core.Launcher;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Starward.Services.Launcher;

internal class GamePackageService
{


    private readonly ILogger<GamePackageService> _logger;

    private readonly LauncherClient _launcherClient;

    private readonly HoYoPlayService _hoYoPlayService;

    private readonly GameLauncherService _gameLauncherService;



    public GamePackageService(ILogger<GamePackageService> logger, LauncherClient launcherClient, HoYoPlayService hoYoPlayService, GameLauncherService gameLauncherService)
    {
        _logger = logger;
        _launcherClient = launcherClient;
        _hoYoPlayService = hoYoPlayService;
        _gameLauncherService = gameLauncherService;
    }





    public async Task<GamePackage> GetGamePackageAsync(GameBiz biz)
    {
        return await _hoYoPlayService.GetGamePackageAsync(biz);
    }





    public async Task<bool> CheckPreDownloadIsOKAsync(GameBiz biz, string? installPath = null)
    {
        installPath ??= _gameLauncherService.GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }
        var package = await GetGamePackageAsync(biz);
        if (package.PreDownload?.Major != null)
        {
            var localVersion = await _gameLauncherService.GetLocalGameVersionAsync(biz, installPath);
            VoiceLanguage language = await GetVoiceLanguageAsync(biz, installPath);
            if (package.PreDownload.Patches?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackageResource resource)
            {
                return CheckGamePackageResourceIsDownloadOK(resource, installPath, language);
            }
            else
            {
                return CheckGamePackageResourceIsDownloadOK(package.PreDownload.Major, installPath, language);
            }
        }
        return false;
    }





    public async Task<GamePackageResource?> GetNeedDownloadGamePackageResourceAsync(GameBiz biz, string? installPath = null)
    {
        installPath ??= _gameLauncherService.GetGameInstallPath(biz);
        Version? localVersion = await _gameLauncherService.GetLocalGameVersionAsync(biz, installPath);
        Version? latestVersion = await _gameLauncherService.GetLatestGameVersionAsync(biz);
        if (latestVersion is null)
        {
            return null;
        }
        GamePackage package = await GetGamePackageAsync(biz);
        if (localVersion is null)
        {
            return package.Main.Major;
        }
        else if (localVersion < latestVersion)
        {
            if (package.Main.Patches.FirstOrDefault(x => x.Version == localVersion.ToString()) is GamePackageResource resource)
            {
                return resource;
            }
            else
            {
                return package.Main.Major;
            }
        }
        else if (package.PreDownload is not null)
        {
            if (package.PreDownload.Patches.FirstOrDefault(x => x.Version == localVersion.ToString()) is GamePackageResource resource)
            {
                return resource;
            }
            else
            {
                return package.PreDownload.Major;
            }
        }
        return null;
    }





    private static bool CheckGamePackageResourceIsDownloadOK(GamePackageResource resource, string installPath, VoiceLanguage language)
    {
        foreach (var item in resource.GamePackages)
        {
            string file = Path.Combine(installPath, Path.GetFileName(item.Url));
            if (!File.Exists(file))
            {
                return false;
            }
        }
        foreach (var lang in Enum.GetValues<VoiceLanguage>())
        {
            if (language.HasFlag(lang))
            {
                if (resource.AudioPackages.FirstOrDefault(x => x.Language == lang.ToDescription()) is GamePackageFile packageFile)
                {
                    string file = Path.Combine(installPath, Path.GetFileName(packageFile.Url));
                    if (!File.Exists(file))
                    {
                        return false;
                    }
                }
            }
        }
        return true;
    }




    private static long GetFileDownloadedLength(string file)
    {
        if (File.Exists(file))
        {
            return new FileInfo(file).Length;
        }
        else if (File.Exists(file + "_tmp"))
        {
            return new FileInfo(file + "_tmp").Length;
        }
        return 0;
    }




    public async Task<VoiceLanguage> GetVoiceLanguageAsync(GameBiz biz, string? installPath = null)
    {
        if (biz.ToGame() is GameBiz.Honkai3rd)
        {
            return VoiceLanguage.None;
        }
        installPath ??= _gameLauncherService.GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return VoiceLanguage.None;
        }
        VoiceLanguage flag = VoiceLanguage.None;
        var config = await _hoYoPlayService.GetGameConfigAsync(biz);
        if (!string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
        {
            string file = Path.Join(installPath, config.AudioPackageScanDir);
            if (File.Exists(file))
            {
                var lines = await File.ReadAllLinesAsync(file);
                if (lines.Any(x => x.Contains("Chinese"))) { flag |= VoiceLanguage.Chinese; }
                if (lines.Any(x => x.Contains("English(US)"))) { flag |= VoiceLanguage.English; }
                if (lines.Any(x => x.Contains("Japanese"))) { flag |= VoiceLanguage.Japanese; }
                if (lines.Any(x => x.Contains("Korean"))) { flag |= VoiceLanguage.Korean; }
            }
        }
        return flag;
    }




    public async Task SetVoiceLanguageAsync(GameBiz biz, string installPath, VoiceLanguage lang)
    {
        if (biz.ToGame() is GameBiz.Honkai3rd)
        {
            return;
        }
        var config = await _hoYoPlayService.GetGameConfigAsync(biz);
        if (!string.IsNullOrWhiteSpace(config?.AudioPackageScanDir))
        {
            string file = Path.Join(installPath, config.AudioPackageScanDir);
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var lines = new List<string>(4);
            if (lang.HasFlag(VoiceLanguage.Chinese)) { lines.Add("Chinese"); }
            if (lang.HasFlag(VoiceLanguage.English)) { lines.Add("English(US)"); }
            if (lang.HasFlag(VoiceLanguage.Japanese)) { lines.Add("Japanese"); }
            if (lang.HasFlag(VoiceLanguage.Korean)) { lines.Add("Korean"); }
            await File.WriteAllLinesAsync(file, lines);
        }
    }




    public DownloadGameResource GetDownloadGameResourceAsync(GamePackageResource resource, string installPath)
    {
        var downloadResource = new DownloadGameResource
        {
            Game = new DownloadPackageState
            {
                Name = resource.Version,
                Url = resource.GamePackages[0].Url,
                PackageSize = resource.GamePackages.Sum(x => x.Size),
                DecompressedSize = resource.GamePackages.Sum(x => x.DecompressedSize),
                DownloadedSize = resource.GamePackages.Sum(x => GetFileDownloadedLength(Path.Combine(installPath, Path.GetFileName(x.Url)))),
            },
            FreeSpace = new DriveInfo(Path.GetFullPath(installPath)).AvailableFreeSpace,
        };
        foreach (var item in resource.AudioPackages)
        {
            downloadResource.Voices.Add(new DownloadPackageState
            {
                Name = item.Language!,
                Url = item.Url,
                PackageSize = item.Size,
                DecompressedSize = item.DecompressedSize,
                DownloadedSize = GetFileDownloadedLength(Path.Combine(installPath, Path.GetFileName(item.Url))),
            });
        }
        return downloadResource;
    }





}
