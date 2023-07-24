using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.StarRail;
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



    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("""
            SELECT item.*, info.IconUrl Icon FROM StarRailGachaItem item LEFT JOIN StarRailGachaInfo info ON item.ItemId=info.ItemId WHERE Uid=@uid ORDER BY item.Id;
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
                if (item.RankType == 5)
                {
                    pity = 0;
                }
            }
        }
        return list;
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
        UpdateGachaItemId();
        return affeted;
    }



    public override async Task ExportGachaLogAsync(long uid, string file, string format)
    {
        if (format is "excel")
        {
            await ExportAsExcelAsync(uid, file);
        }
        else
        {
            await ExportAsJsonAsync(uid, file);
        }
    }



    private async Task ExportAsJsonAsync(long uid, string output)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<StarRailGachaItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new SRGFObj(uid, list);
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




    public override long ImportGachaLog(string file)
    {
        var str = File.ReadAllText(file);
        var obj = JsonSerializer.Deserialize<SRGFObj>(str);
        if (obj != null)
        {
            var lang = obj.info.lang ?? "";
            long uid = obj.info.uid;
            foreach (var item in obj.list)
            {
                if (item.Lang is null)
                {
                    item.Lang = lang;
                }
                if (item.Uid == 0)
                {
                    item.Uid = uid;
                }
            }
            var count = InsertGachaLogItems(obj.list.ToList<GachaLogItem>());
            // 成功导入跃迁记录 {count} 条
            NotificationBehavior.Instance.Success($"Uid {obj.info.uid}", string.Format(Lang.StarRailGachaService_ImportWarpRecordsSuccessfully, count), 5000);
            return obj.info.uid;
        }
        return 0;
    }


    public override async Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        var data = await _client.GetStarRailGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = _database.CreateConnection();
        using var t = dapper.BeginTransaction();
        const string insertSql = """
            INSERT OR REPLACE INTO StarRailGachaInfo (ItemId, ItemName, IconUrl, DamageType, Rarity, AvatarBaseType, WikiUrl, IsSystem)
            VALUES (@ItemId, @ItemName, @IconUrl, @DamageType, @Rarity, @AvatarBaseType, @WikiUrl, @IsSystem);
            """;
        dapper.Execute(insertSql, data.Avatar, t);
        dapper.Execute(insertSql, data.Equipment, t);
        t.Commit();
        UpdateGachaItemId();
        return data.Language;
    }



    private void UpdateGachaItemId()
    {
        using var dapper = _database.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO StarRailGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, GachaId, Count, Lang) 
            SELECT item.Uid, Id, Name, Time, info.ItemId, ItemType, RankType, GachaType, GachaId, Count, Lang
            FROM StarRailGachaItem item INNER JOIN StarRailGachaInfo info ON item.Name = info.ItemName WHERE item.ItemId = 0;
            """);
    }


    public override async Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = await UpdateGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = _database.CreateConnection();
        int count = dapper.Execute("""
            INSERT OR REPLACE INTO StarRailGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, GachaId, Count, Lang) 
            SELECT item.Uid, Id, info.ItemName, Time, item.ItemId, ItemType, RankType, GachaType, GachaId, Count, @Lang
            FROM StarRailGachaItem item INNER JOIN StarRailGachaInfo info ON item.ItemId = info.ItemId;
            """, new { Lang = lang });
        return (lang, count);
    }



    private class SRGFObj
    {
        public SRGFObj() { }

        public SRGFObj(long uid, List<StarRailGachaItem> list)
        {
            this.info = new SRGFInfo(uid, list);
            this.list = list;
        }

        public SRGFInfo info { get; set; }

        public List<StarRailGachaItem> list { get; set; }
    }


    private class SRGFInfo
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public long uid { get; set; }

        public string lang { get; set; }

        public int region_time_zone { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long export_timestamp { get; set; }

        public string export_app { get; set; } = "Starward";

        public string export_app_version { get; set; } = AppConfig.AppVersion ?? "";

        public string srgf_version { get; set; } = "v1.0";

        public SRGFInfo() { }

        public SRGFInfo(long uid, List<StarRailGachaItem> list)
        {
            this.uid = uid;
            lang = list.FirstOrDefault()?.Lang ?? "";
            export_timestamp = DateTimeOffset.Now.ToUnixTimeSeconds();
            region_time_zone = uid.ToString().FirstOrDefault() switch
            {
                '6' => -5,
                '7' => 1,
                _ => 8,
            };
        }
    }



}