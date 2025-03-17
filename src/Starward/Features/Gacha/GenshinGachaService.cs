using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Features.Database;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.Gacha;

internal class GenshinGachaService : GachaLogService
{



    protected override GameBiz CurrentGameBiz { get; } = GameBiz.hk4e;

    protected override string GachaTableName { get; } = "GenshinGachaItem";




    public GenshinGachaService(ILogger<GenshinGachaService> logger, GenshinGachaClient client) : base(logger, client)
    {

    }



    protected override List<GachaLogItemEx> GetGachaLogItemsByQueryType(IEnumerable<GachaLogItemEx> items, IGachaType type)
    {
        return type.Value switch
        {
            GenshinGachaType.CharacterEventWish => items.Where(x => x.GachaType == GenshinGachaType.CharacterEventWish || x.GachaType == GenshinGachaType.CharacterEventWish_2).ToList(),
            _ => items.Where(x => x.GachaType == type.Value).ToList(),
        };
    }


    public override List<GachaLogItemEx> GetGachaLogItemEx(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("""
            SELECT item.*, info.Icon FROM GenshinGachaItem item LEFT JOIN GenshinGachaInfo info ON item.ItemId=info.Id WHERE Uid=@uid ORDER BY item.Id;
            """, new { uid }).ToList();
        foreach (var type in QueryGachaTypes)
        {
            var l = GetGachaLogItemsByQueryType(list, (IGachaType)type);
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


    public Dictionary<int, GenshinGachaInfo> GetItemsInfo()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<GenshinGachaInfo>("SELECT Id, Name, Level FROM GenshinGachaInfo").ToDictionary(item => item.Id);
    }


    protected override int InsertGachaLogItems(List<GachaLogItem> items)
    {
        using var dapper = DatabaseService.CreateConnection();
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
            await ExportAsExcelAsync(uid, file);
        else if (format is "json")
            await ExportAsJsonAsync(uid, file);
        else
            await ExportAsJsonoldAsync(uid, file);
    }



    private async Task ExportAsJsonAsync(long uid, string output)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<UIGFItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new UIGF40Obj(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        await File.WriteAllTextAsync(output, str);
    }


    private async Task ExportAsJsonoldAsync(long uid, string output)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<UIGFItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new UIGFObj(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        await File.WriteAllTextAsync(output, str);
    }


    private async Task ExportAsExcelAsync(long uid, string output)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = GetGachaLogItemEx(uid);
        var template = Path.Combine(AppContext.BaseDirectory, @"Assets\Template\GachaLog.xlsx");
        if (File.Exists(template))
        {
            await MiniExcel.SaveAsByTemplateAsync(output, template, new { list });
        }
    }



    public override List<GachaLogItem> CheckUIGFItems(List<GachaLogItem> list, long uid, string lang)
    {
        var infos = GetItemsInfo();
        foreach (var item in list)
        {
            infos.TryGetValue(item.ItemId, out GenshinGachaInfo? info);
            if (item.GachaType == 0 || item.ItemId == 0 || item.Id == 0)
                throw new JsonException("Missing required properties.");
            item.Uid = uid;
            if (item.Count == 0)
                item.Count = 1;
            item.Name ??= info?.Name ?? "";
            item.ItemType ??= "";
            if (item.RankType == 0)
                item.RankType = info?.Level ?? 0;
            item.Lang ??= lang;
        }
        return list;
    }



    public override List<long> ImportGachaLog(string file)
    {
        var str = File.ReadAllText(file);
        var count = 0;
        List<long> uids = [];
        using (JsonDocument doc = JsonDocument.Parse(str))
        {
            if (doc.RootElement.TryGetProperty("info", out JsonElement infoElement) && infoElement.TryGetProperty("version", out _))
            {
                var obj = JsonSerializer.Deserialize<UIGF40Obj>(str)!.hk4e;
                foreach (var user in obj)
                {
                    var lang = user.lang ?? "";
                    var uid = user.uid;
                    var list = CheckUIGFItems(user.list.ToList<GachaLogItem>(), uid, lang);
                    uids.Add(uid);
                    count += InsertGachaLogItems(list);
                }
            }
            else if (infoElement.TryGetProperty("uigf_version", out _))
            {
                var obj = JsonSerializer.Deserialize<UIGFObj>(str)!;
                var lang = obj.info.lang ?? "";
                var uid = obj.info.uid;
                uids.Add(uid);
                var list = CheckUIGFItems(obj.list.ToList<GachaLogItem>(), uid, lang);
                count += InsertGachaLogItems(list);
            }
        }
        if (uids.Count == 0)
            throw new JsonException("Unsupported Json Structures.");
        // 成功导入祈愿记录 {count} 条
        InAppToast.MainWindow?.Success($"Uid {string.Join(" ", uids)}", string.Format(Lang.GenshinGachaService_ImportWishRecordsSuccessfully, count), 5000);
        return uids;
    }



    public override async Task<string> UpdateGachaInfoAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        var data = await _client.GetGenshinGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
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
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            SELECT item.Uid, item.Id, item.Name, Time, info.Id, ItemType, RankType, GachaType, Count, Lang
            FROM GenshinGachaItem item INNER JOIN GenshinGachaInfo info ON item.Name = info.Name WHERE item.ItemId = 0;
            """);
    }


    public override async Task<(string Language, int Count)> ChangeGachaItemNameAsync(GameBiz gameBiz, string lang, CancellationToken cancellationToken = default)
    {
        lang = await UpdateGachaInfoAsync(gameBiz, lang, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
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

        public string uigf_version { get; set; } = "v3.0";

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



    private class UIGF40Obj
    {
        public UIGF40Obj() { }

        public UIGF40Obj(long uid, List<UIGFItem> list)
        {
            this.info = new UIGF40Info();
            this.hk4e = [new UIGF40Game(uid, list)];
        }

        public UIGF40Info info { get; set; }

        public List<UIGF40Game> hk4e { get; set; }
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

        public UIGF40Game(long uid, List<UIGFItem> list)
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

        public List<UIGFItem> list { get; set; }
    }



    private class UIGFItem : GenshinGachaItem
    {
        public string uigf_gacha_type
        {
            get => GachaType switch
            {
                GenshinGachaType.CharacterEventWish_2 => GenshinGachaType.CharacterEventWish.ToString(),
                _ => GachaType.ToString(),
            };
        }
    }



}
