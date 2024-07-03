using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.ZZZ;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Gacha;

internal class ZZZGachaService : GachaLogService
{


    protected override GameBiz GameBiz { get; } = GameBiz.ZZZ;

    protected override string GachaTableName { get; } = "ZZZGachaItem";

    // todo
    protected override IReadOnlyCollection<int> GachaTypes { get; } = new int[] { 200, 301, 302, 500, 100 }.AsReadOnly();




    public ZZZGachaService(ILogger<ZZZGachaService> logger, DatabaseService database, ZZZGachaClient client) : base(logger, database, client)
    {

    }


    protected override List<GachaLogItemEx> GetGroupGachaLogItems(IEnumerable<GachaLogItemEx> items, GachaType type)
    {
        return type switch
        {
            _ => items.Where(x => x.GachaType == type).ToList(),
        };
    }



    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("""
            SELECT item.*, info.Icon FROM ZZZGachaItem item LEFT JOIN ZZZGachaInfo info ON item.ItemId=info.Id WHERE Uid=@uid ORDER BY item.Id;
            """, new { uid }).ToList();
        foreach (var type in GachaTypes)
        {
            var l = GetGroupGachaLogItems(list, (GachaType)type);
            int index = 0;
            int pity = 0;
            foreach (var item in l)
            {
                item.Index = ++index;
                item.Pity = ++pity;
                // todo
                if (item.RankType == 4)
                {
                    pity = 0;
                }
            }
        }
        return list;
    }



    public override (List<GachaTypeStats> GachaStats, List<GachaLogItemEx> ItemStats) GetGachaTypeStats(long uid)
    {
        var statsList = new List<GachaTypeStats>();
        var groupStats = new List<GachaLogItemEx>();
        using var dapper = _database.CreateConnection();
        var allItems = GetGachaLogItemEx(uid);
        if (allItems.Count > 0)
        {
            foreach (int type in GachaTypes)
            {
                var list = GetGroupGachaLogItems(allItems, (GachaType)type);
                if (list.Count == 0)
                {
                    continue;
                }
                var stats = new GachaTypeStats
                {
                    GachaType = (GachaType)type,
                    Count = list.Count,
                    Count_5 = list.Count(x => x.RankType == 4),
                    Count_4 = list.Count(x => x.RankType == 3),
                    Count_3 = list.Count(x => x.RankType == 2),
                    StartTime = list.First().Time,
                    EndTime = list.Last().Time
                };
                stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
                stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
                stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
                stats.List_5 = list.Where(x => x.RankType == 5).Reverse().ToList();
                stats.List_4 = list.Where(x => x.RankType == 4).Reverse().ToList();
                stats.Pity_5 = list.Last().Pity;
                if (list.Last().RankType == 4)
                {
                    stats.Pity_5 = 0;
                }
                stats.Average_5 = (double)(stats.Count - stats.Pity_5) / stats.Count_5;
                stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 4);

                int pity_4 = 0;
                foreach (var item in list)
                {
                    pity_4++;
                    if (item.RankType == 3)
                    {
                        item.Pity = pity_4;
                        pity_4 = 0;
                    }
                }

                statsList.Add(stats);
                if ((GachaType)type == GachaType.NoviceWish && stats.Count == 20)
                {
                    continue;
                }
                else if ((GachaType)type == GachaType.DepartureWarp && stats.Count == 50)
                {
                    continue;
                }
                else
                {
                    stats.List_5.Insert(0, new GachaLogItemEx
                    {
                        Name = Lang.GachaStatsCard_Pity,
                        Pity = stats.Pity_5,
                        Time = list.Last().Time,
                    });
                    stats.List_4.Insert(0, new GachaLogItemEx
                    {
                        Name = Lang.GachaStatsCard_Pity,
                        Pity = stats.Pity_4,
                        Time = list.Last().Time,
                    });
                }
            }
            groupStats = allItems.GroupBy(x => x.ItemId)
                                 .Select(x => { var item = x.First(); item.ItemCount = x.Count(); return item; })
                                 .OrderByDescending(x => x.RankType)
                                 .ThenByDescending(x => x.ItemCount)
                                 .ThenByDescending(x => x.Time)
                                 .ToList();
        }
        return (statsList, groupStats);
    }




    protected override int InsertGachaLogItems(List<GachaLogItem> items)
    {
        using var dapper = _database.CreateConnection();
        using var t = dapper.BeginTransaction();
        var affect = dapper.Execute("""
            INSERT OR REPLACE INTO ZZZGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, items, t);
        t.Commit();
        return affect;
    }



    public override Task ExportGachaLogAsync(long uid, string file, string format)
    {
        throw new NotImplementedException();
    }



    public override long ImportGachaLog(string file)
    {
        throw new NotImplementedException();
    }



    public override async Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        var data = await _client.GetZZZGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = _database.CreateConnection();
        using var t = dapper.BeginTransaction();
        // todo
        const string insertSql = """
            INSERT OR REPLACE INTO ZZZGachaInfo (Id, Name, Icon, Element, Level, CatId, WeaponCatId)
            VALUES (@Id, @Name, @Icon, @Element, @Level, @CatId, @WeaponCatId);
            """;
        dapper.Execute(insertSql, data.AllAvatar, t);
        dapper.Execute(insertSql, data.AllWeapon, t);
        t.Commit();
        return data.Language;
    }



    public override async Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = await UpdateGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = _database.CreateConnection();
        // todo
        int count = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            SELECT item.Uid, item.Id, info.Name, Time, ItemId, ItemType, RankType, GachaType, Count, @Lang
            FROM GenshinGachaItem item INNER JOIN GenshinGachaInfo info ON item.ItemId = info.Id;
            """, new { Lang = lang });
        return (lang, count);
    }


}
