using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Models;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Services.Gacha;

internal class GenshinGachaService : GachaLogService
{



    protected override GameBiz GameBiz { get; } = GameBiz.GenshinImpact;

    protected override string GachaTableName { get; } = "GenshinGachaItem";

    protected override IReadOnlyCollection<int> GachaTypes { get; } = new int[] { 200, 301, 302, 100 }.AsReadOnly();



    public GenshinGachaService(ILogger<GenshinGachaService> logger, DatabaseService database, GenshinGachaClient client) : base(logger, database, client)
    {

    }



    protected override List<GachaLogItemEx> GetGroupGachaLogItems(IEnumerable<GachaLogItemEx> items, GachaType type)
    {
        return type switch
        {
            GachaType.CharacterEventWish => items.Where(x => x.GachaType == GachaType.CharacterEventWish || x.GachaType == GachaType.CharacterEventWish_2).ToList(),
            _ => items.Where(x => x.GachaType == type).ToList(),
        };
    }



    protected override int InsertGachaLogItems(List<GachaLogItem> items)
    {
        using var dapper = _database.CreateConnection();
        using var t = dapper.BeginTransaction();
        var affect = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, items, t);
        t.Commit();
        return affect;
    }





}
