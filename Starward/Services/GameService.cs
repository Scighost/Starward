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

namespace Starward.Services;

internal class GameService
{


    private const string REG_KEY_YS_CN = @"HKEY_CURRENT_USER\Software\miHoYo\原神";
    private const string ADL_YS_CN = "MIHOYOSDK_ADL_PROD_CN_h3123967166";

    private const string REG_KEY_YS_OS = @"HKEY_CURRENT_USER\Software\miHoYo\Genshin Impact";
    private const string ADL_YS_OS = "MIHOYOSDK_ADL_PROD_OVERSEA_h1158948810";

    private const string REG_KEY_YS_CLOUD = @"HKEY_CURRENT_USER\Software\miHoYo\云·原神";
    private const string ADL_YS_CLOUD = "MIHOYOSDK_ADL_0";


    private const string REG_KEY_SR_CN = @"HKEY_CURRENT_USER\Software\miHoYo\崩坏：星穹铁道";
    private const string ADL_SR_CN = "MIHOYOSDK_ADL_PROD_CN_h3123967166";

    private const string REG_KEY_SR_OS = @"HKEY_CURRENT_USER\Software\Cognosphere\Star Rail";
    private const string ADL_SR_OS = "MIHOYOSDK_ADL_PROD_OVERSEA_h1158948810";

    private const string SR_AppLastUserId = "App_LastUserID_h2841727341";
    private const string SR_GraphicsSetting = "GraphicsSettings_Model_h2986158309";


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
            return null;
        }
    }



    public string? GetGameScreenshotPath(GameBiz biz)
    {
        string? folder = null;
        if (biz is GameBiz.hk4e_cloud)
        {
            folder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        }
        else
        {
            folder = GetGameInstallPath(biz);
        }
        var relativePath = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_global => "ScreenShot",
            GameBiz.hk4e_cloud => "GenshinImpactCloudGame",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global => @"StarRail_Data\ScreenShots",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var path = Path.Join(folder, relativePath);
        if (Directory.Exists(path))
        {
            return path;
        }
        else
        {
            return null;
        }
    }




    public GameAccount? GetGameAccountsFromRegistry(GameBiz biz)
    {
        var key = biz switch
        {
            GameBiz.hk4e_cn => REG_KEY_YS_CN,
            GameBiz.hk4e_global => REG_KEY_YS_OS,
            GameBiz.hk4e_cloud => REG_KEY_YS_CLOUD,
            GameBiz.hkrpg_cn => REG_KEY_SR_CN,
            GameBiz.hkrpg_global => REG_KEY_SR_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };

        var keyName = biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hkrpg_cn => ADL_SR_CN,
            GameBiz.hk4e_global or GameBiz.hkrpg_global => ADL_YS_OS,
            GameBiz.hk4e_cloud => ADL_YS_CLOUD,
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
            if (biz is GameBiz.hkrpg_cn or GameBiz.hkrpg_global)
            {
                account.Uid = (int)(Registry.GetValue(key, SR_AppLastUserId, 0) ?? 0);
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
        if (string.IsNullOrWhiteSpace(account.Name))
        {
            _logger.LogWarning("Account name is null, cannot be saved.");
            return;
        }
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
        switch (account.GameBiz)
        {
            case GameBiz.None:
                break;
            case GameBiz.hk4e_cn:
                Registry.SetValue(REG_KEY_YS_CN, ADL_YS_CN, account.Value);
                break;
            case GameBiz.hk4e_global:
                Registry.SetValue(REG_KEY_YS_OS, ADL_YS_OS, account.Value);
                break;
            case GameBiz.hk4e_cloud:
                Registry.SetValue(REG_KEY_YS_CLOUD, ADL_YS_CLOUD, account.Value);
                break;
            case GameBiz.hkrpg_cn:
                Registry.SetValue(REG_KEY_SR_CN, ADL_SR_CN, account.Value);
                Registry.SetValue(REG_KEY_SR_CN, SR_AppLastUserId, account.Uid);
                break;
            case GameBiz.hkrpg_global:
                Registry.SetValue(REG_KEY_SR_OS, ADL_SR_OS, account.Value);
                Registry.SetValue(REG_KEY_SR_OS, SR_AppLastUserId, account.Uid);
                break;
            default:
                break;
        }
        _logger.LogInformation("Change account {name} ({biz}) successfully!", account.Name, account.GameBiz);
    }





    public Process? GetGameProcess(GameBiz biz)
    {
        var name = biz switch
        {
            GameBiz.hk4e_cn => "YuanShen",
            GameBiz.hk4e_global => "GenshinImpact",
            GameBiz.hk4e_cloud => "Genshin Impact Cloud Game",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global => "StarRail",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        return Process.GetProcessesByName(name).FirstOrDefault();
    }





    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public Process? StartGame(GameBiz biz, bool ignoreRunningGame = false)
    {
        try
        {
            if (!ignoreRunningGame)
            {
                var process = GetGameProcess(biz);
                if (process != null)
                {
                    throw new Exception("Game process is running.");
                }
            }
            var folder = GetGameInstallPath(biz);
            var name = biz switch
            {
                GameBiz.hk4e_cn => "YuanShen.exe",
                GameBiz.hk4e_global => "GenshinImpact.exe",
                GameBiz.hk4e_cloud => "Genshin Impact Cloud Game.exe",
                GameBiz.hkrpg_cn or GameBiz.hkrpg_global => "StarRail.exe",
                _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
            };
            var exe = Path.Join(folder, name);
            var arg = AppConfig.GetStartArgument(biz)?.Trim();
            _logger.LogInformation("Start game ({biz})\r\npath: {exe}\r\nargu: {argu}", biz, exe, arg);
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arg,
                UseShellExecute = true,
                Verb = (biz is GameBiz.hk4e_cloud) ? "" : "runas",
                WorkingDirectory = folder,
            };
            return Process.Start(info);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // Operation canceled
            _logger.LogInformation("Start game operation canceled.");
            return null;
        }
    }




    public int GetStarRailFPS(GameBiz biz)
    {
        var key = biz switch
        {
            GameBiz.hkrpg_cn => REG_KEY_SR_CN,
            GameBiz.hkrpg_global => REG_KEY_SR_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var bytes = Registry.GetValue(key, SR_GraphicsSetting, null) as byte[];
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
            GameBiz.hkrpg_cn => REG_KEY_SR_CN,
            GameBiz.hkrpg_global => REG_KEY_SR_OS,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };
        var bytes = Registry.GetValue(key, SR_GraphicsSetting, null) as byte[];
        if (bytes != null)
        {
            var str = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            var node = JsonNode.Parse(str);
            if (node != null)
            {
                node["FPS"] = fps;
                bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(node));
                Registry.SetValue(key, SR_GraphicsSetting, bytes);
            }
        }
    }






}
