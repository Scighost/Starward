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
using System.Threading.Tasks;

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



    public override async Task ExportGachaLogAsync(int uid, string file, string format)
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



    private async Task ExportAsJsonAsync(int uid, string output)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<UIAFItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new UIAFObj(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        await File.WriteAllTextAsync(output, str);
    }


    private async Task ExportAsExcelAsync(int uid, string output)
    {
        using var dapper = _database.CreateConnection();
        var list = GetGachaLogItemEx(uid);
        var template = Path.Combine(AppContext.BaseDirectory, @"Assets\Template\GachaLog.xlsx");
        if (File.Exists(template))
        {
            await MiniExcel.SaveAsByTemplateAsync(output, template, new { list });
        }
    }



    public override int ImportGachaLog(string file)
    {
        var str = File.ReadAllText(file);
        var obj = JsonSerializer.Deserialize<UIAFObj>(str);
        if (obj != null)
        {
            var lang = obj.info.lang ?? "";
            int uid = obj.info.uid;
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



    private class UIAFObj
    {
        public UIAFObj() { }

        public UIAFObj(int uid, List<UIAFItem> list)
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

        public List<UIAFItem> list { get; set; }
    }



    private class UIAFInfo
    {
        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString | JsonNumberHandling.WriteAsString)]
        public int uid { get; set; }

        public string lang { get; set; }

        [JsonNumberHandling(JsonNumberHandling.AllowReadingFromString)]
        public long export_timestamp { get; set; }

        public string export_time { get; set; }

        public string export_app { get; set; } = "Starward";

        public string export_app_version { get; set; } = AppConfig.AppVersion ?? "";

        public string uigf_version { get; set; } = "v2.2";

        public UIAFInfo() { }

        public UIAFInfo(int uid, List<UIAFItem> list)
        {
            this.uid = uid;
            lang = list.FirstOrDefault()?.Lang ?? "";
            var time = DateTimeOffset.Now;
            export_time = time.ToString("yyyy-MM-dd HH:mm:ss");
            export_timestamp = time.ToUnixTimeSeconds();
        }
    }



    private class UIAFItem : GenshinGachaItem
    {
        public string uigf_gacha_type { get; set; }
    }



}
