using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;
using Starward.Helpers;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services.Gacha;

internal class ZZZGachaService : GachaLogService
{


    protected override GameBiz CurrentGameBiz { get; } = GameBiz.nap;

    protected override string GachaTableName { get; } = "ZZZGachaItem";



    public ZZZGachaService(ILogger<ZZZGachaService> logger, DatabaseService database, ZZZGachaClient client) : base(logger, database, client)
    {

    }


    protected override List<GachaLogItemEx> GetGachaLogItemsByQueryType(IEnumerable<GachaLogItemEx> items, IGachaType type)
    {
        return type switch
        {
            _ => items.Where(x => x.GachaType == type.Value).ToList(),
        };
    }



    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("""
            SELECT item.* FROM ZZZGachaItem item WHERE Uid=@uid ORDER BY item.Id;
            """, new { uid }).ToList();
        foreach (var type in QueryGachaTypes)
        {
            var l = GetGachaLogItemsByQueryType(list, type);
            int index = 0;
            int pity = 0;
            foreach (var item in l)
            {
                item.Index = ++index;
                item.Pity = ++pity;
                if (item.RankType == 4)
                {
                    pity = 0;
                }
            }
        }
        return list;
    }



    // todo
    public Dictionary<int, ZZZGachaInfo> GetItemsInfo()
    {
        throw new NotImplementedException();
        //using var dapper = _database.CreateConnection();
        //return dapper.Query<ZZZGachaInfo>("SELECT ItemId, ItemName, Rarity FROM ZZZGachaItem").ToDictionary(item => item.Id);
    }



    public override (List<GachaTypeStats> GachaStats, List<GachaLogItemEx> ItemStats) GetGachaTypeStats(long uid)
    {
        var statsList = new List<GachaTypeStats>();
        var groupStats = new List<GachaLogItemEx>();
        using var dapper = _database.CreateConnection();
        var allItems = GetGachaLogItemEx(uid);
        if (allItems.Count > 0)
        {
            foreach (IGachaType type in QueryGachaTypes)
            {
                var list = GetGachaLogItemsByQueryType(allItems, type);
                if (list.Count == 0)
                {
                    continue;
                }
                var stats = new GachaTypeStats
                {
                    GachaType = type.Value,
                    GachaTypeText = type.ToLocalization(),
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
                stats.List_5 = list.Where(x => x.RankType == 4).Reverse().ToList();
                stats.List_4 = list.Where(x => x.RankType == 3).Reverse().ToList();
                stats.Pity_5 = list.Last().Pity;
                if (list.Last().RankType == 4)
                {
                    stats.Pity_5 = 0;
                }
                stats.Average_5 = (double)(stats.Count - stats.Pity_5) / stats.Count_5;
                stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 3);

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
                if (CurrentGameBiz == GameBiz.hk4e && type.Value == GenshinGachaType.NoviceWish && stats.Count == 20)
                {
                    continue;
                }
                else if (CurrentGameBiz == GameBiz.hkrpg && type.Value == StarRailGachaType.DepartureWarp && stats.Count == 50)
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



    public override async Task ExportGachaLogAsync(long uid, string file, string format)
    {
        if (format is "excel")
            await ExportAsExcelAsync(uid, file);
        else
            await ExportAsJsonAsync(uid, file);
    }



    private async Task ExportAsJsonAsync(long uid, string output)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<ZZZGachaItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new UIGF40Obj(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        await File.WriteAllTextAsync(output, str);
    }


    private async Task ExportAsExcelAsync(long uid, string output)
    {
        using var dapper = _database.CreateConnection();
        var list = GetGachaLogItemEx(uid);
        var template = Path.Combine(AppContext.BaseDirectory, @"Assets\Template\GachaLog.xlsx");
        if (File.Exists(template))
        {
            await MiniExcel.SaveAsByTemplateAsync(output, template, new { list });
        }
    }



    // todo
    public override List<GachaLogItem> CheckUIGFItems(List<GachaLogItem> list, long uid, string lang)
    {
        // var infos = GetItemsInfo();
        // foreach (var item in list)
        // {
        //     infos.TryGetValue(item.ItemId, out ZZZGachaInfo? info);
        //     if (item.GachaType == 0 || item.ItemId == 0 || item.Id == 0)
        //         throw new JsonException("Missing required properties.");
        //     item.Uid = uid;
        //     if (item.Count == 0)
        //         item.Count = 1;
        //     item.Name ??= info?.Name ?? "";
        //     item.ItemType ??= "";
        //     if (item.RankType == 0)
        //         item.RankType = info?.Level ?? 0;
        //     item.Lang ??= lang;
        // }
        return list;
    }



    public override List<long> ImportGachaLog(string file)
    {
        var str = File.ReadAllText(file);
        var count = 0;
        List<long> uids = [];
        var obj = (JsonSerializer.Deserialize<UIGF40Obj>(str)?.nap) ?? throw new JsonException("Unsupported Json Structures.");
        foreach (var user in obj)
        {
            var lang = user.lang ?? "";
            var uid = user.uid;
            var list = CheckUIGFItems(user.list.ToList<GachaLogItem>(), uid, lang);
            uids.Add(uid);
            count += InsertGachaLogItems(list);
        }
        if (uids.Count == 0)
            throw new JsonException("Unsupported Json Structures.");
        // 成功导入调频记录 {count} 条
        NotificationBehavior.Instance.Success($"Uid {string.Join(" ", uids)}", string.Format(Lang.ZZZGachaService_ImportSignalSearchSuccessfully, count), 5000);
        return uids;
    }


    // todo
    public override async Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        return lang;
        // var data = await _client.GetZZZGachaInfoAsync(gameBiz, lang, cancellationToken);
        // using var dapper = _database.CreateConnection();
        // using var t = dapper.BeginTransaction();
        // // todo
        // const string insertSql = """
        //     INSERT OR REPLACE INTO ZZZGachaInfo (Id, Name, Icon, Element, Level, CatId, WeaponCatId)
        //     VALUES (@Id, @Name, @Icon, @Element, @Level, @CatId, @WeaponCatId);
        //     """;
        // dapper.Execute(insertSql, data.AllAvatar, t);
        // dapper.Execute(insertSql, data.AllWeapon, t);
        // t.Commit();
        // return data.Language;
    }


    // todo
    public override async Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        return (lang, 0);
        // lang = await UpdateGachaInfoAsync(gameBiz, lang, cancellationToken);
        // using var dapper = _database.CreateConnection();
        // // todo
        // int count = dapper.Execute("""
        //     INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
        //     SELECT item.Uid, item.Id, info.Name, Time, ItemId, ItemType, RankType, GachaType, Count, @Lang
        //     FROM GenshinGachaItem item INNER JOIN GenshinGachaInfo info ON item.ItemId = info.Id;
        //     """, new { Lang = lang });
        // return (lang, count);
    }



    private class UIGF40Obj
    {
        public UIGF40Obj() { }

        public UIGF40Obj(long uid, List<ZZZGachaItem> list)
        {
            this.info = new UIGF40Info();
            this.nap = [new UIGF40Game(uid, list)];
        }

        public UIGF40Info info { get; set; }

        public List<UIGF40Game> nap { get; set; }
    }


    private class UIGF40Info
    {
        public string version { get; set; } = "v4.0";

        public string export_app { get; set; } = "Starward";

        public string export_app_version { get; set; } = AppConfig.AppVersion ?? "";

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long export_timestamp { get; set; }

        public UIGF40Info()
        {
            export_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
        }
    }


    private class UIGF40Game
    {
        public UIGF40Game() { }

        public UIGF40Game(long uid, List<ZZZGachaItem> list)
        {
            this.uid = uid;
            timezone = uid.ToString().FirstOrDefault() switch
            {
                '6' => -5,
                '7' => 1,
                _ => 8,
            };
            lang = list.FirstOrDefault()?.Lang ?? "";
            this.list = list;
        }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public long uid { get; set; }

        public int timezone { get; set; }

        public string lang { get; set; }

        public List<ZZZGachaItem> list { get; set; }
    }



}
