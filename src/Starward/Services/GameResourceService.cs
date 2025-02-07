using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Launcher;
using Starward.Models;
using Starward.Services.Cache;
using Starward.Services.Launcher;
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


    private readonly HoYoPlayService _hoYoPlayService;


    private readonly GameLauncherService _gameLauncherService;


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
            if (!string.IsNullOrWhiteSpace(path) && AppConfig.GetGameInstallPathRemovable(biz))
            {
                return path;
            }
            else
            {
                AppConfig.SetGameInstallPath(biz, null);
                AppConfig.SetGameInstallPathRemovable(biz, false);
            }
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
        return biz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            GameBiz.clgm_cn => "Genshin Impact Cloud Game.exe",
            _ => biz.ToGame().Value switch
            {
                GameBiz.hkrpg => "StarRail.exe",
                GameBiz.bh3 => "BH3.exe",
                GameBiz.nap => "ZenlessZoneZero.exe",
                _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
            },
        };
    }



    public async Task<Version?> GetLocalGameVersionAsync(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }
        Version? version = null;
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = await File.ReadAllTextAsync(config);
            Version.TryParse(Regex.Match(str, @"game_version=(.+)").Groups[1].Value, out version);
        }
        else
        {
            _logger.LogWarning("config.ini not found: {path}", config);
        }
        return version;
    }


    public GameBiz GetLocalGameBiz(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return GameBiz.None;
        }
        if (biz == GameBiz.nap_cn)
        {
            return GameBiz.nap_cn;
        }
        GameBiz gameBiz = GameBiz.None;
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = File.ReadAllText(config);
            gameBiz = Regex.Match(str, @"game_biz=(.+)").Groups[1].Value;
        }
        else
        {
            _logger.LogWarning("config.ini not found: {path}", config);
        }
        return gameBiz;
    }



    public async Task<LauncherGameResource> GetGameResourceAsync(GameBiz biz)
    {
        var resource = MemoryCache.Instance.GetItem<LauncherGameResource>($"LauncherResource_{biz}", TimeSpan.FromSeconds(10));
        if (resource is null)
        {
            resource = await _launcherClient.GetLauncherGameResourceAsync(biz);
            MemoryCache.Instance.SetItem($"LauncherResource_{biz}", resource);
        }
        return resource;
    }



    public async Task<(Version? LatestVersion, Version? PreDownloadVersion)> GetGameResourceVersionAsync(GameBiz biz)
    {
        if (biz == GameBiz.nap_cn)
        {
            const string url = "https://hyp-api.mihoyo.com/hyp/hyp-connect/api/getGamePackages?game_ids%5B%5D=ol93169Cmh&launcher_id=PFKmM45gSW";
            var str = await _httpClient.GetStringAsync(url);
            var node = JsonNode.Parse(str);
            var versionStr = node?["data"]?["game_packages"]?[0]?["main"]?["major"]?["version"]?.ToString();
            Version.TryParse(versionStr, out Version? version);
            return (version, null);
        }
        else
        {

            var resource = await GetGameResourceAsync(biz);
            _ = Version.TryParse(resource.Game?.Latest?.Version, out Version? latest);
            _ = Version.TryParse(resource.PreDownloadGame?.Latest?.Version, out Version? preDownload);
            return (latest, preDownload);
        }
    }



    public async Task<bool> CheckPreDownloadIsOKAsync(GameBiz biz)
    {
        string? installPath = GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return false;
        }
        var resource = await GetGameResourceAsync(biz);
        if (resource.PreDownloadGame != null)
        {
            var localVersion = await GetLocalGameVersionAsync(biz, installPath);
            if (resource.PreDownloadGame.Diffs?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is DiffPackage diff)
            {
                string file = Path.Combine(installPath, diff.Name);
                if (!File.Exists(file))
                {
                    return false;
                }
                var flag = await GetVoiceLanguageAsync(biz, installPath);
                foreach (var lang in Enum.GetValues<AudioLanguage>())
                {
                    if (flag.HasFlag(lang))
                    {
                        if (diff.VoicePacks.FirstOrDefault(x => x.Language == lang.ToDescription()) is VoicePack pack)
                        {
                            file = Path.Combine(installPath, pack.Name);
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
                string file = Path.Combine(installPath, Path.GetFileName(resource.PreDownloadGame.Latest.Path));
                if (!File.Exists(file))
                {
                    return false;
                }
                var flag = await GetVoiceLanguageAsync(biz, installPath);
                foreach (var lang in Enum.GetValues<AudioLanguage>())
                {
                    if (flag.HasFlag(lang))
                    {
                        if (resource.PreDownloadGame.Latest.VoicePacks.FirstOrDefault(x => x.Language == lang.ToDescription()) is VoicePack pack)
                        {
                            file = Path.Combine(installPath, pack.Name);
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
        var localVersion = await GetLocalGameVersionAsync(biz, installPath);
        (Version? latestVersion, Version? preDownloadVersion) = await GetGameResourceVersionAsync(biz);
        var resource = await GetGameResourceAsync(biz);
        GameResource? gameResource = null;

        if (localVersion is null || reinstall)
        {
            gameResource = resource.Game;
        }
        else if (preDownloadVersion != null)
        {
            gameResource = resource.PreDownloadGame;
        }
        else if (latestVersion > localVersion)
        {
            gameResource = resource.Game;
        }


        if (gameResource != null)
        {
            var downloadGameResource = new DownloadGameResource();
            downloadGameResource.FreeSpace = new DriveInfo(Path.GetPathRoot(Path.GetFullPath(installPath))!).AvailableFreeSpace;
            if (gameResource.Diffs?.FirstOrDefault(x => x.Version == localVersion?.ToString()) is DiffPackage diff)
            {
                downloadGameResource.Game = CheckDownloadPackage(diff, installPath);
                foreach (var pack in diff.VoicePacks)
                {
                    var state = CheckDownloadPackage(pack, installPath);
                    state.Name = pack.Language;
                    downloadGameResource.Voices.Add(state);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(gameResource.Latest.Path))
                {
                    var state = new DownloadPackageState
                    {
                        PackageSize = gameResource.Latest.PackageSize,
                        DecompressedSize = gameResource.Latest.Size,
                    };
                    var size = gameResource.Latest.Segments.Sum(x => CheckDownloadPackage(Path.GetFileName(x.Path), installPath));
                    state.DownloadedSize = size;
                    downloadGameResource.Game = state;
                }
                else
                {
                    downloadGameResource.Game = CheckDownloadPackage(gameResource.Latest, installPath);
                }
                foreach (var pack in gameResource.Latest.VoicePacks)
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
            Name = Path.GetFileName(package.Path),
            Url = package.Path,
            PackageSize = package.PackageSize,
            DecompressedSize = package.Size,
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



    public async Task<AudioLanguage> GetVoiceLanguageAsync(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return AudioLanguage.None;
        }
        var file = biz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
            GameBiz.hk4e_global => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, @"StarRail_Data\Persistent\AudioLaucherRecord.txt"),
            _ => ""
        };
        if (!File.Exists(file))
        {
            file = biz.Value switch
            {
                GameBiz.hk4e_global => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
                GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
                GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, @"StarRail_Data\Persistent\AudioLaucherRecord.txt"),
                _ => ""
            };
        }
        var flag = AudioLanguage.None;
        if (File.Exists(file))
        {
            var lines = await File.ReadAllLinesAsync(file);
            if (lines.Any(x => x.Contains("Chinese"))) { flag |= AudioLanguage.Chinese; }
            if (lines.Any(x => x.Contains("English(US)"))) { flag |= AudioLanguage.English; }
            if (lines.Any(x => x.Contains("Japanese"))) { flag |= AudioLanguage.Japanese; }
            if (lines.Any(x => x.Contains("Korean"))) { flag |= AudioLanguage.Korean; }
        }
        return flag;
    }



    public async Task SetVoiceLanguageAsync(GameBiz biz, string installPath, AudioLanguage lang)
    {
        if (biz.Value is GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hkrpg_cn or GameBiz.hkrpg_global)
        {
            var file = biz.Value switch
            {
                GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => Path.Join(installPath, @"YuanShen_Data\Persistent\audio_lang_14"),
                GameBiz.hk4e_global => Path.Join(installPath, @"GenshinImpact_Data\Persistent\audio_lang_14"),
                GameBiz.hkrpg_cn or GameBiz.hkrpg_global or GameBiz.hkrpg_bilibili => Path.Join(installPath, @"StarRail_Data\Persistent\AudioLaucherRecord.txt"),
                _ => ""
            };
            Directory.CreateDirectory(Path.GetDirectoryName(file)!);
            var lines = new List<string>(4);
            if (lang.HasFlag(AudioLanguage.Chinese)) { lines.Add("Chinese"); }
            if (lang.HasFlag(AudioLanguage.English)) { lines.Add("English(US)"); }
            if (lang.HasFlag(AudioLanguage.Japanese)) { lines.Add("Japanese"); }
            if (lang.HasFlag(AudioLanguage.Korean)) { lines.Add("Korean"); }
            await File.WriteAllLinesAsync(file, lines);
        }
    }





}
