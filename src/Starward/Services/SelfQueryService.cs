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




    private int InsertGenshinQueryItems(long uid, GenshinQueryType type, List<GenshinQueryItem> items)
    {
        using var dapper = _databaseService.CreateConnection();
        int oldCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM GenshinQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        using var t = dapper.BeginTransaction();
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






    public async Task<(long Add, long Sub)> UpdateGenshinQueryItemsAsync(GenshinQueryType type, IProgress<int> progress, CancellationToken cancellationToken = default)
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
            if (endId <= lastId)
            {
                break;
            }
        }
        InsertGenshinQueryItems(uid, type, list);
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



    private DateTime GetStarRailLastTime(long uid, StarRailQueryType type)
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<DateTime>("""
            SELECT Time FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type ORDER BY Time DESC LIMIT 1;
            """, new { uid, type });
    }




    private int InsertStarRailQueryItems(long uid, StarRailQueryType type, List<StarRailQueryItem> items)
    {
        using var dapper = _databaseService.CreateConnection();
        int oldCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        using var t = dapper.BeginTransaction();
        dapper.Execute("""
            INSERT OR REPLACE INTO StarRailQueryItem(Uid, Type, Action, AddNum, Time, RelicName, RelicLevel, RelicRarity, EquipmentName, EquipmentLevel, EquipmentRarity)
            VALUES (@Uid, @Type, @Action, @AddNum, @Time, @RelicName, @RelicLevel, @RelicRarity, @EquipmentName, @EquipmentLevel, @EquipmentRarity);
            """, items, t);
        t.Commit();
        int newCount = dapper.QueryFirstOrDefault<int>("""
            SELECT COUNT(*) FROM StarRailQueryItem WHERE Uid=@uid AND Type=@type;
            """, new { uid, type });
        return newCount - oldCount;
    }




    public async Task<(long Add, long Sub)> UpdateStarRailQueryItemsAsync(StarRailQueryType type, IProgress<int> progress, CancellationToken cancellationToken = default)
    {
        _selfQueryClient.EnsureInitialized();
        long uid = _selfQueryClient.UserInfo?.Uid ?? 0;
        using var dapper = _databaseService.CreateConnection();
        DateTime lastTime = GetStarRailLastTime(uid, type);
        DateTime endTime = DateTime.MinValue;
        var list = new List<StarRailQueryItem>(20);
        for (int i = 1; ; i++)
        {
            progress.Report(i);
            var temp_list = await _selfQueryClient.GetStarRailQueryItemsAsync(type, i, 100, cancellationToken);
            if (temp_list.Count < 100)
            {
                temp_list = temp_list.Where(x => x.Time > lastTime).ToList();
                list.AddRange(temp_list);
                break;
            }
            endTime = temp_list[99].Time;
            if (endTime <= lastTime)
            {
                temp_list = temp_list.Where(x => x.Time > lastTime).ToList();
                list.AddRange(temp_list);
                break;
            }
            list.AddRange(temp_list);
        }
        InsertStarRailQueryItems(uid, type, list);
        return GetStarRailQueryItemsNumSum(uid, type);
    }





    #endregion


}
