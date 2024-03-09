using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Models;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Starward.Services;

internal class GameService
{


    private readonly ILogger<GameService> _logger;


    private readonly GameResourceService _gameResourceService;



    public GameService(ILogger<GameService> logger, GameResourceService gameResourceService)
    {
        _logger = logger;
        _gameResourceService = gameResourceService;
    }




    public string? GetGameScreenshotPath(GameBiz biz)
    {
        string? folder = null;
        if (biz is GameBiz.hk4e_cloud)
        {
            var config = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"GenshinImpactCloudGame\config\config.ini");
            if (File.Exists(config))
            {
                var str = File.ReadAllText(config);
                var escape = Regex.Match(str, @"save_game_screenshot_directory=(.+)").Groups[1].Value.Trim();
                folder = Regex.Unescape(escape.Replace(@"\x", @"\u"));
            }
        }
        else
        {
            folder = _gameResourceService.GetGameInstallPath(biz);
            var relativePath = biz.ToGame() switch
            {
                GameBiz.GenshinImpact => "ScreenShot",
                GameBiz.StarRail => @"StarRail_Data\ScreenShots",
                GameBiz.Honkai3rd => @"ScreenShot",
                _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
            };
            folder = Path.Join(folder, relativePath);
        }
        if (Directory.Exists(folder))
        {
            return Path.GetFullPath(folder);
        }
        else
        {
            return null;
        }
    }






    public Process? GetGameProcess(GameBiz biz)
    {
        int currentSessionId = Process.GetCurrentProcess().SessionId;
        var name = GameResourceService.GetGameExeName(biz).Replace(".exe", "");
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
                var e = Path.Join(installPath, GameResourceService.GetGameExeName(biz));
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
                var folder = _gameResourceService.GetGameInstallPath(biz);
                var name = GameResourceService.GetGameExeName(biz);
                exe = Path.Join(folder, name);
                arg = AppConfig.GetStartArgument(biz)?.Trim();
                verb = (biz is GameBiz.hk4e_cloud) ? "" : "runas";
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




    public int UninstallGame(GameBiz gameBiz, string? loc, UninstallStep steps)
    {
        try
        {
            if (steps == UninstallStep.None)
            {
                _logger.LogWarning("No nned to do anything.");
                return 0;
            }
            if (GetGameProcess(gameBiz) != null)
            {
                _logger.LogWarning("Game is runnning");
                return -1;
            }

            if (steps.HasFlag(UninstallStep.BackupScreenshot) && Directory.Exists(AppConfig.UserDataFolder))
            {
                _logger.LogInformation("Start to backup screenshot");
                string relativePath = gameBiz.ToGame() switch
                {
                    GameBiz.GenshinImpact => "ScreenShot",
                    GameBiz.StarRail => @"StarRail_Data\ScreenShots",
                    GameBiz.Honkai3rd => @"ScreenShot",
                    _ => throw new ArgumentOutOfRangeException($"Unknown region {gameBiz}"),
                };
                string folder = Path.Join(loc, relativePath);
                if (Directory.Exists(folder))
                {
                    _logger.LogInformation("Screenshot folder is {folder}", folder);
                    string dest = Path.Join(AppConfig.UserDataFolder, "Screenshots", gameBiz.ToString());
                    Directory.CreateDirectory(dest);
                    string[] files = Directory.GetFiles(folder);
                    int count = 0;
                    foreach (var file in files)
                    {
                        string t = Path.Join(dest, Path.GetFileName(file));
                        if (!File.Exists(t))
                        {
                            File.Copy(file, t, true);
                            count++;
                        }
                    }
                    _logger.LogInformation("Backed up {count} screenshots.", count);
                }
                else
                {
                    _logger.LogWarning("Screenshot folder does not exist: {folder}", folder);
                }
            }

            if (steps.HasFlag(UninstallStep.CleanRegistry))
            {
                _logger.LogInformation("Start to clean registry.");
                string key = gameBiz.GetGameRegistryKey().Replace(@"HKEY_CURRENT_USER\", "");
                Registry.CurrentUser.DeleteSubKeyTree(key, false);
                var parent = Registry.CurrentUser.OpenSubKey(Path.GetDirectoryName(key)!);
                if (parent != null && parent.SubKeyCount == 0 && parent.ValueCount == 0)
                {
                    Registry.CurrentUser.DeleteSubKey(Path.GetDirectoryName(key)!, false);
                }
                _logger.LogInformation("Finished clean registry.");
            }

            if (steps.HasFlag(UninstallStep.DeleteTempFiles))
            {
                _logger.LogInformation("Start to delete temp files.");
                string relativePath = gameBiz.GetGameRegistryKey().Replace(@"HKEY_CURRENT_USER\Software\", "");
                string temp = Path.Join(Path.GetTempPath(), relativePath);
                string local = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), relativePath);
                string locallow = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "Low", relativePath);
                DeleteFolderAndParent(temp);
                DeleteFolderAndParent(local);
                DeleteFolderAndParent(locallow);
                _logger.LogInformation("Finished deleting temp files.");
            }

            if (steps.HasFlag(UninstallStep.DeleteGameAssets) && Directory.Exists(loc))
            {
                _logger.LogInformation("Start to delete game assets: {loc}", loc);
                if (new DirectoryInfo(loc).FullName == new DirectoryInfo(loc).Root.FullName)
                {
                    _logger.LogWarning("Game assets' folder is the root of drive.");
                    return -1;
                }
                if (Directory.GetDirectories(loc).Length > 3)
                {
                    _logger.LogWarning("Game assets' folder has more than 3 subfolders, it may delete other non-game assets files.");
                    return -1;
                }
                string[] files = Directory.GetFiles(loc, "*", SearchOption.AllDirectories);
                foreach (string file in files)
                {
                    File.SetAttributes(file, FileAttributes.Normal);
                }
                Directory.Delete(loc, true);
                _logger.LogInformation("Finished deleting game assets.");
            }

            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstall game");
            return -1;
        }
    }



    private void DeleteFolderAndParent(string folder)
    {
        if (Directory.Exists(folder))
        {
            Directory.Delete(folder, true);
            string? parent = Path.GetDirectoryName(folder);
            if (Directory.Exists(parent) && Directory.GetDirectories(parent).Length == 0 && Directory.GetFiles(parent).Length == 0)
            {
                Directory.Delete(parent, true);
            }
            _logger.LogInformation("Deleted folder {folder}", folder);
        }
    }




}
