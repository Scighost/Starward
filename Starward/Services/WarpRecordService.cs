using Dapper;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using Starward.Core;
using Starward.Core.Warp;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.ApplicationModel.Activation;

namespace Starward.Services;


public class WarpRecordService
{


    private readonly WarpRecordClient _client;



    private static readonly Lazy<WarpRecordService> _lazy = new Lazy<WarpRecordService>(() => new WarpRecordService());


    public static WarpRecordService Instance => _lazy.Value;



    public WarpRecordService()
    {
        _client = new WarpRecordClient();
    }




    public List<int> GetUids()
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.Query<int>("SELECT DISTINCT Uid FROM WarpRecordItem;").ToList();
    }



    public List<WarpRecordItemEx> GetWarpRecordItemEx(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        var list = dapper.Query<WarpRecordItemEx>("SELECT * FROM WarpRecordItem WHERE Uid = @uid;", new { uid }).ToList();
        foreach (var type in new int[] { 1, 2, 11, 12 })
        {
            var l = list.Where(x => x.WarpType == (WarpType)type).ToList();
            int index = 0;
            foreach (var item in l)
            {
                index++;
                if (item.RankType == 5)
                {
                    index = 0;
                }
            }
        }
        return list;
    }



    public string? GetWarpRecordUrlFromWebCache(string path)
    {
        return WarpRecordClient.GetWarpUrlFromWebCache(path);
    }




    public async Task<int> GetUidFromWarpRecordUrl(string url)
    {
        var uid = await _client.GetUidByWarpUrlAsync(url);
        if (uid > 0)
        {
            using var dapper = DatabaseService.Instance.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO WarpRecordUrl (Uid, WarpUrl, Time) VALUES (@Uid, @WarpUrl, @Time);", new WarpRecordUrl(uid, url));
        }
        return uid;
    }



    public async Task GetWarpRecordAsync(string url, bool all, string? lang = null, IProgress<string>? progress = null)
    {

        using var dapper = DatabaseService.Instance.CreateConnection();
        if (string.IsNullOrWhiteSpace(lang))
        {
            _client.Language = null;
        }
        else
        {
            _client.Language = lang;
        }
        var uid = await _client.GetUidByWarpUrlAsync(url);
        if (uid == 0)
        {

        }
        else
        {
            long endId = 0;
            if (!all)
            {
                endId = dapper.QueryFirstOrDefault<long>("SELECT Id FROM WarpRecordItem WHERE Uid = @Uid ORDER BY Id DESC LIMIT 1;", new { Uid = uid });
            }
            var internalProgress = new Progress<(WarpType WarpType, int Page)>((x) => progress?.Report($"Getting {x.Page} page of {x.WarpType.ToDescription()}"));
            var list = await _client.GetWarpRecordAsync(url, endId, internalProgress);
            var oldCount = dapper.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM WarpRecordItem WHERE Uid = @Uid;", new { Uid = uid });
            using var t = dapper.BeginTransaction();
            dapper.Execute("""
                    INSERT OR REPLACE INTO WarpRecordItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, WarpType, WarpId, Count, Lang)
                    VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @WarpType, @WarpId, @Count, @Lang);
                    """, list, t);
            t.Commit();
            var newCount = dapper.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM WarpRecordItem WHERE Uid = @Uid;", new { Uid = uid });
            progress?.Report($"");
            progress?.Report($"Got {list.Count} records of uid {uid}, added {newCount - oldCount} records.");
        }
    }





    private List<object> GetStatsSummary(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        var count = dapper.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM WarpRecordItem WHERE Uid = @uid;", new { uid });
        var cols = new List<object> { uid, count };
        foreach (int type in new[] { 1, 2, 11, 12 })
        {
            var obj = new { uid, type };
            var c = dapper.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM WarpRecordItem WHERE Uid = @uid AND WarpType = @type;", obj);
            var g_5 = dapper.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM WarpRecordItem WHERE Uid = @uid AND WarpType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM WarpRecordItem WHERE Uid = @uid AND WarpType = @type AND RankType = 5);
                            """, obj);
            var g_4 = dapper.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM WarpRecordItem WHERE Uid = @uid AND WarpType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM WarpRecordItem WHERE Uid = @uid AND WarpType = @type AND RankType = 4);
                            """, obj);
            cols.Add($"{c} ({g_5}-{g_4})");
        }
        var time = dapper.QueryFirstOrDefault<DateTime>("SELECT Time FROM WarpRecordUrl WHERE Uid = @uid LIMIT 1;", new { uid });
        cols.Add(time.ToString("yyyy-MM-dd HH:mm:ss"));
        return cols;
    }










    public void ExportWarpRecord(int uid, bool all, string output, string format)
    {
        try
        {
            var time = DateTime.Now;
            if (Path.GetExtension(output) == ".json")
            {
                format = "json";
            }
            if (Path.GetExtension(output) == ".xlsx")
            {
                format = "excel";
            }
            using var con = DatabaseService.Instance.CreateConnection();
            if (all)
            {
                var uids = con.Query<int>("SELECT DISTINCT Uid FROM WarpRecordItem;").ToList();
                if (uids.Count == 0)
                {
                    //Logger.Warn("没有任何抽卡数据", true);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        output = Path.Combine(AppConfig.ConfigDirectory, "Export");
                    }
                    output = Path.GetFullPath(output);
                    Directory.CreateDirectory(output);
                    foreach (var u in uids)
                    {
                        string file = "";
                        if (format is "excel")
                        {
                            file = Path.Combine(output, $"export_gacha_{u}_{time:yyyyMMddHHmmss}.xlsx");
                            ExportAsExcel(u, file);
                        }
                        else
                        {
                            file = Path.Combine(output, $"export_gacha_{u}_{time:yyyyMMddHHmmss}.json");
                            ExportAsJson(u, file);
                        }
                        //Logger.Success($"已导出 uid {u}: {file}");
                    }
                }
            }
            else
            {
                var list = con.Query<WarpRecordItem>("SELECT * FROM WarpRecordItem WHERE Uid = @uid;", new { uid }).ToList();
                if (list.Count == 0)
                {
                    //Logger.Warn($"Uid {uid} 没有任何抽卡数据", true);
                }
                else
                {
                    string dir = "./";
                    if (string.IsNullOrWhiteSpace(output))
                    {
                        output = null!;
                        dir = Path.Combine(AppConfig.ConfigDirectory, "Export");
                    }
                    else
                    {
                        output = Path.GetFullPath(output);
                        dir = Path.GetDirectoryName(output)!;
                    }
                    Directory.CreateDirectory(dir);

                    string file = "";
                    if (format is "excel")
                    {
                        file = output ?? Path.Combine(dir, $"export_gacha_{uid}_{time:yyyyMMddHHmmss}.xlsx");
                        ExportAsExcel(uid, file);
                    }
                    else
                    {
                        file = output ?? Path.Combine(dir, $"export_gacha_{uid}_{time:yyyyMMddHHmmss}.json");
                        ExportAsJson(uid, file);
                    }
                    //Logger.Success($"已导出 uid {uid}: {file}", true);
                }
            }
        }
        catch (Exception ex)
        {
            //Logger.Error(ex.Message);
        }
    }






    private void ExportAsJson(int uid, string output)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<WarpRecordItem>("SELECT * FROM WarpRecordItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new WarpRecordExportFile(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        File.WriteAllText(output, str);
    }



    private void ExportAsExcel(int uid, string output)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<WarpRecordItem>("SELECT * FROM WarpRecordItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();

        var sheets = new DataSet();
        var table1 = new DataTable("原始数据");
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
                            ((int)item.WarpType).ToString(),
                            item.WarpId.ToString(),
                            item.ItemId.ToString(),
                            item.Lang,
                            item.Count.ToString());
        }
        sheets.Tables.Add(table1);

        foreach (var type in new int[] { 1, 2, 11, 12 })
        {
            var table = new DataTable(((WarpType)type).ToDescription());
            table.Columns.Add("Uid", typeof(string));
            table.Columns.Add("Id", typeof(string));
            table.Columns.Add("时间", typeof(string));
            table.Columns.Add("名称", typeof(string));
            table.Columns.Add("物品类型", typeof(string));
            table.Columns.Add("稀有度", typeof(string));
            table.Columns.Add("跃迁类型", typeof(string));
            table.Columns.Add("保底内排序", typeof(string));
            var l = list.Where(x => x.WarpType == (WarpType)type).ToList();
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
                            ((int)item.WarpType).ToString(),
                            index.ToString());
                if (item.RankType == 5)
                {
                    index = 0;
                }
            }
            sheets.Tables.Add(table);
        }

        MiniExcel.SaveAs(output, sheets, configuration: new OpenXmlConfiguration { TableStyles = TableStyles.None, });
    }









    public IEnumerable<string> GetUidCompletions(string uid)
    {
        try
        {
            using var con = DatabaseService.Instance.CreateConnection();
            return con.Query<string>("SELECT Uid FROM WarpRecordUrl WHERE Uid LIKE @key;", new { key = uid + "%" });
        }
        catch
        {
            return new List<string>();
        }
    }

}
