using Dapper;
using Microsoft.Extensions.Logging;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.StarRail;
using Starward.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Service.Gacha;


public abstract class GachaLogService
{


    private readonly ILogger<GachaLogService> _logger;

    private readonly GachaLogClient _client;


    protected GachaLogService(ILogger<GachaLogService> logger, GachaLogClient client)
    {
        _logger = logger;
        _client = client;
    }



    protected abstract GameBiz GameBiz { get; }

    protected abstract string GachaTableName { get; }

    protected abstract IReadOnlyCollection<int> GachaTypes { get; }


    protected abstract List<GachaLogItemEx> GetGroupGachaLogItems(IEnumerable<GachaLogItemEx> items, GachaType type);



    public static string GetGachaLogText(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hk4e_cloud => "祈愿记录",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global => "跃迁记录",
            _ => ""
        };
    }



    public virtual List<int> GetUids()
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.Query<int>($"SELECT DISTINCT Uid FROM {GachaTableName};").ToList();
    }



    public virtual List<GachaLogItemEx> GetGachaLogItemEx(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>($"SELECT * FROM {GachaTableName} WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        foreach (var type in GachaTypes)
        {
            var l = GetGroupGachaLogItems(list, (GachaType)type);
            int index = 0;
            int pity = 0;
            foreach (var item in l)
            {
                index++;
                item.Pity = ++pity;
                if (item.RankType == 5)
                {
                    pity = 0;
                }
            }
        }
        return list;
    }



    public virtual string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path)
    {
        return GachaLogClient.GetGachaUrlFromWebCache(gameBiz, path);
    }




    public virtual async Task<int> GetUidFromGachaLogUrl(string url)
    {
        var uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid > 0)
        {
            using var dapper = DatabaseService.Instance.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO GachaLogUrl (GameBiz, Uid, Url, Time) VALUES (@GameBiz, @Uid, @Url, @Time);", new GachaLogUrl(GameBiz, uid, url));
        }
        return uid;
    }



    public virtual string? GetGachaLogUrlByUid(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.QueryFirstOrDefault<string>("SELECT Url FROM GachaLogUrl WHERE Uid = @uid AND GameBiz = @GameBiz LIMIT 1;", new { uid, GameBiz });
    }



    protected abstract int InsertGachaLogItems(List<GachaLogItem> items);



    public virtual async Task<int> GetGachaLogAsync(string url, bool all, string? lang = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        progress?.Report("正在获取 uid");
        var uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid == 0)
        {
            progress?.Report($"该账户最近6个月没有抽卡记录");
        }
        else
        {
            long endId = 0;
            if (!all)
            {
                endId = dapper.QueryFirstOrDefault<long>($"SELECT Id FROM {GachaTableName} WHERE Uid = @Uid ORDER BY Id DESC LIMIT 1;", new { Uid = uid });
                _logger.LogInformation($"Last gacha log id of uid {uid} is {endId}");
            }
            var internalProgress = new Progress<(GachaType GachaType, int Page)>((x) => progress?.Report($"正在获取 {x.GachaType.ToDescription()} 第 {x.Page} 页"));
            var list = (await _client.GetGachaLogAsync(url, endId, lang, internalProgress)).ToList();
            var oldCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = uid });
            InsertGachaLogItems(list);
            var newCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = uid });
            progress?.Report($"获取 {list.Count} 条记录，新增 {newCount - oldCount} 条记录");
        }
        return uid;
    }






    public virtual List<GachaTypeStats> GetGachaTypeStats(int uid)
    {
        var statsList = new List<GachaTypeStats>();
        using var dapper = DatabaseService.Instance.CreateConnection();
        var alllist = GetGachaLogItemEx(uid);
        if (alllist.Count > 0)
        {
            foreach (int type in GachaTypes)
            {
                var list = GetGroupGachaLogItems(alllist, (GachaType)type);
                var stats = new GachaTypeStats
                {
                    GachaType = (GachaType)type,
                    Count = list.Count,
                    Count_5 = list.Count(x => x.RankType == 5),
                    Count_4 = list.Count(x => x.RankType == 4),
                    Count_3 = list.Count(x => x.RankType == 3),
                };
                if (stats.Count > 0)
                {
                    stats.StartTime = list.First().Time;
                    stats.EndTime = list.Last().Time;
                    stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
                    stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
                    stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
                    stats.List_5 = list.Where(x => x.RankType == 5).Reverse().ToList();
                    stats.List_4 = list.Where(x => x.RankType == 4).Reverse().ToList();
                    stats.Average_5 = (double)(stats.Count - stats.Pity_5) / stats.Count_5;
                    stats.Pity_5 = list.Last().Pity;
                    stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 4);
                }
                statsList.Add(stats);
            }
        }
        return statsList;
    }






    public virtual int DeleteUid(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.Execute($"DELETE FROM {GachaTableName} WHERE Uid = @uid;", new { uid });
    }






    public void ExportGachaLog(int uid, string file, string format)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<StarRailGachaItem>($"SELECT * FROM WarpRecordItem WHERE Uid = @uid;", new { uid }).ToList();
        if (list.Count == 0)
        {

        }
        else
        {
            if (format is "excel")
            {
                ExportAsExcel(uid, file);
            }
            else
            {
                ExportAsJson(uid, file);
            }
        }
    }






    private void ExportAsJson(int uid, string output)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<StarRailGachaItem>("SELECT * FROM WarpRecordItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new GachaLogExportFile<StarRailGachaItem>(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        File.WriteAllText(output, str);
    }



    private void ExportAsExcel(int uid, string output)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<StarRailGachaItem>("SELECT * FROM WarpRecordItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();

        var sheets = new DataSet();
        var table1 = new DataTable("Raw Data");
        table1.Columns.Add("uid", typeof(string));
        table1.Columns.Add("id", typeof(string));
        table1.Columns.Add("time", typeof(string));
        table1.Columns.Add("name", typeof(string));
        table1.Columns.Add("item_type", typeof(string));
        table1.Columns.Add("rank_type", typeof(string));
        table1.Columns.Add("gacha_type", typeof(string));
        table1.Columns.Add("gacha_id", typeof(string));
        table1.Columns.Add("item_id", typeof(string));
        table1.Columns.Add("lang", typeof(string));
        table1.Columns.Add("count", typeof(string));
        foreach (var item in list)
        {
            table1.Rows.Add(item.Uid.ToString(),
                            item.Id.ToString(),
                            item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                            item.Name,
                            item.ItemType,
                            item.RankType.ToString(),
                            ((int)item.GachaType).ToString(),
                            item.GachaId.ToString(),
                            item.ItemId.ToString(),
                            item.Lang,
                            item.Count.ToString());
        }
        sheets.Tables.Add(table1);

        foreach (var type in new int[] { 1, 2, 11, 12 })
        {
            var table = new DataTable(((GachaType)type).ToDescription());
            table.Columns.Add("Uid", typeof(string));
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("Time", typeof(string));
            table.Columns.Add("Name", typeof(string));
            table.Columns.Add("Item Type", typeof(string));
            table.Columns.Add("Rarity", typeof(string));
            table.Columns.Add("Warp Type", typeof(string));
            table.Columns.Add("Pity", typeof(string));
            var l = list.Where(x => x.GachaType == (GachaType)type).ToList();
            int index = 0;
            foreach (var item in l)
            {
                index++;
                table.Rows.Add(item.Uid.ToString(),
                            item.Id.ToString(),
                            item.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                            item.Name,
                            item.ItemType,
                            item.RankType.ToString(),
                            ((int)item.GachaType).ToString(),
                            index.ToString());
                if (item.RankType == 5)
                {
                    index = 0;
                }
            }
            sheets.Tables.Add(table);
        }

        MiniExcel.SaveAs(output, sheets, configuration: new OpenXmlConfiguration { TableStyles = TableStyles.None, }, overwriteFile: true);
    }








}
