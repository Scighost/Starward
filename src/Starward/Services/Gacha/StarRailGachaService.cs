﻿using Dapper;
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


    protected override GameBiz CurrentGameBiz { get; } = GameBiz.hkrpg;

    protected override string GachaTableName { get; } = "StarRailGachaItem";



    public StarRailGachaService(ILogger<StarRailGachaService> logger, DatabaseService database, StarRailGachaClient client) : base(logger, database, client)
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
            SELECT item.*, info.IconUrl Icon FROM StarRailGachaItem item LEFT JOIN StarRailGachaInfo info ON item.ItemId=info.ItemId WHERE Uid=@uid ORDER BY item.Id;
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
                if (item.RankType == 5)
                {
                    pity = 0;
                }
            }
        }
        return list;
    }



    public Dictionary<int, StarRailGachaInfo> GetItemsInfo()
    {
        using var dapper = _database.CreateConnection();
        return dapper.Query<StarRailGachaInfo>("SELECT ItemId, ItemName, Rarity FROM StarRailGachaInfo").ToDictionary(item => item.ItemId);
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
            await ExportAsExcelAsync(uid, file);
        else if (format is "json")
            await ExportAsJsonAsync(uid, file);
        else
            await ExportAsJsonoldAsync(uid, file);
    }



    private async Task ExportAsJsonAsync(long uid, string output)
    {
        using var dapper = _database.CreateConnection();
        var list = dapper.Query<StarRailGachaItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new UIGF40Obj(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        await File.WriteAllTextAsync(output, str);
    }


    private async Task ExportAsJsonoldAsync(long uid, string output)
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




    public override List<GachaLogItem> CheckUIGFItems(List<GachaLogItem> list, long uid, string lang)
    {
        var infos = GetItemsInfo();
        foreach (var item in list)
        {
            infos.TryGetValue(item.ItemId, out StarRailGachaInfo? info);
            if (item.GachaType == 0 || item.ItemId == 0 || item.Id == 0)
                throw new JsonException("Missing required properties.");
            item.Uid = uid;
            if (item.Count == 0)
                item.Count = 1;
            item.Name ??= info?.ItemName ?? "";
            item.ItemType ??= "";
            if (item.RankType == 0)
                item.RankType = info?.Rarity ?? 0;
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
                var options = new JsonSerializerOptions
                {
                    Converters = { new StarRailGachaItemConverter() }
                };
                var obj = JsonSerializer.Deserialize<UIGF40Obj>(str, options)!.hkrpg;
                foreach (var user in obj)
                {
                    var lang = user.lang ?? "";
                    var uid = user.uid;
                    var list = CheckUIGFItems(user.list.ToList<GachaLogItem>(), uid, lang);
                    uids.Add(uid);
                    count += InsertGachaLogItems(list);
                }
            }
            else if (infoElement.TryGetProperty("srgf_version", out _))
            {
                var obj = JsonSerializer.Deserialize<SRGFObj>(str)!;
                var lang = obj.info.lang ?? "";
                var uid = obj.info.uid;
                uids.Add(uid);
                var list = CheckUIGFItems(obj.list.ToList<GachaLogItem>(), uid, lang);
                count += InsertGachaLogItems(list);
            }
        }
        if (uids.Count == 0)
            throw new JsonException("Unsupported Json Structures.");
        // 成功导入跃迁记录 {count} 条
        NotificationBehavior.Instance.Success($"Uid {string.Join(" ", uids)}", string.Format(Lang.StarRailGachaService_ImportWarpRecordsSuccessfully, count), 5000);
        return uids;
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



    private class UIGF40Obj
    {
        public UIGF40Obj() { }

        public UIGF40Obj(long uid, List<StarRailGachaItem> list)
        {
            this.info = new UIGF40Info();
            this.hkrpg = [new UIGF40Game(uid, list)];
        }

        public UIGF40Info info { get; set; }

        public List<UIGF40Game> hkrpg { get; set; }
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

        public UIGF40Game(long uid, List<StarRailGachaItem> list)
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

        public List<StarRailGachaItem> list { get; set; }
    }



}