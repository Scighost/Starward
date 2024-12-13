using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Starward.Services;

internal class GameAccountService
{


    private readonly ILogger<GameAccountService> _logger;


    private readonly DatabaseService _database;



    public GameAccountService(ILogger<GameAccountService> logger, DatabaseService database)
    {
        _logger = logger;
        _database = database;
    }





    public GameAccount? GetGameAccountsFromRegistry(GameBiz biz)
    {
        var key = biz.GetGameRegistryKey();
        var keyName = (biz.IsChinaServer(), biz.IsGlobalServer(), biz.IsBilibili(), biz.IsChinaCloud()) switch
        {
            (true, false, false, false) => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
            (false, false, true, false) => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
            (false, false, false, true) => GameRegistry.MIHOYOSDK_ADL_0,
            (false, true, false, false) => GameRegistry.MIHOYOSDK_ADL_PROD_OVERSEA_h1158948810,
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
            if (biz.ToGame() == GameBiz.hkrpg)
            {
                account.Uid = (int)(Registry.GetValue(key, GameRegistry.App_LastUserID_h2841727341, 0) ?? 0);
            }
            else if (biz.ToGame() == GameBiz.bh3)
            {
                account.Uid = (int)(Registry.GetValue(key, GameRegistry.GENERAL_DATA_V2_LastLoginUserId_h47158221, 0) ?? 0);
            }
            else if (biz.Value is GameBiz.hk4e_cn or GameBiz.hk4e_global)
            {
                byte[]? uidBytes = Registry.GetValue(key, GameRegistry.__LastUid___h2153286551, 0) as byte[];
                if (uidBytes is not null)
                {
                    string uidStr = Encoding.UTF8.GetString(uidBytes).Trim();
                    if (long.TryParse(uidStr, out long uid))
                    {
                        account.Uid = uid;
                    }
                }
            }
            return account;
        }
        return null;
    }





    public IEnumerable<GameAccount> GetGameAccountsFromDatabase(GameBiz biz)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<GameAccount>("SELECT * FROM GameAccount WHERE GameBiz = @biz;", new { biz });
        foreach (var item in list)
        {
            item.IsSaved = true;
            yield return item;
        }
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



    public IEnumerable<int> GetSuggestionUids(GameBiz biz)
    {
        using var dapper = _database.CreateConnection();
        List<int> uids = dapper.Query<int>("SELECT DISTINCT Uid FROM GameAccount WHERE GameBiz = @biz AND Uid > 0;", new { biz }).ToList();
        return uids.Distinct().Order();
    }



    public void SaveGameAccount(GameAccount account)
    {
        account.Name ??= "";
        account.IsSaved = true;
        using var dapper = _database.CreateConnection();
        dapper.Execute("INSERT OR REPLACE INTO GameAccount (SHA256, GameBiz, Uid, Name, Value, Time) VALUES (@SHA256, @GameBiz, @Uid, @Name, @Value, @Time);", account);
        _logger.LogInformation("Save account {name} ({biz}) successfully!", account.Name, account.GameBiz);
    }



    public void DeleteGameAccount(GameAccount account)
    {
        using var dapper = _database.CreateConnection();
        dapper.Execute("DELETE FROM GameAccount WHERE GameBiz=@GameBiz AND SHA256=@SHA256;", account);
        _logger.LogInformation("Delete account {name} ({biz}) successfully!", account.Name, account.GameBiz);
    }




    public void ChangeGameAccount(GameAccount account)
    {
        var key = account.GameBiz.GetGameRegistryKey();
        var biz = account.GameBiz;
        var keyName = (biz.IsChinaServer(), biz.IsGlobalServer(), biz.IsBilibili(), biz.IsChinaCloud()) switch
        {
            (true, false, false, false) => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
            (false, false, true, false) => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
            (false, false, false, true) => GameRegistry.MIHOYOSDK_ADL_0,
            (false, true, false, false) => GameRegistry.MIHOYOSDK_ADL_PROD_OVERSEA_h1158948810,
            _ => throw new ArgumentOutOfRangeException($"Unknown region {biz}"),
        };

        Registry.SetValue(key, keyName, account.Value);
        if (account.GameBiz.ToGame() == GameBiz.hkrpg)
        {
            Registry.SetValue(key, GameRegistry.App_LastUserID_h2841727341, (int)account.Uid, RegistryValueKind.DWord);
        }
        if (account.GameBiz.ToGame() == GameBiz.bh3)
        {
            Registry.SetValue(key, GameRegistry.GENERAL_DATA_V2_LastLoginUserId_h47158221, (int)account.Uid, RegistryValueKind.DWord);
        }
        if (account.GameBiz.ToGame() == GameBiz.hk4e)
        {
            Registry.SetValue(key, GameRegistry.__LastUid___h2153286551, Encoding.UTF8.GetBytes($"{account.Uid}\0"));
        }
        _logger.LogInformation("Change account {name} ({biz}) successfully!", account.Name, (object)account.GameBiz);
    }




}
