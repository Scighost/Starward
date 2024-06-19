using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Launcher;
using Starward.Models;
using Starward.Services.Cache;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Starward.Services;

internal class GameResourceService
{


    private readonly ILogger<GameResourceService> _logger;


    private readonly LauncherClient _launcherClient;


    private readonly HttpClient _httpClient;


    public GameResourceService(ILogger<GameResourceService> logger, LauncherClient launcherClient, HttpClient httpClient)
    {
        _logger = logger;
        _launcherClient = launcherClient;
        _httpClient = httpClient;
    }







    public string? GetGameInstallPath(GameBiz biz)
    {
        var path = AppConfig.GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(path))
        {
            path = GachaLogClient.GetGameInstallPathFromRegistry(biz);
        }
        if (Directory.Exists(path))
        {
            return path;
        }
        else
        {
            AppConfig.SetGameInstallPath(biz, null);
            return null;
        }
    }





    public bool IsGameExeExists(GameBiz biz)
    {
        var path = GetGameInstallPath(biz);
        if (path != null)
        {
            var exe = Path.Combine(path, GetGameExeName(biz));
            return File.Exists(exe);
        }
        return false;
    }



    public static string GetGameExeName(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            GameBiz.hk4e_cloud => "Genshin Impact Cloud Game.exe",
            GameBiz.nap_cn => "ZZZ.exe",
            _ => biz.ToGame() switch
            {
                GameBiz.StarRail => "StarRail.exe",
                GameBiz.Honkai3rd => "BH3.exe",
                _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
            },
        };
    }



    public async Task<(Version?, GameBiz)> GetLocalGameVersionAndBizAsync(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return (null, GameBiz.None);
        }
        Version? version = null;
        GameBiz gameBiz = GameBiz.None;
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = await File.ReadAllTextAsync(config);
            Version.TryParse(Regex.Match(str, @"game_version=(.+)").Groups[1].Value, out version);
            Enum.TryParse(Regex.Match(str, @"game_biz=(.+)").Groups[1].Value, out gameBiz);
        }
        else
        {
            _logger.LogWarning("config.ini not found: {path}", config);
        }
        return (version, gameBiz);
    }


    public GameBiz GetLocalGameBiz(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return GameBiz.None;
        }
        if (biz is GameBiz.nap_cn)
        {
            return GameBiz.nap_cn;
        }
        GameBiz gameBiz = GameBiz.None;
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = File.ReadAllText(config);
            Enum.TryParse(Regex.Match(str, @"game_biz=(.+)").Groups[1].Value, out gameBiz);
        }
        else
        {
            _logger.LogWarning("config.ini not found: {path}", config);
        }
        return gameBiz;
    }



    public async Task<GamePackagesWrapper> GetGameResourceAsync(GameBiz biz)
    {
        var resource = MemoryCache.Instance.GetItem<GamePackagesWrapper>($"LauncherResource_{biz}", TimeSpan.FromSeconds(10));
        if (resource is null)
        {
            resource = await _launcherClient.GetLauncherGameResourceAsync(biz);
            MemoryCache.Instance.SetItem($"LauncherResource_{biz}", resource);
        }
        return resource;
    }


    public async Task<GameSDK?> GetGameSdkAsync(GameBiz biz)
    {
        var resource = MemoryCache.Instance.GetItem<GameSDK>($"LauncherSdk_{biz}", TimeSpan.FromSeconds(10));
        if (resource is null)
        {
            resource = await _launcherClient.GetLauncherGameSdkAsync(biz);
            MemoryCache.Instance.SetItem($"LauncherSdk_{biz}", resource);
        }
        return resource;
    }



    public async Task<(Version? LatestVersion, Version? PreDownloadVersion)> GetGameResourceVersionAsync(GameBiz biz)
    {
        /*if (biz is GameBiz.nap_cn)
        {
            const string url = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?game_ids%5B%5D=ol93169Cmh&launcher_id=PFKmM45gSW";
            var str = await _httpClient.GetStringAsync(url);
            var node = JsonNode.Parse(str);
            var versionStr = node?["data"]?["game_packages"]?[0]?["main"]?["major"]?["version"]?.ToString();
            Version.TryParse(versionStr, out Version? version);
            return (version, null);
        }
        else
        {*/

            var resource = await GetGameResourceAsync(biz);
            _ = Version.TryParse(resource.Main?.Major?.Version, out Version? latest);
            _ = Version.TryParse(resource.PreDownload?.Major?.Version, out Version? preDownload);
            return (latest, preDownload);
        //}
    }



    public async Task<bool> CheckPreDownloadIsOKAsync(GameBiz biz)
    {
        string? installPath = GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }
        var resource = await GetGameResourceAsync(biz);
        if (resource.PreDownload.Major != null)
        {
            (var localVersion, _) = await GetLocalGameVersionAndBizAsync(biz, installPath);
            if (resource.PreDownload.Patches?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackages diff)
            {
                string file = Path.Combine(installPath, Path.GetFileName(diff.GamePkgs.First().Url));
                if (!File.Exists(file))
                {
                    return false;
                }
                var flag = await GetVoiceLanguageAsync(biz, installPath);
                foreach (var lang in Enum.GetValues<VoiceLanguage>())
                {
                    if (flag.HasFlag(lang))
                    {
                        if (diff.AudioPkgs.FirstOrDefault(x => x.Language == lang.ToDescription()) is AudioPkg pack)
                        {
                            file = Path.Combine(installPath, Path.GetFileName(pack.Url));
                            if (!File.Exists(file))
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
            else
            {
                string file = Path.Combine(installPath, Path.GetFileName(resource.PreDownload.Major.GamePkgs.First().Url));
                if (!File.Exists(file))
                {
                    return false;
                }
                var flag = await GetVoiceLanguageAsync(biz, installPath);
                foreach (var lang in Enum.GetValues<VoiceLanguage>())
                {
                    if (flag.HasFlag(lang))
                    {
                        if (resource.PreDownload.Major.AudioPkgs.FirstOrDefault(x => x.Language == lang.ToDescription()) is AudioPkg pack)
                        {
                            file = Path.Combine(installPath, Path.GetFileName(pack.Url));
                            if (!File.Exists(file))
                            {
                                return false;
                            }
                        }
                    }
                }
                return true;
            }
        }
        return false;
    }



    public async Task<DownloadGameResource?> CheckDownloadGameResourceAsync(GameBiz biz, string installPath, bool reinstall = false)
    {
        (var localVersion, _) = await GetLocalGameVersionAndBizAsync(biz, installPath);
        (Version? latestVersion, Version? preDownloadVersion) = await GetGameResourceVersionAsync(biz);
        var resource = await GetGameResourceAsync(biz);
        GameBranch? gameResource = null;

        if (localVersion is null || reinstall)
        {
            gameResource = resource.Main;
        }
        else if (preDownloadVersion != null)
        {
            gameResource = resource.PreDownload;
        }
        else if (latestVersion > localVersion)
        {
            gameResource = resource.Main;
        }


        if (gameResource != null)
        {
            var downloadGameResource = new DownloadGameResource();
            downloadGameResource.FreeSpace = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(installPath))!).AvailableFreeSpace;
            if (gameResource.Patches?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is GamePackages diff)
            {
                downloadGameResource.Game = CheckDownloadPackage(diff.GamePkgs.First(), installPath);
                foreach (var pack in diff.AudioPkgs)
                {
                    var state = CheckDownloadPackage(pack, installPath);
                    state.Name = pack.Language;
                    downloadGameResource.Voices.Add(state);
                }
            }
            else
            {
                if (gameResource.Major.GamePkgs.Count >= 1)
                {
                    var state = new DownloadPackageState
                    {
                        PackageSize = gameResource.Major.GamePkgs.Sum(x => x.Size),
                        DecompressedSize = gameResource.Major.GamePkgs.Sum(x => x.DecompressedSize),
                    };
                    var size = gameResource.Major.GamePkgs.Sum(x => CheckDownloadPackage(Path.GetFileName(x.Url), installPath));
                    state.DownloadedSize = size;
                    downloadGameResource.Game = state;
                }
                /*else
                {
                    downloadGameResource.Game = CheckDownloadPackage(gameResource.Major.GamePkgs.FirstOrDefault(), installPath);
                }*/
                foreach (var pack in gameResource.Major.AudioPkgs)
                {
                    var state = CheckDownloadPackage(pack, installPath);
                    state.Name = pack.Language;
                    downloadGameResource.Voices.Add(state);
                }
            }
            return downloadGameResource;
        }

        return null;

    }


    private DownloadPackageState CheckDownloadPackage(IGamePackage package, string installPath)
    {
        var state = new DownloadPackageState
        {
            Name = Path.GetFileName(package.Url),
            Url = package.Url,
            PackageSize = package.Size,
            DecompressedSize = package.DecompressedSize,
        };
        string file = Path.Join(installPath, state.Name);
        string file_tmp = file + "_tmp";
        if (File.Exists(file))
        {
            state.DownloadedSize = new FileInfo(file).Length;
        }
        else if (File.Exists(file_tmp))
        {
            state.DownloadedSize = new FileInfo(file_tmp).Length;
        }
        return state;
    }


    private long CheckDownloadPackage(string name, string installPath)
    {
        string file = Path.Join(installPath, name);
        string file_tmp = file + "_tmp";
        if (File.Exists(file))
        {
            return new FileInfo(file).Length;
        }
        else if (File.Exists(file_tmp))
        {
            return new FileInfo(file_tmp).Length;
        }
        else
        {
            return 0;
        }
    }



    public async Task<VoiceLanguage> GetVoiceLanguageAsync(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return VoiceLanguage.None;
        }
        var file = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
            GameBiz.hk4e_global => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, @"StarRail_Data\Persistent\AudioLaucherRecord.txt"),
            _ => ""
        };
        if (!File.Exists(file))
        {
            file = biz switch
            {
                GameBiz.hk4e_global => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
                GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
                GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, @"StarRail_Data\Persistent\AudioLaucherRecord.txt"),
                _ => ""
            };
        }
        var flag = VoiceLanguage.None;
        if (File.Exists(file))
        {
            var lines = await File.ReadAllLinesAsync(file);
            if (lines.Any(x => x.Contains("Chinese"))) { flag |= VoiceLanguage.Chinese; }
            if (lines.Any(x => x.Contains("English(US)"))) { flag |= VoiceLanguage.English; }
            if (lines.Any(x => x.Contains("Japanese"))) { flag |= VoiceLanguage.Japanese; }
            if (lines.Any(x => x.Contains("Korean"))) { flag |= VoiceLanguage.Korean; }
        }
        return flag;
    }



    public async Task SetVoiceLanguageAsync(GameBiz biz, string installPath, VoiceLanguage lang)
    {
        if (biz is GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hkrpg_cn or GameBiz.hkrpg_global)
        {
            var file = biz switch
            {
                GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
                GameBiz.hk4e_global => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
                GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, @"StarRail_Data\Persistent\AudioLaucherRecord.txt"),
                _ => ""
            };
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var lines = new List<string>(4);
            if (lang.HasFlag(VoiceLanguage.Chinese)) { lines.Add("Chinese"); }
            if (lang.HasFlag(VoiceLanguage.English)) { lines.Add("English(US)"); }
            if (lang.HasFlag(VoiceLanguage.Japanese)) { lines.Add("Japanese"); }
            if (lang.HasFlag(VoiceLanguage.Korean)) { lines.Add("Korean"); }
            await File.WriteAllLinesAsync(file, lines);
        }
    }





}
