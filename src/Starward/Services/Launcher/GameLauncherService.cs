using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.HoYoPlay;
using Starward.Core.Launcher;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Starward.Services.Launcher;

internal class GameLauncherService
{


    private readonly ILogger<GameLauncherService> _logger;


    private readonly HoYoPlayService _hoYoPlayService;


    private readonly LauncherClient _launcherClient;



    public GameLauncherService(ILogger<GameLauncherService> logger, HoYoPlayService hoYoPlayService, LauncherClient launcherClient)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _launcherClient = launcherClient;
    }






    /// <summary>
    /// 游戏内公告网页URL
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="uid"></param>
    /// <returns></returns>
    //public Task<string> GetGameNoticesWebURLAsync(GameBiz gameBiz, long uid)
    //{

    //}


    /// <summary>
    /// 是否显示公告提醒
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="uid"></param>
    /// <returns></returns>
    //public Task<bool> IsGameNoticesAlertAsync(GameBiz gameBiz, long uid)
    //{

    //}




    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public string? GetGameInstallPath(GameBiz gameBiz)
    {
        var path = AppConfig.GetGameInstallPath(gameBiz);
        if (string.IsNullOrWhiteSpace(path))
        {
            path = GetDefaultGameInstallPath(gameBiz);
        }
        if (Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(path) && AppConfig.GetGameInstallPathRemovable(gameBiz))
            {
                return path;
            }
            else
            {
                AppConfig.SetGameInstallPath(gameBiz, null);
                AppConfig.SetGameInstallPathRemovable(gameBiz, false);
            }
            return null;
        }
    }



    private string? GetDefaultGameInstallPath(GameBiz gameBiz)
    {
        if (gameBiz.IsChinaServer())
        {
            return Registry.GetValue($@"HKEY_CURRENT_USER\Software\miHoYo\HYP\1_1\{gameBiz}", "GameInstallPath", null) as string;
        }
        else if (gameBiz.IsGlobalServer())
        {
            if (gameBiz.ToGame() == GameBiz.bh3)
            {
                return GachaLogClient.GetGameInstallPathFromRegistry(gameBiz);
            }
            else
            {
                return Registry.GetValue($@"HKEY_CURRENT_USER\Software\Cognosphere\HYP\1_0\{gameBiz}", "GameInstallPath", null) as string;
            }
        }
        else if (gameBiz.IsBilibili())
        {
            return Registry.GetValue($@"HKEY_CURRENT_USER\Software\miHoYo\HYP\standalone\14_0\{gameBiz}\{LauncherId.FromGameBiz(gameBiz)}\{gameBiz}", "GameInstallPath", null) as string;
        }
        else if (gameBiz.IsChinaCloud())
        {
            return GachaLogClient.GetGameInstallPathFromRegistry(gameBiz);
        }
        else
        {
            return null;
        }
    }



    /// <summary>
    /// 最新游戏版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<Version?> GetLatestGameVersionAsync(GameBiz gameBiz)
    {
        if (gameBiz.IsGlobalServer() && gameBiz.ToGame() == GameBiz.bh3)
        {
            var resource = await _launcherClient.GetLauncherGameResourceAsync(gameBiz);
            return TryParseVersion(resource.Game.Latest.Version);
        }
        else if (gameBiz.IsChinaServer() || gameBiz.IsGlobalServer() || gameBiz.IsBilibili())
        {
            var package = await _hoYoPlayService.GetGamePackageAsync(gameBiz);
            return TryParseVersion(package.Main.Major?.Version);
        }
        else
        {
            return null;
        }
    }


    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(GameBiz gameBiz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(gameBiz);
        if (gameBiz == GameBiz.clgm_cn)
        {
            var exe = Path.Join(installPath, GetGameExeName(gameBiz));
            if (File.Exists(exe))
            {
                return TryParseVersion(FileVersionInfo.GetVersionInfo(exe).ProductVersion);
            }
            else
            {
                return null;
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(installPath))
            {
                return null;
            }
            var config = Path.Join(installPath, "config.ini");
            if (File.Exists(config))
            {
                var str = await File.ReadAllTextAsync(config);
                return TryParseVersion(Regex.Match(str, @"game_version=(.+)").Groups[1].Value);
            }
            else
            {
                _logger.LogWarning("config.ini not found: {path}", config);
                return null;
            }
        }
    }


    /// <summary>
    /// 硬链接信息
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<(GameBiz, string?)> GetHardLinkInfoAsync(GameBiz gameBiz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(gameBiz);
        if (gameBiz == GameBiz.clgm_cn)
        {
            return (GameBiz.None, null);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(installPath))
            {
                return (GameBiz.None, null);
            }
            var config = Path.Join(installPath, "config.ini");
            if (File.Exists(config))
            {
                var str = await File.ReadAllTextAsync(config);
                GameBiz biz = Regex.Match(str, @"hardlink_gamebiz=(.+)").Groups[1].Value;
                var path = Regex.Match(str, @"hardlink_path=(.+)").Groups[1].Value;
                return (biz, path);
            }
            else
            {
                _logger.LogWarning("config.ini not found: {path}", config);
                return (GameBiz.None, null);
            }
        }
    }


    /// <summary>
    /// 预下载版本
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public async Task<Version?> GetPreDownloadGameVersionAsync(GameBiz gameBiz)
    {
        if (gameBiz.IsGlobalServer() && gameBiz.ToGame() == GameBiz.bh3)
        {
            var resource = await _launcherClient.GetLauncherGameResourceAsync(gameBiz);
            return TryParseVersion(resource.PreDownloadGame?.Latest.Version);
        }
        else if (gameBiz.IsChinaServer() || gameBiz.IsGlobalServer() || gameBiz.IsBilibili())
        {
            var package = await _hoYoPlayService.GetGamePackageAsync(gameBiz);
            return TryParseVersion(package.PreDownload?.Major?.Version);
        }
        else
        {
            return null;
        }
    }



    private static Version? TryParseVersion(string? version)
    {
        if (Version.TryParse(version, out var result))
        {
            return result;
        }
        else
        {
            return null;
        }
    }



    /// <summary>
    /// 游戏进程名，带 .exe 扩展名
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <returns></returns>
    public string GetGameExeName(GameBiz gameBiz)
    {
        return gameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            GameBiz.clgm_cn => "Genshin Impact Cloud Game.exe",
            _ => gameBiz.ToGame().Value switch
            {
                GameBiz.hkrpg => "StarRail.exe",
                GameBiz.bh3 => "BH3.exe",
                GameBiz.nap => "ZenlessZoneZero.exe",
                _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
            },
        };
    }


    /// <summary>
    /// 游戏进程文件是否存在
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public bool IsGameExeExists(GameBiz biz, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(biz);
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            var exe = Path.Join(installPath, GetGameExeName(biz));
            return File.Exists(exe);
        }
        return false;
    }



    /// <summary>
    /// 获取游戏进程
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public Process? GetGameProcess(GameBiz biz)
    {
        int currentSessionId = Process.GetCurrentProcess().SessionId;
        var name = GetGameExeName(biz).Replace(".exe", "");
        return Process.GetProcessesByName(name).Where(x => x.SessionId == currentSessionId).FirstOrDefault();
    }



    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public Process? StartGame(GameBiz biz, bool ignoreRunningGame = false, string? installPath = null)
    {
        const int ERROR_CANCELLED = 0x000004C7;
        try
        {
            if (!ignoreRunningGame)
            {
                if (GetGameProcess(biz) != null)
                {
                    throw new Exception("Game process is running.");
                }
            }
            string? exe = null, arg = null, verb = null;
            if (Directory.Exists(installPath))
            {
                var e = Path.Join(installPath, GetGameExeName(biz));
                if (File.Exists(e))
                {
                    exe = e;
                }
            }
            if (string.IsNullOrWhiteSpace(exe) && AppConfig.GetEnableThirdPartyTool(biz))
            {
                exe = AppConfig.GetThirdPartyToolPath(biz);
                if (File.Exists(exe))
                {
                    verb = Path.GetExtension(exe) is ".exe" or ".bat" ? "runas" : "";
                }
                else
                {
                    exe = null;
                    AppConfig.SetThirdPartyToolPath(biz, null);
                    _logger.LogWarning("Third party tool not found: {path}", exe);
                }
            }
            if (string.IsNullOrWhiteSpace(exe))
            {
                var folder = GetGameInstallPath(biz);
                var name = GetGameExeName(biz);
                exe = Path.Join(folder, name);
                arg = AppConfig.GetStartArgument(biz)?.Trim();
                verb = (biz == GameBiz.clgm_cn) ? "" : "runas";
                if (!File.Exists(exe))
                {
                    _logger.LogWarning("Game exe not found: {path}", exe);
                    throw new FileNotFoundException("Game exe not found", name);
                }
            }
            _logger.LogInformation("Start game ({biz})\r\npath: {exe}\r\narg: {arg}", biz, exe, arg);
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arg,
                UseShellExecute = true,
                Verb = verb,
                WorkingDirectory = Path.GetDirectoryName(exe),
            };
            return Process.Start(info);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            // Operation canceled
            _logger.LogInformation("Start game operation canceled.");
        }
        return null;
    }


}
