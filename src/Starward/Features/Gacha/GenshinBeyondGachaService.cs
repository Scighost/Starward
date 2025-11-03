using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Localization;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI;

namespace Starward.Features.Gacha;

internal class GenshinBeyondGachaService
{


    private readonly ILogger<GenshinBeyondGachaService> _logger;

    private readonly GenshinBeyondGachaClient _client;


    private const string GachaTableName = "GenshinBeyondGachaItem";


    public GenshinBeyondGachaService(ILogger<GenshinBeyondGachaService> logger, GenshinBeyondGachaClient client)
    {
        _logger = logger;
        _client = client;
    }



    public virtual List<long> GetUids()
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<long>($"SELECT DISTINCT Uid FROM {GachaTableName};").ToList();
    }



    public string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path)
    {
        return GenshinBeyondGachaClient.GetGachaUrlFromWebCache(gameBiz, path);
    }



    public virtual async Task<long> GetUidFromGachaLogUrl(string url)
    {
        long uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid > 0)
        {
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO GachaLogUrl (GameBiz, Uid, Url, Time) VALUES (@GameBiz, @Uid, @Url, @Time);", new GachaLogUrl("hk4eugc", uid, url));
        }
        return uid;
    }



    public virtual string? GetGachaLogUrlByUid(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<string>("SELECT Url FROM GachaLogUrl WHERE Uid = @uid AND GameBiz = @GameBiz LIMIT 1;", new { uid, GameBiz = "hk4eugc" });
    }



    private int InsertGachaLogItems(List<GenshinBeyondGachaItem> items)
    {
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        int count = dapper.Execute("""
            INSERT OR REPLACE INTO GenshinBeyondGachaItem(Uid, Id, Region, OpGachaType, ScheduleId, ItemType, ItemId, ItemName, RankType, IsUp, Time)
            VALUES (@Uid, @Id, @Region, @OpGachaType, @ScheduleId, @ItemType, @ItemId, @ItemName, @RankType, @IsUp, @Time);
            """, items, t);
        t.Commit();
        return count;
    }



    public virtual async Task<long> GetGachaLogAsync(string url, bool all, string? lang = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        using var dapper = DatabaseService.CreateConnection();
        // 正在获取 uid
        progress?.Report(Lang.GachaLogService_GettingUid);
        var uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid == 0)
        {
            // 该账号最近6个月没有抽卡记录
            progress?.Report(Lang.GachaLogService_ThisAccountHasNoGachaRecordsInTheLast6Months);
        }
        else
        {
            long endId = 0;
            if (!all)
            {
                endId = dapper.QueryFirstOrDefault<long>($"SELECT Id FROM {GachaTableName} WHERE Uid = @Uid ORDER BY Id DESC LIMIT 1;", new { Uid = uid });
                _logger.LogInformation($"Last gacha log id of uid {uid} is {endId}");
            }

            var internalProgress = new Progress<(int GachaType, int Page)>((x) => progress?.Report(string.Format(Lang.GachaLogService_GetGachaProgressText, x.GachaType == 1000 ? CoreLang.GachaType_StandardOde : CoreLang.GachaType_EventOde, x.Page)));
            var list = (await _client.GetGachaLogAsync(url, endId, lang, internalProgress, cancellationToken)).ToList();
            if (cancellationToken.IsCancellationRequested)
            {
                throw new TaskCanceledException();
            }
            var oldCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = uid });
            InsertGachaLogItems(list);
            var newCount = dapper.QueryFirstOrDefault<int>($"SELECT COUNT(*) FROM {GachaTableName} WHERE Uid = @Uid;", new { Uid = uid });
            // 获取 {list.Count} 条记录，新增 {newCount - oldCount} 条记录
            progress?.Report(string.Format(Lang.GachaLogService_GetGachaResult, list.Count, newCount - oldCount));
        }
        return uid;
    }



    public GenshinBeyondGachaTypeStats? GetGachaTypeStatsType1000(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GenshinBeyondGachaItemEx>("""
            SELECT item.*, info.Icon FROM GenshinBeyondGachaItem item LEFT JOIN GenshinBeyondGachaInfo info
            ON item.ItemId = info.Id WHERE Uid = @uid AND OpGachaType = 1000 ORDER BY item.Id;
            """, new { uid }).ToList();
        if (list.Count == 0)
        {
            return null;
        }

        int index = 0;
        int pity = 0;
        foreach (var item in list)
        {
            item.Index = ++index;
            item.Pity = ++pity;
            if (item.RankType == 4)
            {
                pity = 0;
            }
        }

        var stats = new GenshinBeyondGachaTypeStats
        {
            GachaType = 1000,
            GachaTypeText = CoreLang.GachaType_StandardOde,
            Count = list.Count,
            Count_5 = list.Count(x => x.RankType == 5),
            Count_4 = list.Count(x => x.RankType == 4),
            Count_3 = list.Count(x => x.RankType == 3),
            Count_2 = list.Count(x => x.RankType == 2),
            StartTime = list.First().Time,
            EndTime = list.Last().Time,
        };
        stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
        stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
        stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
        stats.Ratio_2 = (double)stats.Count_2 / stats.Count;
        stats.List_5 = list.Where(x => x.RankType == 5).Reverse().ToList();
        stats.List_4 = list.Where(x => x.RankType == 4).Reverse().ToList();
        stats.List_3 = list.Where(x => x.RankType == 3).Reverse().ToList();

        stats.Pity_4 = list.Last().Pity;
        if (list.Last().RankType == 4)
        {
            stats.Pity_5 = 0;
        }
        stats.Average_4 = (double)(stats.Count - stats.Pity_4) / stats.Count_4;
        stats.Pity_3 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 3);
        int pity_3 = 0;
        foreach (var item in list)
        {
            pity_3++;
            if (item.RankType == 3)
            {
                item.Pity = pity_3;
                pity_3 = 0;
            }
        }

        stats.List_4.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 1000,
            ItemName = Lang.GachaStatsCard_Pity,
            Pity = stats.Pity_4,
            Time = list.Last().Time,
        });
        stats.List_3.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 1000,
            ItemName = Lang.GachaStatsCard_Pity,
            Pity = stats.Pity_3,
            Time = list.Last().Time,
        });

        return stats;
    }


    public GenshinBeyondGachaTypeStats? GetGachaTypeStatsType2000(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GenshinBeyondGachaItemEx>("""
            SELECT item.*, info.Icon FROM GenshinBeyondGachaItem item LEFT JOIN GenshinBeyondGachaInfo info
            ON item.ItemId = info.Id WHERE Uid = @uid AND OpGachaType != 1000 ORDER BY item.Id;
            """, new { uid }).ToList();
        if (list.Count == 0)
        {
            return null;
        }

        int index = 0;
        int pity = 0;
        foreach (var item in list)
        {
            item.Index = ++index;
            item.Pity = ++pity;
            if (item.RankType == 5)
            {
                pity = 0;
            }
        }

        var stats = new GenshinBeyondGachaTypeStats
        {
            GachaType = 2000,
            GachaTypeText = CoreLang.GachaType_EventOde,
            Count = list.Count,
            Count_5 = list.Count(x => x.RankType == 5),
            Count_4 = list.Count(x => x.RankType == 4),
            Count_3 = list.Count(x => x.RankType == 3),
            Count_2 = list.Count(x => x.RankType == 2),
            StartTime = list.First().Time,
            EndTime = list.Last().Time,
        };
        stats.Ratio_5 = (double)stats.Count_5 / stats.Count;
        stats.Ratio_4 = (double)stats.Count_4 / stats.Count;
        stats.Ratio_3 = (double)stats.Count_3 / stats.Count;
        stats.Ratio_2 = (double)stats.Count_2 / stats.Count;
        stats.List_5 = list.Where(x => x.RankType == 5).Reverse().ToList();
        stats.List_4 = list.Where(x => x.RankType == 4).Reverse().ToList();
        stats.List_3 = list.Where(x => x.RankType == 3).Reverse().ToList();

        stats.Pity_5 = list.Last().Pity;
        if (list.Last().RankType == 5)
        {
            stats.Pity_5 = 0;
        }
        stats.Average_5 = (double)(stats.Count - stats.Pity_5) / stats.Count_5;
        stats.Pity_4 = list.Count - 1 - list.FindLastIndex(x => x.RankType == 4);
        int pity_4 = 0;
        foreach (var item in list)
        {
            pity_4++;
            if (item.RankType == 4)
            {
                item.Pity = pity_4;
                pity_4 = 0;
            }
        }

        stats.List_5.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 2000,
            ItemName = Lang.GachaStatsCard_Pity,
            Pity = stats.Pity_5,
            Time = list.Last().Time,
        });
        stats.List_4.Insert(0, new GenshinBeyondGachaItemEx
        {
            OpGachaType = 2000,
            ItemName = Lang.GachaStatsCard_Pity,
            Pity = stats.Pity_4,
            Time = list.Last().Time,
        });

        return stats;
    }


    public List<GenshinBeyondGachaItemEx>? GetGachaItemStats(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GenshinBeyondGachaItemEx>("""
            SELECT item.*, info.Icon FROM GenshinBeyondGachaItem item LEFT JOIN GenshinBeyondGachaInfo info
            ON item.ItemId = info.Id WHERE Uid = @uid ORDER BY item.Id;
            """, new { uid }).ToList();
        if (list.Count == 0)
        {
            return null;
        }
        return list.GroupBy(x => x.ItemId)
                   .Select(x => { var item = x.First(); item.Count = x.Count(); return item; })
                   .OrderByDescending(x => x.RankType)
                   .ThenByDescending(x => x.Count)
                   .ThenByDescending(x => x.Time)
                   .ToList();
    }


    public virtual int DeleteUid(long uid)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Execute($"DELETE FROM {GachaTableName} WHERE Uid = @uid;", new { uid });
    }



    public virtual int DeleteGachaLogByTime(long uid, DateTime begin, DateTime end)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Execute($"DELETE FROM {GachaTableName} WHERE Uid = @uid AND Time >= @begin AND Time <= @end;", new { uid, begin, end });
    }



    public async Task UpdateGachaInfoAsync(CancellationToken cancellationToken = default)
    {
        var data = await _client.GetGenshinBeyondGachaInfoAsync(cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        const string insertSql = """INSERT OR REPLACE INTO GenshinBeyondGachaInfo (Id, Name, Rank, Icon) VALUES (@Id, @Name, @Rank, @Icon);""";
        dapper.Execute(insertSql, data, t);
        t.Commit();
    }


}


