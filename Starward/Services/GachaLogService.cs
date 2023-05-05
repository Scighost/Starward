using Dapper;
using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Starward.Services;


internal class GachaLogService
{


    private readonly GachaLogClient _client;



    private static readonly Lazy<GachaLogService> _lazy = new Lazy<GachaLogService>(() => new GachaLogService());


    public static GachaLogService Instance => _lazy.Value;



    public GachaLogService()
    {
        _client = new GachaLogClient();
    }







    private List<object> GetStatsSummary(int uid)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var count = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid;", new { uid });
        var cols = new List<object> { uid, count };
        foreach (int type in new[] { 1, 2, 11, 12 })
        {
            var obj = new { uid, type };
            var c = con.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type;", obj);
            var g_5 = con.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 5);
                            """, obj);
            var g_4 = con.QueryFirstOrDefault<int>("""
                            SELECT COUNT(*) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND
                            Id > (SELECT IFNULL(MAX(Id), 0) FROM GachaLogItem WHERE Uid = @uid AND GachaType = @type AND RankType = 4);
                            """, obj);
            cols.Add($"{c} ({g_5}-{g_4})");
        }
        var time = con.QueryFirstOrDefault<DateTime>("SELECT Time FROM GachaLogUrl WHERE Uid = @uid LIMIT 1;", new { uid });
        cols.Add(time.ToString("yyyy-MM-dd HH:mm:ss"));
        return cols;
    }










    public void ExportGachaLog(int uid, bool all, string output, string format)
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
                var uids = con.Query<int>("SELECT DISTINCT Uid FROM GachaLogItem;").ToList();
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
                var list = con.Query<GachaLogItem>("SELECT * FROM GachaLogItem WHERE Uid = @uid;", new { uid }).ToList();
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
        var list = con.Query<GachaLogItem>("SELECT * FROM GachaLogItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        var obj = new GachaLogExportFile(uid, list);
        var str = JsonSerializer.Serialize(obj, AppConfig.JsonSerializerOptions);
        File.WriteAllText(output, str);
    }



    private void ExportAsExcel(int uid, string output)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<GachaLogItem>("SELECT * FROM GachaLogItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();

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
            table.Columns.Add("时间", typeof(string));
            table.Columns.Add("名称", typeof(string));
            table.Columns.Add("物品类型", typeof(string));
            table.Columns.Add("稀有度", typeof(string));
            table.Columns.Add("跃迁类型", typeof(string));
            table.Columns.Add("保底内排序", typeof(string));
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

        MiniExcel.SaveAs(output, sheets, configuration: new OpenXmlConfiguration { TableStyles = TableStyles.None, });
    }






    private class GachaLogItemEx : GachaLogItem
    {
        public int Index { get; set; }
    }



    public IEnumerable<string> GetUidCompletions(string uid)
    {
        try
        {
            using var con = DatabaseService.Instance.CreateConnection();
            return con.Query<string>("SELECT Uid FROM GachaLogUrl WHERE Uid LIKE @key;", new { key = uid + "%" });
        }
        catch
        {
            return new List<string>();
        }
    }

}
