using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
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

internal class GenshinGachaService : GachaLogService
{



    protected override GameBiz GameBiz { get; } = GameBiz.GenshinImpact;

    protected override string GachaTableName { get; } = "GenshinGachaItem";

    protected override IReadOnlyCollection<int> GachaTypes { get; } = new int[] { 200, 301, 302, 500, 100 }.AsReadOnly();



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


    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("""
            SELECT item.*, info.Icon FROM GenshinGachaItem item LEFT JOIN GenshinGachaInfo info ON item.ItemId=info.Id WHERE Uid=@uid ORDER BY item.Id;
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
        var affect = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, items, t);
        t.Commit();
        UpdateGachaItemId();
        return affect;
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
        var list = dapper.Query<UIGFItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new UIGFObj(uid, list);
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
        var obj = JsonSerializer.Deserialize<UIGFObj>(str);
        if (obj != null)
        {
            string lang = obj.info.lang ?? "";
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
            // 成功导入祈愿记录 {count} 条
            NotificationBehavior.Instance.Success($"Uid {obj.info.uid}", string.Format(Lang.GenshinGachaService_ImportWishRecordsSuccessfully, count), 5000);
            return obj.info.uid;
        }
        return 0;
    }



    public override async Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        var data = await _client.GetGenshinGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = _database.CreateConnection();
        using var t = dapper.BeginTransaction();
        const string insertSql = """
            INSERT OR REPLACE INTO GenshinGachaInfo (Id, Name, Icon, Element, Level, CatId, WeaponCatId)
            VALUES (@Id, @Name, @Icon, @Element, @Level, @CatId, @WeaponCatId);
            """;
        dapper.Execute(insertSql, data.AllAvatar, t);
        dapper.Execute(insertSql, data.AllWeapon, t);
        t.Commit();
        UpdateGachaItemId();
        return data.Language;
    }


    private void UpdateGachaItemId()
    {
        using var dapper = _database.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            SELECT item.Uid, item.Id, item.Name, Time, info.Id, ItemType, RankType, GachaType, Count, Lang
            FROM GenshinGachaItem item INNER JOIN GenshinGachaInfo info ON item.Name = info.Name WHERE item.ItemId = 0;
            """);
    }


    public override async Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = await UpdateGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = _database.CreateConnection();
        int count = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            SELECT item.Uid, item.Id, info.Name, Time, ItemId, ItemType, RankType, GachaType, Count, @Lang
            FROM GenshinGachaItem item INNER JOIN GenshinGachaInfo info ON item.ItemId = info.Id;
            """, new { Lang = lang });
        return (lang, count);
    }



    private class UIGFObj
    {
        public UIGFObj() { }

        public UIGFObj(long uid, List<UIGFItem> list)
        {
            this.info = new UIAFInfo(uid, list);
            foreach (var item in list)
            {
                item.uigf_gacha_type = item.GachaType switch
                {
                    GachaType.CharacterEventWish_2 => ((int)GachaType.CharacterEventWish).ToString(),
                    _ => ((int)item.GachaType).ToString(),
                };
            }
            this.list = list;
        }

        public UIAFInfo info { get; set; }

        public List<UIGFItem> list { get; set; }
    }



    private class UIAFInfo
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public long uid { get; set; }

        public string lang { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long export_timestamp { get; set; }

        public string export_time { get; set; }

        public string export_app { get; set; } = "Starward";

        public string export_app_version { get; set; } = AppConfig.AppVersion ?? "";

        public string uigf_version { get; set; } = "v2.3";

        public int? region_time_zone { get; set; }

        public UIAFInfo() { }

        public UIAFInfo(long uid, List<UIGFItem> list)
        {
            this.uid = uid;
            lang = list.FirstOrDefault()?.Lang ?? "";
            var time = DateTimeOffset.Now;
            export_time = time.ToString("yyyy-MM-dd HH:mm:ss");
            export_timestamp = time.ToUnixTimeSeconds();
            region_time_zone = uid.ToString().FirstOrDefault() switch
            {
                >= '1' and <= '5' or '8' or '9' => 8,
                '6' => -5,
                '7' => 1,
                _ => null,
            };
        }
    }



    private class UIGFItem : GenshinGachaItem
    {
        public string uigf_gacha_type { get; set; }
    }



}
