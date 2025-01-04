using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Features.PlayTime;
using Starward.Frameworks;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Starward.Features.GameLauncher;

internal class GameLauncherService
{


    private readonly ILogger<GameLauncherService> _logger;


    private readonly HoYoPlayService _hoYoPlayService;

    private readonly PlayTimeService _playTimeService;


    public GameLauncherService(ILogger<GameLauncherService> logger, HoYoPlayService hoYoPlayService, PlayTimeService playTimeService)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
        _playTimeService = playTimeService;
    }





    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public string? GetGameInstallPath(GameId gameId)
    {
        var path = AppSetting.GetGameInstallPath(gameId.GameBiz);
        if (Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }
        else if (!string.IsNullOrWhiteSpace(path) && AppSetting.GetGameInstallPathRemovable(gameId.GameBiz))
        {
            return path;
        }
        else
        {
            AppSetting.SetGameInstallPath(gameId.GameBiz, null);
            AppSetting.SetGameInstallPathRemovable(gameId.GameBiz, false);
            return null;
        }
    }



    /// <summary>
    /// 游戏安装目录，为空时未找到
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="storageRemoved">可移动存储设备已移除</param>
    /// <returns></returns>
    public string? GetGameInstallPath(GameId gameId, out bool storageRemoved)
    {
        storageRemoved = false;
        var path = AppSetting.GetGameInstallPath(gameId.GameBiz);
        if (Directory.Exists(path))
        {
            return Path.GetFullPath(path);
        }
        else if (!string.IsNullOrWhiteSpace(path) && AppSetting.GetGameInstallPathRemovable(gameId.GameBiz))
        {
            storageRemoved = true;
            return path;
        }
        else
        {
            AppSetting.SetGameInstallPath(gameId.GameBiz, null);
            AppSetting.SetGameInstallPathRemovable(gameId.GameBiz, false);
            return null;
        }
    }





    /// <summary>
    /// 本地游戏版本
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<Version?> GetLocalGameVersionAsync(GameId gameId, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(gameId);
        if (string.IsNullOrWhiteSpace(installPath))
        {
            return null;
        }
        var config = Path.Join(installPath, "config.ini");
        if (File.Exists(config))
        {
            var str = await File.ReadAllTextAsync(config);
            _ = Version.TryParse(Regex.Match(str, @"game_version=(.+)").Groups[1].Value, out Version? version);
            return version;
        }
        else
        {
            _logger.LogWarning("config.ini not found: {path}", config);
            return null;
        }
    }





    /// <summary>
    /// 游戏进程名，带 .exe 扩展名
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<string> GetGameExeNameAsync(GameId gameId)
    {
        string? name = gameId.GameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            _ => gameId.GameBiz.Game switch
            {
                GameBiz.hkrpg => "StarRail.exe",
                GameBiz.bh3 => "BH3.exe",
                GameBiz.nap => "ZenlessZoneZero.exe",
                _ => null,
            },
        };
        if (string.IsNullOrWhiteSpace(name))
        {
            var config = await _hoYoPlayService.GetGameConfigAsync(gameId);
            name = config?.ExeFileName;
        }
        return name ?? throw new ArgumentOutOfRangeException($"Unknown game ({gameId.Id}, {gameId.GameBiz}).");
    }



    /// <summary>
    /// 游戏进程文件是否存在
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="installPath"></param>
    /// <returns></returns>
    public async Task<bool> IsGameExeExistsAsync(GameId gameId, string? installPath = null)
    {
        installPath ??= GetGameInstallPath(gameId);
        if (!string.IsNullOrWhiteSpace(installPath))
        {
            var exe = Path.Join(installPath, await GetGameExeNameAsync(gameId));
            return File.Exists(exe);
        }
        return false;
    }



    /// <summary>
    /// 获取游戏进程
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<Process?> GetGameProcessAsync(GameId gameId)
    {
        int currentSessionId = Process.GetCurrentProcess().SessionId;
        var name = (await GetGameExeNameAsync(gameId)).Replace(".exe", "");
        return Process.GetProcessesByName(name).Where(x => x.SessionId == currentSessionId).FirstOrDefault();
    }



    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public async Task<Process?> StartGameAsync(GameId gameId, bool ignoreRunningGame = false, string? installPath = null)
    {
        const int ERROR_CANCELLED = 0x000004C7;
        try
        {
            if (!ignoreRunningGame)
            {
                if (await GetGameProcessAsync(gameId) != null)
                {
                    throw new Exception("Game process is running.");
                }
            }
            string? exe = null, arg = null, verb = null;
            if (Directory.Exists(installPath))
            {
                var e = Path.Join(installPath, await GetGameExeNameAsync(gameId));
                if (File.Exists(e))
                {
                    exe = e;
                }
            }
            bool thirdPartyTool = false;
            if (string.IsNullOrWhiteSpace(exe) && AppSetting.GetEnableThirdPartyTool(gameId.GameBiz))
            {
                exe = AppSetting.GetThirdPartyToolPath(gameId.GameBiz);
                if (File.Exists(exe))
                {
                    thirdPartyTool = true;
                    verb = Path.GetExtension(exe) is ".exe" or ".bat" ? "runas" : "";
                }
                else
                {
                    exe = null;
                    AppSetting.SetThirdPartyToolPath(gameId.GameBiz, null);
                    _logger.LogWarning("Third party tool not found: {path}", exe);
                }
            }
            if (string.IsNullOrWhiteSpace(exe))
            {
                var folder = GetGameInstallPath(gameId);
                var name = await GetGameExeNameAsync(gameId);
                exe = Path.Join(folder, name);
                arg = AppSetting.GetStartArgument(gameId.GameBiz)?.Trim();
                verb = "runas";
                if (!File.Exists(exe))
                {
                    _logger.LogWarning("Game exe not found: {path}", exe);
                    throw new FileNotFoundException("Game exe not found", name);
                }
            }
            _logger.LogInformation("Start game ({biz})\r\npath: {exe}\r\narg: {arg}", gameId, exe, arg);
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arg,
                UseShellExecute = true,
                Verb = verb,
                WorkingDirectory = Path.GetDirectoryName(exe),
            };
            Process? process = Process.Start(info);
            if (process != null)
            {
                if (thirdPartyTool)
                {
                    return await _playTimeService.StartProcessToLogAsync(gameId);
                }
                else
                {
                    await _playTimeService.StartProcessToLogAsync(gameId, process.Id);
                    return process;
                }
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == ERROR_CANCELLED)
        {
            // Operation canceled
            _logger.LogInformation("Start game operation canceled.");
        }
        return null;
    }






}
