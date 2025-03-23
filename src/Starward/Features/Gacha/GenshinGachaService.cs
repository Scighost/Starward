using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Features.Database;
using Starward.Features.Gacha.UIGF;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
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
        foreach (IGachaType type in QueryGachaTypes)
        {
            var l = GetGachaLogItemsByQueryType(list, (IGachaType)type);
            int index = 0;
            int pity = 0;
            bool hasNoUp = GachaNoUp.Dictionary.TryGetValue($"{CurrentGameBiz}{type.Value}", out var noUp);
            foreach (var item in l)
            {
                item.Index = ++index;
                item.Pity = ++pity;
                if (item.RankType == 5)
                {
                    pity = 0;
                    item.HasUpItem = hasNoUp;
                    if (hasNoUp)
                    {
                        bool isUp = true;
                        if (noUp!.Items.TryGetValue(item.ItemId, out GachaNoUpItem? noUpItem))
                        {
                            foreach ((DateTime start, DateTime end) in noUpItem.NoUpTimes)
                            {
                                if (item.Time >= start && item.Time <= end)
                                {
                                    isUp = false;
                                    break;
                                }
                            }
                        }
                        item.IsUp = isUp;
                    }
                }
            }
        }
        return list;
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
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<UIGFGenshinGachaItem>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        foreach (var item in list)
        {
            item.UIGFGachaType = item.GachaType switch
            {
                400 => 301,
                _ => item.GachaType,
            };
        }
        DateTimeOffset time = DateTimeOffset.Now;
        var uigfObj = new UIGF3File<UIGFGenshinGachaItem>
        {
            Info = new UIAF3FileInfo
            {
                Uid = uid,
                Lang = list.Last().Lang ?? "",
                ExportTimestamp = time.ToUnixTimeSeconds(),
                ExportTime = time.ToString("yyyy-MM-dd HH:mm:ss"),
                ExportAppVersion = AppConfig.AppVersion,
                RegionTimeZone = uid.ToString()[0] switch
                {
                    '6' => -5,
                    '7' => 1,
                    _ => 8,
                },
            },
            List = list,
        };
        using FileStream fs = File.Create(output);
        await JsonSerializer.SerializeAsync(fs, uigfObj, AppConfig.JsonSerializerOptions);
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



    public override long ImportGachaLog(string file)
    {
        var str = File.ReadAllText(file);
        var obj = JsonSerializer.Deserialize<UIGF3File<UIGFGenshinGachaItem>>(str);
        if (obj != null)
        {
            string lang = obj.Info.Lang ?? "";
            long uid = obj.Info.Uid;
            foreach (var item in obj.List)
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
            var count = InsertGachaLogItems(obj.List.ToList<GachaLogItem>());
            // 成功导入祈愿记录 {count} 条
            InAppToast.MainWindow?.Success($"Uid {obj.Info.Uid}", string.Format(Lang.GenshinGachaService_ImportWishRecordsSuccessfully, count), 5000);
            return obj.Info.Uid;
        }
        return 0;
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


}
