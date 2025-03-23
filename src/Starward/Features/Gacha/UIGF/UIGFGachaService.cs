using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.Features.Gacha.UIGF;

internal class UIGFGachaService
{

    private readonly ILogger<UIGFGachaService> _logger;


    public UIGFGachaService(ILogger<UIGFGachaService> logger)
    {
        _logger = logger;
    }




    #region Export


    public List<GachaUidArchiveDisplay> GetLocalGachaArchives()
    {
        using var dapper = DatabaseService.CreateConnection();
        List<GachaUidArchiveDisplay> result =
        [
            .. GetLocalGachaArchivesForGenshin(),
            .. GetLocalGachaArchivesForStarRail(),
            .. GetLocalGachaArchivesForZZZ(),
        ];
        return result;
    }



    private List<GachaUidArchiveDisplay> GetLocalGachaArchivesForGenshin()
    {
        using var dapper = DatabaseService.CreateConnection();
        List<GachaUidArchiveDisplay> result = new();
        var uidList = dapper.Query<long>($"SELECT DISTINCT Uid FROM GenshinGachaItem;");
        foreach (long uid in uidList)
        {
            int count = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM GenshinGachaItem WHERE Uid=@Uid;", new { Uid = uid });
            GachaLogItem lastItem = dapper.QueryFirst<GachaLogItem>($"SELECT * FROM GenshinGachaItem WHERE Uid=@Uid ORDER BY Time DESC LIMIT 1;", new { Uid = uid });
            var display = new GachaUidArchiveDisplay
            {
                Game = GameBiz.hk4e,
                GameIcon = "ms-appx:///Assets/Image/icon_ys.jpg",
                Uid = uid,
                Count = count,
                LastItemGachaType = ((GenshinGachaType)lastItem.GachaType).ToLocalization(),
                LastItemName = lastItem.Name,
                LastItemTime = lastItem.Time
            };
            result.Add(display);
        }
        return result;
    }


    private List<GachaUidArchiveDisplay> GetLocalGachaArchivesForStarRail()
    {
        using var dapper = DatabaseService.CreateConnection();
        List<GachaUidArchiveDisplay> result = new();
        var uidList = dapper.Query<long>($"SELECT DISTINCT Uid FROM StarRailGachaItem;");
        foreach (long uid in uidList)
        {
            int count = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM StarRailGachaItem WHERE Uid=@Uid;", new { Uid = uid });
            GachaLogItem lastItem = dapper.QueryFirst<GachaLogItem>($"SELECT * FROM StarRailGachaItem WHERE Uid=@Uid ORDER BY Time DESC LIMIT 1;", new { Uid = uid });
            var display = new GachaUidArchiveDisplay
            {
                Game = GameBiz.hkrpg,
                GameIcon = "ms-appx:///Assets/Image/icon_sr.jpg",
                Uid = uid,
                Count = count,
                LastItemGachaType = ((StarRailGachaType)lastItem.GachaType).ToLocalization(),
                LastItemName = lastItem.Name,
                LastItemTime = lastItem.Time
            };
            result.Add(display);
        }
        return result;
    }


    private List<GachaUidArchiveDisplay> GetLocalGachaArchivesForZZZ()
    {
        using var dapper = DatabaseService.CreateConnection();
        List<GachaUidArchiveDisplay> result = new();
        var uidList = dapper.Query<long>($"SELECT DISTINCT Uid FROM ZZZGachaItem;");
        foreach (long uid in uidList)
        {
            int count = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM ZZZGachaItem WHERE Uid=@Uid;", new { Uid = uid });
            GachaLogItem lastItem = dapper.QueryFirst<GachaLogItem>($"SELECT * FROM ZZZGachaItem WHERE Uid=@Uid ORDER BY Time DESC LIMIT 1;", new { Uid = uid });
            var display = new GachaUidArchiveDisplay
            {
                Game = GameBiz.nap,
                GameIcon = "ms-appx:///Assets/Image/icon_zzz.jpg",
                Uid = uid,
                Count = count,
                LastItemGachaType = ((ZZZGachaType)lastItem.GachaType).ToLocalization(),
                LastItemName = lastItem.Name,
                LastItemTime = lastItem.Time
            };
            result.Add(display);
        }
        return result;
    }