public partial class GenshinBeyondGachaItemEx : GenshinBeyondGachaItem
{
    /// <summary>
    /// 相同保底卡池中的顺序
    /// </summary>
    public int Index { get; set; }

    public int Pity { get; set; }

    public string Icon { get; set; }

    public int Count { get; set; }

}



public class GenshinBeyondGachaTypeStats
{

    public int GachaType { get; set; }

    public string GachaTypeText { get; set; }

    public int Count { get; set; }

    public int Pity_5 { get; set; }

    public int Pity_4 { get; set; }

    public int Pity_3 { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Count_5 { get; set; }

    public int Count_4 { get; set; }

    public int Count_3 { get; set; }

    public int Count_2 { get; set; }

    public double Ratio_5 { get; set; }

    public double Ratio_4 { get; set; }

    public double Ratio_3 { get; set; }

    public double Ratio_2 { get; set; }

    public double Average_5 { get; set; }

    public double Average_4 { get; set; }

    public List<GenshinBeyondGachaItemEx> List_5 { get; set; }

    public List<GenshinBeyondGachaItemEx> List_4 { get; set; }

    public List<GenshinBeyondGachaItemEx> List_3 { get; set; }

}


public class GenshinBeyondGachaPityProgressBackgroundBrushConverter : IValueConverter
{
    private static Color Red = Color.FromArgb(0xFF, 0xC8, 0x3C, 0x23);
    private static Color Green = Color.FromArgb(0xFF, 0x00, 0xE0, 0x79);

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is GenshinBeyondGachaItemEx item)
        {
            int pity = item.Pity;
            var brush = new LinearGradientBrush { StartPoint = new Point(0, 0), EndPoint = new Point(1, 0), Opacity = 0.4 };
            int point = 64;
            double guarantee = 70;
            double offset = pity / guarantee;
            if (pity < point)
            {
                brush.GradientStops.Add(new GradientStop { Color = Green, Offset = 0 });
                brush.GradientStops.Add(new GradientStop { Color = Green, Offset = offset });
                brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = offset });
            }
            else
            {
                brush.GradientStops.Add(new GradientStop { Color = Red, Offset = 0 });
                brush.GradientStops.Add(new GradientStop { Color = Red, Offset = offset });
                brush.GradientStops.Add(new GradientStop { Color = Colors.Transparent, Offset = offset });
            }
            return brush;
        }
        return null!;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}