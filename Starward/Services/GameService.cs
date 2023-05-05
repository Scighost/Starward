using Dapper;
using Microsoft.Win32;
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

    private const string REG_KEY_CN = @"HKEY_CURRENT_USER\Software\miHoYo\崩坏：星穹铁道";
    private const string ADL_CN = "MIHOYOSDK_ADL_PROD_CN_h3123967166";

    private const string REG_KEY_OS = @"HKEY_CURRENT_USER\Software\Cognosphere\Star Rail";
    private const string ADL_OS = "MIHOYOSDK_ADL_PROD_OVERSEA_h1158948810";

    private const string AppLastUserId = "App_LastUserID_h2841727341";
    private const string GraphicsSetting = "GraphicsSettings_Model_h2986158309";






    public static List<GameAccount> GetGameAccountsFromRegistry()
    {
        var list = new List<GameAccount>();
        using var dapper = DatabaseService.Instance.CreateConnection();

        var cnKey = Registry.GetValue(REG_KEY_CN, ADL_CN, null) as byte[];
        var uid = (int)(Registry.GetValue(REG_KEY_CN, AppLastUserId, 0) ?? 0);
        if (cnKey != null)
        {
            var account = new GameAccount
            {
                SHA256 = Convert.ToHexString(SHA256.HashData(cnKey)),
                Uid = uid,
                Server = 0,
                Value = cnKey,
                IsLogin = true,
            };
            list.Add(account);
        }


        var globalKey = Registry.GetValue(REG_KEY_OS, ADL_OS, null) as byte[];
        uid = (int)(Registry.GetValue(REG_KEY_OS, AppLastUserId, 0) ?? 0);
        if (globalKey != null)
        {
            var account = new GameAccount
            {
                SHA256 = Convert.ToHexString(SHA256.HashData(globalKey)),
                Uid = uid,
                Server = 1,
                Value = globalKey,
                IsLogin = true,
            };
            list.Add(account);
        }

        return list;
    }



    public static IEnumerable<GameAccount> GetGameAccountsFromDatabase()
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.Query<GameAccount>("SELECT * FROM GameAccount ORDER BY Server;");
    }



    public static List<GameAccount> GetGameAccounts()
    {
        var databaseAccounts = GetGameAccountsFromDatabase().ToList();
        var regAccounts = GetGameAccountsFromRegistry();
        foreach (var account in regAccounts)
        {
            if (databaseAccounts.FirstOrDefault(x => x.SHA256 == account.SHA256) is GameAccount ga)
            {
                ga.IsLogin = true;
                databaseAccounts.Remove(ga);
                databaseAccounts.Insert(0, ga);
            }
            else
            {
                databaseAccounts.Insert(0, account);
            }
        }
        return databaseAccounts;
    }





    public static void SaveGameAccount(GameAccount account)
    {
        if (string.IsNullOrWhiteSpace(account.Name))
        {
            return;
        }
        using var dapper = DatabaseService.Instance.CreateConnection();
        dapper.Execute("INSERT OR REPLACE INTO GameAccount (SHA256, Uid, Name, Server, Value, Time) VALUES (@SHA256, @Uid, @Name, @Server, @Value, @Time);", account);
    }



    public static void DeleteGameAccount(GameAccount account)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        dapper.Execute("DELETE FROM GameAccount WHERE SHA256=@SHA256;", account);
    }



    public static void ChangeGameAccount(GameAccount account)
    {
        if (IsGameRunning())
        {
            throw new Exception("Game process is running.");
        }
        if (account.Server == 0)
        {
            Registry.SetValue(REG_KEY_CN, ADL_CN, account.Value);
            Registry.SetValue(REG_KEY_CN, AppLastUserId, account.Uid);
        }
        else
        {
            Registry.SetValue(REG_KEY_OS, ADL_OS, account.Value);
            Registry.SetValue(REG_KEY_OS, AppLastUserId, account.Uid);
        }
    }





    /// <summary>
    /// 游戏是否在运行
    /// </summary>
    /// <param name="server"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static bool IsGameRunning()
    {
        return Process.GetProcessesByName("StarRail").Any();
    }





    /// <summary>
    /// 启动游戏
    /// </summary>
    /// <returns></returns>
    public static void StartGame(int serverIndex)
    {
        try
        {
            if (IsGameRunning())
            {
                throw new Exception("Game process is running.");
            }
            var folder = GachaLogClient.GetGameInstallPathFromRegistry(serverIndex);
            var exe = Path.Join(folder, "StarRail.exe");
            var arg = AppConfig.StartGameArgument?.Trim();
            var info = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = arg,
                UseShellExecute = true,
                Verb = "runas",
                WorkingDirectory = folder,
            };
            Process.Start(info);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            // Operation canceled
            return;
        }
    }




    public static void ChangeGameFPS(int fps)
    {
        var bytes = Registry.GetValue(REG_KEY_CN, GraphicsSetting, null) as byte[];
        if (bytes != null)
        {
            var str = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            var node = JsonNode.Parse(str);
            if (node != null)
            {
                node["FPS"] = fps;
                bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(node));
                Registry.SetValue(REG_KEY_CN, GraphicsSetting, bytes);
            }
        }
        bytes = Registry.GetValue(REG_KEY_OS, GraphicsSetting, null) as byte[];
        if (bytes != null)
        {
            var str = Encoding.UTF8.GetString(bytes).TrimEnd('\0');
            var node = JsonNode.Parse(str);
            if (node != null)
            {
                node["FPS"] = fps;
                bytes = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(node));
                Registry.SetValue(REG_KEY_OS, GraphicsSetting, bytes);
            }
        }
    }




}