    public async Task ExportUIGF4Async(string path, params IEnumerable<GachaUidArchiveDisplay> archives)
    {
        var uigfObj = new UIGF4File();
        foreach (GachaUidArchiveDisplay archive in archives)
        {
            if (archive.Game == GameBiz.hk4e)
            {
                uigfObj.hk4eGachaArchives!.Add(GetUIGFGachaArchiveForGenshin(archive.Uid));
            }
            if (archive.Game == GameBiz.hkrpg)
            {
                uigfObj.hkrpgGachaArchives!.Add(GetUIGFGachaArchiveForStarRail(archive.Uid));
            }
            if (archive.Game == GameBiz.nap)
            {
                uigfObj.napGachaArchives!.Add(GetUIGFGachaArchiveForZZZ(archive.Uid));
            }
        }
        using FileStream fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, uigfObj, AppConfig.JsonSerializerOptions);
    }



    private UIGF4GachaArchive<UIGFGenshinGachaItem> GetUIGFGachaArchiveForGenshin(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        IEnumerable<UIGFGenshinGachaItem> list = dapper.Query<UIGFGenshinGachaItem>($"SELECT * FROM GenshinGachaItem WHERE Uid=@Uid ORDER BY Id;", new { Uid = uid });
        foreach (UIGFGenshinGachaItem item in list)
        {
            item.UIGFGachaType = item.GachaType switch
            {
                400 => 301,
                _ => item.GachaType,
            };
        }
        UIGF4GachaArchive<UIGFGenshinGachaItem> archive = new()
        {
            Uid = uid,
            List = list.ToList(),
            Lang = list.LastOrDefault()?.Lang ?? "",
        };
        archive.Timezone = uid.ToString()[0] switch
        {
            '6' => -5,
            '7' => 1,
            _ => 8,
        };
        return archive;
    }



    private UIGF4GachaArchive<StarRailGachaItem> GetUIGFGachaArchiveForStarRail(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        IEnumerable<StarRailGachaItem> list = dapper.Query<StarRailGachaItem>($"SELECT * FROM StarRailGachaItem WHERE Uid=@Uid ORDER BY Id;", new { Uid = uid });
        UIGF4GachaArchive<StarRailGachaItem> archive = new()
        {
            Uid = uid,
            List = list.ToList(),
            Lang = list.LastOrDefault()?.Lang ?? "",
        };
        archive.Timezone = uid.ToString()[0] switch
        {
            '6' => -5,
            '7' => 1,
            _ => 8,
        };
        return archive;
    }



    private UIGF4GachaArchive<ZZZGachaItem> GetUIGFGachaArchiveForZZZ(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        IEnumerable<ZZZGachaItem> list = dapper.Query<ZZZGachaItem>($"SELECT * FROM ZZZGachaItem WHERE Uid=@Uid ORDER BY Id;", new { Uid = uid });
        UIGF4GachaArchive<ZZZGachaItem> archive = new()
        {
            Uid = uid,
            List = list.ToList(),
            Lang = list.LastOrDefault()?.Lang ?? "",
        };
        return archive;
    }


    #endregion




    #region Import



    public async Task<List<GachaUidArchiveDisplay>> ImportFileAsync(string path)
    {
        using FileStream fs = File.OpenRead(path);
        UIGF4File? uigf4Obj = await JsonSerializer.DeserializeAsync<UIGF4File>(fs);
        _ = uigf4Obj ?? throw new NullReferenceException("Cannot parse the select file: Parsed result is null.");
        List<GachaUidArchiveDisplay> list = new();
        foreach (UIGF4GachaArchive<UIGFGenshinGachaItem> item in uigf4Obj.hk4eGachaArchives ?? [])
        {
            if (item.List.Count > 0)
            {
                UIGFGenshinGachaItem last = item.List.OrderBy(x => x.Id).Last();
                GachaUidArchiveDisplay archive = new()
                {
                    Game = GameBiz.hk4e,
                    GameIcon = "ms-appx:///Assets/Image/icon_ys.jpg",
                    Uid = item.Uid,
                    hke4List = item.List,
                    Count = item.List.Count,
                    LastItemGachaType = ((GenshinGachaType)last.GachaType).ToLocalization(),
                    LastItemName = last.Name,
                    LastItemTime = last.Time,
                    LastItemTimeOffest = last.Time,
                };
                list.Add(archive);
            }
        }
        foreach (UIGF4GachaArchive<StarRailGachaItem> item in uigf4Obj.hkrpgGachaArchives ?? [])
        {
            if (item.List.Count > 0)
            {
                StarRailGachaItem last = item.List.OrderBy(x => x.Id).Last();
                GachaUidArchiveDisplay archive = new()
                {
                    Game = GameBiz.hkrpg,
                    GameIcon = "ms-appx:///Assets/Image/icon_sr.jpg",
                    Uid = item.Uid,
                    hkrpgList = item.List,
                    Count = item.List.Count,
                    LastItemGachaType = ((StarRailGachaType)last.GachaType).ToLocalization(),
                    LastItemName = last.Name,
                    LastItemTime = last.Time,
                    LastItemTimeOffest = last.Time,
                };
                list.Add(archive);
            }
        }
        foreach (UIGF4GachaArchive<ZZZGachaItem> item in uigf4Obj.napGachaArchives ?? [])
        {
            if (item.List.Count > 0)
            {
                ZZZGachaItem last = item.List.OrderBy(x => x.Id).Last();
                GachaUidArchiveDisplay archive = new()
                {
                    Game = GameBiz.nap,
                    GameIcon = "ms-appx:///Assets/Image/icon_zzz.jpg",
                    Uid = item.Uid,
                    napList = item.List,
                    Count = item.List.Count,
                    LastItemGachaType = ((ZZZGachaType)last.GachaType).ToLocalization(),
                    LastItemName = last.Name,
                    LastItemTime = last.Time,
                    LastItemTimeOffest = last.Time,
                };
                list.Add(archive);
            }
        }
        return list;
    }





    public async Task ImportAsync(params IEnumerable<GachaUidArchiveDisplay> archives)
    {
        foreach (GachaUidArchiveDisplay archive in archives)
        {
            try
            {
                archive.Result = null;
                archive.Error = null;
                if (archive.Game == GameBiz.hk4e)
                {
                    ImportForGenshin(archive);
                }
                if (archive.Game == GameBiz.hkrpg)
                {
                    ImportForStarRail(archive);
                }
                if (archive.Game == GameBiz.nap)
                {
                    ImportForZZZ(archive);
                }
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                archive.Error = ex.Message;
            }
        }
    }



    private void ImportForGenshin(GachaUidArchiveDisplay archive)
    {
        List<GenshinGachaItem> list = new();
        DateTime TIME = new DateTime(2020, 9, 1);
        bool noName = false;
        foreach (UIGFGenshinGachaItem item in archive.hke4List ?? [])
        {
            var clone = item.Clone();
            if (clone.Id == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "id"));
            }
            if (clone.ItemId == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "item_id"));
            }
            if (clone.GachaType == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "gacha_type"));
            }
            if (clone.Time < TIME)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "time"));
            }
            if (clone.RankType == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "rank_type"));
            }
            if (string.IsNullOrWhiteSpace(clone.Name))
            {
                noName = true;
            }
            if (clone.Uid == 0)
            {
                clone.Uid = archive.Uid;
            }
            else if (clone.Uid != archive.Uid)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_UidMismatchDetectedExpected0ButFound1, archive.Uid, clone.Uid));
            }
            clone.Time = item.Time.AddHours(archive.Timezone);
            list.Add(clone);
        }
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        var affect = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, list, t);
        t.Commit();
        _logger.LogInformation("Imported {count} gacha records for {game}.", affect, archive.Game);
        archive.Result = noName ? Lang.UIGFGachaService_ImportSuccessfulButNoRecordItemName : Lang.UIGFGachaService_ImportSuccessful;
    }



    private void ImportForStarRail(GachaUidArchiveDisplay archive)
    {
        List<StarRailGachaItem> list = new();
        DateTime TIME = new DateTime(2023, 4, 1);
        bool noName = false;
        foreach (StarRailGachaItem item in archive.hkrpgList ?? [])
        {
            var clone = item.Clone();
            if (clone.Id == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "id"));
            }
            if (clone.ItemId == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "item_id"));
            }
            if (clone.GachaType == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "gacha_type"));
            }
            if (clone.Time < TIME)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "time"));
            }
            if (clone.RankType == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "rank_type"));
            }
            if (clone.GachaId == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "gacha_id"));
            }
            if (string.IsNullOrWhiteSpace(clone.Name))
            {
                noName = true;
            }
            if (clone.Uid == 0)
            {
                clone.Uid = archive.Uid;
            }
            else if (clone.Uid != archive.Uid)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_UidMismatchDetectedExpected0ButFound1, archive.Uid, clone.Uid));
            }
            clone.Time = item.Time.AddHours(archive.Timezone);
            list.Add(clone);
        }
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        var affect = dapper.Execute("""
            INSERT OR REPLACE INTO StarRailGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, GachaId, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @GachaId, @Count, @Lang);
            """, list, t);
        t.Commit();
        _logger.LogInformation("Imported {count} gacha records for {game}.", affect, archive.Game);
        archive.Result = noName ? Lang.UIGFGachaService_ImportSuccessfulButNoRecordItemName : Lang.UIGFGachaService_ImportSuccessful;
    }



    private void ImportForZZZ(GachaUidArchiveDisplay archive)
    {
        List<ZZZGachaItem> list = new();
        DateTime TIME = new DateTime(2024, 7, 1);
        bool noName = false;
        foreach (ZZZGachaItem item in archive.napList ?? [])
        {
            var clone = item.Clone();
            if (clone.Id == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "id"));
            }
            if (clone.ItemId == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "item_id"));
            }
            if (clone.GachaType == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "gacha_type"));
            }
            if (clone.Time < TIME)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "time"));
            }
            if (clone.RankType == 0)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_0FieldIsMissingInAGachaRecord, "rank_type"));
            }
            if (string.IsNullOrWhiteSpace(clone.Name))
            {
                noName = true;
            }
            if (clone.Uid == 0)
            {
                clone.Uid = archive.Uid;
            }
            else if (clone.Uid != archive.Uid)
            {
                throw new UIGF4ImportException(archive.Game, archive.Uid, string.Format(Lang.UIGFGachaService_UidMismatchDetectedExpected0ButFound1, archive.Uid, clone.Uid));
            }
            clone.Time = item.Time.AddHours(archive.Timezone);
            list.Add(clone);
        }
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        var affect = dapper.Execute("""
            INSERT OR REPLACE INTO ZZZGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
            VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
            """, list, t);
        t.Commit();
        _logger.LogInformation("Imported {count} gacha records for {game}.", affect, archive.Game);
        archive.Result = noName ? Lang.UIGFGachaService_ImportSuccessfulButNoRecordItemName : Lang.UIGFGachaService_ImportSuccessful;
    }





    #endregion



}
