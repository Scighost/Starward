using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;

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
        var keyName = (int)biz switch
        {
            11 or 21 or 31 or 14 or 24 => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
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
            else if (biz.ToGame() is GameBiz.Honkai3rd)
            {
                account.Uid = (int)(Registry.GetValue(key, GameRegistry.GENERAL_DATA_V2_LastLoginUserId_h47158221, 0) ?? 0);
            }
            else if (biz is GameBiz.hk4e_cn or GameBiz.hk4e_global)
            {
                List<int> uids = GetUidsFromRegistry(biz).ToList();
                if (uids.Count == 1)
                {
                    account.Uid = uids[0];
                }
            }
            return account;
        }
        return null;
    }



    private IEnumerable<int> GetUidsFromRegistry(GameBiz biz)
    {
        if (biz is GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hk4e_bilibili)
        {
            string key = biz.GetGameRegistryKey().Replace(@"HKEY_CURRENT_USER\", "");
            List<string> usds = Registry.CurrentUser.OpenSubKey(key)?.GetValueNames()?.Where(x => x.StartsWith("USD_"))?.ToList() ?? [];
            foreach (var usd in usds)
            {
                string uidstr = Regex.Match(usd, @"USD_(\d+)_h").Groups[1].Value;
                if (int.TryParse(uidstr, out int uid))
                {
                    if (uid != 0)
                    {
                        yield return uid;
                    }
                }
            }
        }
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
        if (biz is GameBiz.hk4e_cn or GameBiz.hk4e_global)
        {
            uids.AddRange(GetUidsFromRegistry(biz));
        }
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
        dapper.Execute("DELETE FROM GameAccount WHERE SHA256=@SHA256;", account);
        _logger.LogInformation("Delete account {name} ({biz}) successfully!", account.Name, account.GameBiz);
    }




    public void ChangeGameAccount(GameAccount account)
    {
        var key = account.GameBiz.GetGameRegistryKey();
        var keyName = (int)account.GameBiz switch
        {
            11 or 21 or 31 or 14 or 24 => GameRegistry.MIHOYOSDK_ADL_PROD_CN_h3123967166,
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




}
