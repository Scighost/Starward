using Dapper;
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


    //private readonly ILogger<GachaLogService> _logger;

    //private readonly GenshinGachaClient _genshinClient;

    //private readonly StarRailGachaClient _starRailClient;

    //protected abstract IReadOnlyCollection<GachaType> GachaTypes { get; }

    //public GachaLogService(ILogger<GachaLogService> logger, GenshinGachaClient genshinClient, StarRailGachaClient starRailClient)
    //{
    //    _logger = logger;
    //    _genshinClient = genshinClient;
    //    _starRailClient = starRailClient;
    //}


    public static string GetGachaLogText(GameBiz biz)
    {
        return biz switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_global or GameBiz.hk4e_cloud => "祈愿记录",
            GameBiz.hkrpg_cn or GameBiz.hkrpg_global => "跃迁记录",
            _ => ""
        };
    }



    public abstract List<int> GetUids();



    public abstract List<GachaLogItemEx> GetGachaLogItemEx(int uid);



    public abstract string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path);




    public abstract Task<int> GetUidFromGachaLogUrl(string url);



    public abstract string? GetUrlByUid(int uid);



    public abstract Task<int> GetWarpRecordAsync(string url, bool all, string? lang = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default);






    public abstract List<GachaTypeStats> GetGachaTypeStats(int uid);





    public abstract int DeleteUid(int uid);









    public void ExportWarpRecord(int uid, string file, string format)
    {
        using var con = DatabaseService.Instance.CreateConnection();
        var list = con.Query<StarRailGachaItem>("SELECT * FROM WarpRecordItem WHERE Uid = @uid;", new { uid }).ToList();
        if (list.Count == 0)
        {
            //Logger.Warn($"Uid {uid} 没有任何抽卡数据", true);
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
