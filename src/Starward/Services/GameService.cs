using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace Starward.Services;

internal class GameService
{


    private readonly ILogger<GameService> _logger;


    private readonly DatabaseService _database;

    public GameService(ILogger<GameService> logger, DatabaseService database)
    {
        _logger = logger;
        _database = database;
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
            _logger.LogWarning("Game uninstalled path not found ({biz})", biz);
            AppConfig.SetGameInstallPath(biz, null);
            return null;
        }
    }


    public static string GetGameExeName(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.hk4e_cn => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            GameBiz.hk4e_cloud => "Genshin Impact Cloud Game.exe",
            _ => biz.ToGame() switch
            {
                GameBiz.StarRail => "StarRail.exe",
                GameBiz.Honkai3rd => "BH3.exe",
                _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
            },
        };
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
            folder = GetGameInstallPath(biz);
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




    public GameAccount? GetGameAccountsFromRegistry(GameBiz biz)
    {
        var key = biz.GetGameRegistryKey();
        var keyName = (int)biz switch
        {
            11 or 21 or 31 => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
            13 => GameRegistry.MIHOYOSDK_ADL_0,
            12 or 22 or (>= 32 and <= 36) => GameRegistry.MIHOYOSDK_ADL_PROD_OVERSEA_h1158948810,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };

        var adl = Registry.GetValue(key, keyName, null) as byte[];
        if (adl != null)
        {
            var account = new GameAccount
            {
                SHA256 = Convert.ToHexString(SHA256.HashData(adl)),
                GameBiz = biz,
                Value = adl,
                IsLogin = true,
            };
            if (biz.ToGame() is GameBiz.StarRail)
            {
                account.Uid = (int)(Registry.GetValue(key, GameRegistry.App_LastUserID_h2841727341, 0) ?? 0);
            }
            if (biz.ToGame() is GameBiz.Honkai3rd)
            {
                account.Uid = (int)(Registry.GetValue(key, GameRegistry.GENERAL_DATA_V2_LastLoginUserId_h47158221, 0) ?? 0);
            }
            return account;
        }
        return null;
    }



    public IEnumerable<GameAccount> GetGameAccountsFromDatabase(GameBiz biz)
    {
        using var dapper = _database.CreateConnection();
        return dapper.Query<GameAccount>("SELECT * FROM GameAccount WHERE GameBiz = @biz;", new { biz });
    }



    public List<GameAccount> GetGameAccounts(GameBiz biz)
    {
        var databaseAccounts = GetGameAccountsFromDatabase(biz).ToList();
        var regAccount = GetGameAccountsFromRegistry(biz);
        if (regAccount != null)
        {
            if (databaseAccounts.FirstOrDefault(x => x.SHA256 == regAccount.SHA256) is GameAccount ga)
            {
                ga.IsLogin = true;
                databaseAccounts.Remove(ga);
                databaseAccounts.Insert(0, ga);
            }
            else
            {
                databaseAccounts.Insert(0, regAccount);
            }
        }
        return databaseAccounts;
    }





    public void SaveGameAccount(GameAccount account)
    {
        account.Name ??= "";
        using var dapper = _database.CreateConnection();
        dapper.Execute("INSERT OR REPLACE INTO GameAccount (SHA256, GameBiz, Uid, Name, Value, Time) VALUES (@SHA256, @GameBiz, @Uid, @Name, @Value, @Time);", account);
        _logger.LogInformation("Save account {name} ({biz}) successfully!", account.Name, account.GameBiz);
    }



    public void DeleteGameAccount(GameAccount account)
    {
        using var dapper = _database.CreateConnection();
        dapper.Execute("DELETE FROM GameAccount WHERE SHA256=@SHA256;", account);
        _logger.LogInformation("Delete account {name} ({biz}) successfully!", account.Name, account.GameBiz);
    }




    public void ChangeGameAccount(GameAccount account)
    {
        var key = account.GameBiz.GetGameRegistryKey();
        var keyName = (int)account.GameBiz switch
        {
            11 or 21 or 31 => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
            13 => GameRegistry.MIHOYOSDK_ADL_0,
            12 or 22 or (>= 32 and <= 36) => GameRegistry.MIHOYOSDK_ADL_PROD_OVERSEA_h1158948810,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {account.GameBiz}"),
        };
        Registry.SetValue(key, keyName, account.Value);
        if (account.GameBiz.ToGame() is GameBiz.StarRail)
        {
            Registry.SetValue(key, GameRegistry.App_LastUserID_h2841727341, account.Uid);
        }
        if (account.GameBiz.ToGame() is GameBiz.Honkai3rd)
        {
            Registry.SetValue(key, GameRegistry.GENERAL_DATA_V2_LastLoginUserId_h47158221, account.Uid);
        }
        _logger.LogInformation("Change account {name} ({biz}) successfully!", account.Name, account.GameBiz);
    }





    public Process? GetGameProcess(GameBiz biz)
    {
        var name = GetGameExeName(biz).Replace(".exe", "");
        return Process.GetProcessesByName(name).FirstOrDefault();
    }





    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public Process? StartGame(GameBiz biz, bool ignoreRunningGame = false)
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
            if (AppConfig.GetEnableThirdPartyTool(biz))
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




    public int GetStarRailFPS(GameBiz biz)
    {
        var key = biz switch
        {
            GameBiz.hkrpg_cn => GameRegistry.GamePath_hkrpg_cn,
            GameBiz.hkrpg_global => GameRegistry.GamePath_hkrpg_global,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var bytes = Registry.GetValue(key, GameRegistry.GraphicsSettings_Model_h2986158309, null) as byte[];
        if (bytes != null)
        {
            var str = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            var node = JsonNode.Parse(str);
            if (node != null)
            {
                return (int)(node["FPS"] ?? 60);
            }
        }
        return 60;
    }




    public void SetStarRailFPS(GameBiz biz, int fps)
    {
        var key = biz switch
        {
            GameBiz.hkrpg_cn => GameRegistry.GamePath_hkrpg_cn,
            GameBiz.hkrpg_global => GameRegistry.GamePath_hkrpg_global,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var bytes = Registry.GetValue(key, GameRegistry.GraphicsSettings_Model_h2986158309, null) as byte[];
        if (bytes != null)
        {
            var str = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            var node = JsonNode.Parse(str);
            if (node != null)
            {
                node["FPS"] = fps;
                bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(node));
                Registry.SetValue(key, GameRegistry.GraphicsSettings_Model_h2986158309, bytes);
            }
        }
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
                _logger.LogInformation("Finishing deleting temp files.");
            }

            if (steps.HasFlag(UninstallStep.DeleteGameAssets) && Directory.Exists(loc))
            {
                _logger.LogInformation("Start to delete game assets: {loc}", loc);
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



    void DeleteFolderAndParent(string folder)
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
