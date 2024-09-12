using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.SelfQuery;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services;

internal class SelfQueryService
{


    private readonly ILogger<SelfQueryService> _logger;

    private readonly DatabaseService _databaseService;

    private readonly SelfQueryClient _selfQueryClient;



    public SelfQueryService(ILogger<SelfQueryService> logger, DatabaseService databaseService, SelfQueryClient selfQueryClient)
    {
        _logger = logger;
        _databaseService = databaseService;
        _selfQueryClient = selfQueryClient;
    }



    public SelfQueryUserInfo? UserInfo => _selfQueryClient.UserInfo;



    public async Task<SelfQueryUserInfo> InitializeAsync(string url, GameBiz gameBiz)
    {
        return await _selfQueryClient.InitializeAsync(url, gameBiz);
    }



    public void EnsureInitialized()
    {
        _selfQueryClient.EnsureInitialized();
    }


    public void Reset()
    {
        _selfQueryClient.Reset();
    }



    #region Genshin



    public List<long> GetGenshinUids()
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.Query<long>("SELECT DISTINCT Uid FROM GenshinQueryItem;").ToList();
    }



    public (long Add, long Sub) GetGenshinQueryItemsNumSum(long uid, GenshinQueryType type)
    {
        using var dapper = _databaseService.CreateConnection();
        long add = dapper.QueryFirstOrDefault<long>("""
            SELECT IFNULL(SUM(AddNum), 0) FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type AND AddNum>0;
            """, new { uid, type });
        long sub = dapper.QueryFirstOrDefault<long>("""
            SELECT IFNULL(SUM(AddNum), 0) FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type AND AddNum<0;
            """, new { uid, type });
        return (add, sub);
    }




    private long GetGenshinLastId(long uid, GenshinQueryType type)
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<long>("""
            SELECT Id FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type ORDER BY Id DESC LIMIT 1;
            """, new { uid, type });
    }




    private int InsertGenshinQueryItems(long uid, GenshinQueryType type, List<GenshinQueryItem> items, bool all = false)
    {
        using var dapper = _databaseService.CreateConnection();
        int oldCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        using var t = dapper.BeginTransaction();
        if (all)
        {
            var months = items.GroupBy(x => (x.DateTime.Year, x.DateTime.Month)).Select(x => $"{x.Key.Year}-{x.Key.Month:D2}").Distinct().ToList();
            foreach (var month in months)
            {
                dapper.Execute($"""
                DELETE FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type AND DateTime LIKE @time;
                """, new { uid, type, time = month + "%" }, t);
            }
        }
        dapper.Execute("""
            INSERT OR REPLACE INTO GenshinQueryItem (Uid, Id, AddNum, Reason, DateTime, Type, Icon, Level, Quality, Name)
            VALUES (@Uid, @Id, @AddNum, @Reason, @DateTime, @Type, @Icon, @Level, @Quality, @Name);
            """, items, t);
        t.Commit();
        int newCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        return newCount - oldCount;
    }






    public async Task<(long Add, long Sub)> UpdateGenshinQueryItemsAsync(GenshinQueryType type, IProgress<int> progress, bool all = false, CancellationToken cancellationToken = default)
    {
        _selfQueryClient.EnsureInitialized();
        long uid = _selfQueryClient.UserInfo?.Uid ?? 0;
        using var dapper = _databaseService.CreateConnection();
        long lastId = GetGenshinLastId(uid, type);
        long endId = 0;
        var list = new List<GenshinQueryItem>(20);
        for (int i = 1; ; i++)
        {
            progress.Report(i);
            var temp_list = await _selfQueryClient.GetGenshinQueryItemsAsync(type, endId, cancellationToken);
            list.AddRange(temp_list);
            if (temp_list.Count < 20)
            {
                break;
            }
            endId = temp_list[19].Id;
            if (!all && endId <= lastId)
            {
                break;
            }
        }
        InsertGenshinQueryItems(uid, type, list, all);
        return GetGenshinQueryItemsNumSum(uid, type);
    }



    #endregion




    #region Star Rail



    public List<long> GetStarRailUids()
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.Query<long>("SELECT DISTINCT Uid FROM StarRailQueryItem;").ToList();
    }




    public (long Add, long Sub) GetStarRailQueryItemsNumSum(long uid, StarRailQueryType type)
    {
        using var dapper = _databaseService.CreateConnection();
        long add = dapper.QueryFirstOrDefault<long>("""
            SELECT IFNULL(SUM(AddNum), 0) FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type AND AddNum>0;
            """, new { uid, type });
        long sub = dapper.QueryFirstOrDefault<long>("""
            SELECT IFNULL(SUM(AddNum), 0) FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type AND AddNum<0;
            """, new { uid, type });
        return (add, sub);
    }



    private long GetStarRailLastId(long uid, StarRailQueryType type)
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<long>("""
            SELECT Id FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type ORDER BY Id DESC LIMIT 1;
            """, new { uid, type });
    }




    private int InsertStarRailQueryItems(long uid, StarRailQueryType type, List<StarRailQueryItem> items, bool all = false)
    {
        using var dapper = _databaseService.CreateConnection();
        int oldCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        using var t = dapper.BeginTransaction();
        if (all)
        {
            var months = items.GroupBy(x => (x.Time.Year, x.Time.Month)).Select(x => $"{x.Key.Year}-{x.Key.Month:D2}").Distinct().ToList();
            foreach (var month in months)
            {
                dapper.Execute($"""
                DELETE FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type AND Time LIKE @time;
                """, new { uid, type, time = month + "%" }, t);
            }
        }
        dapper.Execute("""
            INSERT OR REPLACE INTO StarRailQueryItem(Id, Uid, Type, Action, AddNum, Time, RelicName, RelicLevel, RelicRarity, EquipmentName, EquipmentLevel, EquipmentRarity)
            VALUES (@Id, @Uid, @Type, @Action, @AddNum, @Time, @RelicName, @RelicLevel, @RelicRarity, @EquipmentName, @EquipmentLevel, @EquipmentRarity);
            """, items, t);
        t.Commit();
        int newCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        return newCount - oldCount;
    }




    public async Task<(long Add, long Sub)> UpdateStarRailQueryItemsAsync(StarRailQueryType type, IProgress<int> progress, bool all = false, CancellationToken cancellationToken = default)
    {
        _selfQueryClient.EnsureInitialized();
        long uid = _selfQueryClient.UserInfo?.Uid ?? 0;
        using var dapper = _databaseService.CreateConnection();
        long lastId = GetStarRailLastId(uid, type);
        var list = new List<StarRailQueryItem>(20);
        long endId = 0;
        for (int i = 1; ; i++)
        {
            progress.Report(i);
            var temp_list = await _selfQueryClient.GetStarRailQueryItemsAsync(type, endId, 20, null, null, cancellationToken);
            list.AddRange(temp_list);
            if (temp_list.Count < 20)
            {
                break;
            }
            endId = temp_list[19].Id;
            if (!all && endId <= lastId)
            {
                break;
            }
        }
        InsertStarRailQueryItems(uid, type, list, all);
        return GetStarRailQueryItemsNumSum(uid, type);
    }





    #endregion




    #region ZZZ



    public List<long> GetZZZUids()
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.Query<long>("SELECT DISTINCT Uid FROM ZZZQueryItem;").ToList();
    }



    public (long Add, long Sub) GetZZZQueryItemsNumSum(long uid, ZZZQueryType type)
    {
        using var dapper = _databaseService.CreateConnection();
        long add = 0, sub = 0;
        if (type is ZZZQueryType.PurchaseGift)
        {
            add = dapper.QueryFirstOrDefault<long>("""
                SELECT COUNT(*) FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type;
                """, new { uid, type });
        }
        else
        {
            add = dapper.QueryFirstOrDefault<long>("""
                SELECT IFNULL(SUM(AddNum), 0) FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type AND AddNum>0;
                """, new { uid, type });
            sub = dapper.QueryFirstOrDefault<long>("""
                SELECT IFNULL(SUM(AddNum), 0) FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type AND AddNum<0;
                """, new { uid, type });
        }
        return (add, sub);
    }




    private long GetZZZLastId(long uid, ZZZQueryType type)
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<long>("""
            SELECT Id FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type ORDER BY Id DESC LIMIT 1;
            """, new { uid, type });
    }




    private int InsertZZZQueryItems(long uid, ZZZQueryType type, List<ZZZQueryItem> items, bool all = false)
    {
        using var dapper = _databaseService.CreateConnection();
        int oldCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        using var t = dapper.BeginTransaction();
        if (all)
        {
            var months = items.GroupBy(x => (x.DateTime.Year, x.DateTime.Month)).Select(x => $"{x.Key.Year}-{x.Key.Month:D2}").Distinct().ToList();
            foreach (var month in months)
            {
                dapper.Execute($"""
                DELETE FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type AND DateTime LIKE @time;
                """, new { uid, type, time = month + "%" }, t);
            }
        }
        dapper.Execute("""
            INSERT OR REPLACE INTO ZZZQueryItem(Id, Uid, Type, Reason, AddNum, DateTime, EquipName, EquipRarity, EquipLevel, WeaponName, WeaponRarity, WeaponLevel, ClientIp, ActionName, CardType, ItemName)
            VALUES (@Id, @Uid, @Type, @Reason, @AddNum, @DateTime, @EquipName, @EquipRarity, @EquipLevel, @WeaponName, @WeaponRarity, @WeaponLevel, @ClientIp, @ActionName, @CardType, @ItemName);
            """, items, t);
        t.Commit();
        int newCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM ZZZQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        return newCount - oldCount;
    }






    public async Task<(long Add, long Sub)> UpdateZZZQueryItemsAsync(ZZZQueryType type, IProgress<int> progress, bool all = false, CancellationToken cancellationToken = default)
    {
        _selfQueryClient.EnsureInitialized();
        long uid = _selfQueryClient.UserInfo?.Uid ?? 0;
        using var dapper = _databaseService.CreateConnection();
        long lastId = GetZZZLastId(uid, type);
        long endId = 0;
        var list = new List<ZZZQueryItem>(20);
        for (int i = 1; ; i++)
        {
            progress.Report(i);
            var temp_list = await _selfQueryClient.GetZZZQueryItemsAsync(type, endId, 20, null, null, cancellationToken);
            list.AddRange(temp_list);
            if (temp_list.Count < 20)
            {
                break;
            }
            endId = temp_list[19].Id;
            if (!all && endId <= lastId)
            {
                break;
            }
        }
        InsertZZZQueryItems(uid, type, list, all);
        return GetZZZQueryItemsNumSum(uid, type);
    }



    #endregion


}
