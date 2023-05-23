using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.StarRail;
using Starward.Models;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Services.Gacha;

internal class StarRailGachaService : GachaLogService
{


    protected override GameBiz GameBiz { get; } = GameBiz.StarRail;

    protected override string GachaTableName { get; } = "StarRailGachaItem";

    protected override IReadOnlyCollection<int> GachaTypes { get; } = new int[] { 1, 11, 12, 2 }.AsReadOnly();


    public StarRailGachaService(ILogger<StarRailGachaService> logger, DatabaseService database, StarRailGachaClient client) : base(logger, database, client)
    {

    }



    protected override List<GachaLogItemEx> GetGroupGachaLogItems(IEnumerable<GachaLogItemEx> items, GachaType type)
    {
        return type switch
        {
            _ => items.Where(x => x.GachaType == type).ToList(),
        };
    }



    protected override int InsertGachaLogItems(List<GachaLogItem> items)
    {
        using var dapper = _database.CreateConnection();
        using var t = dapper.BeginTransaction();
        var affeted = dapper.Execute("""
            INSERT OR REPLACE INTO StarRailGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, GachaId, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @GachaId, @Count, @Lang);
            """, items, t);
        t.Commit();
        return affeted;
    }



}