using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.Gacha;
using Starward.Core.Gacha.Genshin;
using Starward.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Service.Gacha;

internal class GenshinGachaService : GachaLogService
{


    private readonly ILogger<GenshinGachaService> _logger;


    private readonly GenshinGachaClient _client;

    public GenshinGachaService(ILogger<GenshinGachaService> logger, GenshinGachaClient client)
    {
        _logger = logger;
        _client = client;
    }




    public override List<int> GetUids()
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.Query<int>($"SELECT DISTINCT Uid FROM GenshinGachaItem;").ToList();
    }



    public override List<GachaLogItemEx> GetGachaLogItemEx(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        var list = dapper.Query<GachaLogItemEx>("SELECT * FROM GenshinGachaItem WHERE Uid = @uid ORDER BY Id;", new { uid }).ToList();
        foreach (var type in new int[] { 100, 200, 301, 302 })
        {

            var l = type switch
            {
                301 => list.Where(x => x.GachaType == (GachaType)301 || x.GachaType == (GachaType)400).ToList(),
                _ => list.Where(x => x.GachaType == (GachaType)type).ToList(),
            };
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



    public override string? GetGachaLogUrlFromWebCache(GameBiz gameBiz, string path)
    {
        return GachaLogClient.GetGachaUrlFromWebCache(gameBiz, path);
    }



    public override async Task<int> GetUidFromGachaLogUrl(string url)
    {
        var uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid > 0)
        {
            using var dapper = DatabaseService.Instance.CreateConnection();
            dapper.Execute("INSERT OR REPLACE INTO GachaLogUrl (Uid, GameBiz, Url, Time) VALUES (@Uid, @GameBiz, @Url, @Time);", new GachaLogUrl(GameBiz.Genshin, uid, url));
        }
        return uid;
    }



    public override string? GetUrlByUid(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.QueryFirstOrDefault<string>("SELECT Url FROM GachaLogUrl WHERE Uid = @uid AND GameBiz = @biz LIMIT 1;", new { uid, biz = GameBiz.Genshin });
    }



    public override async Task<int> GetWarpRecordAsync(string url, bool all, string? lang = null, IProgress<string>? progress = null, CancellationToken cancellationToken = default)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        progress?.Report("Getting uid of URL...");
        var uid = await _client.GetUidByGachaUrlAsync(url);
        if (uid == 0)
        {

        }
        else
        {
            long endId = 0;
            if (!all)
            {
                endId = dapper.QueryFirstOrDefault<long>("SELECT Id FROM GenshinGachaItem WHERE Uid = @Uid ORDER BY Id DESC LIMIT 1;", new { Uid = uid });
                _logger.LogInformation($"Last wish record id of uid {uid} is {endId}");
            }
            var internalProgress = new Progress<(GachaType GachaType, int Page)>((x) => progress?.Report($"Getting {x.GachaType.ToDescription()} page {x.Page}"));
            var list = (await _client.GetGachaLogAsync(url, endId, lang, internalProgress)).ToList();
            var oldCount = dapper.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GenshinGachaItem WHERE Uid = @Uid;", new { Uid = uid });
            using var t = dapper.BeginTransaction();
            dapper.Execute("""
                    INSERT OR REPLACE INTO GenshinGachaItem (Uid, Id, Name, Time, ItemId, ItemType, RankType, GachaType, Count, Lang)
                    VALUES (@Uid, @Id, @Name, @Time, @ItemId, @ItemType, @RankType, @GachaType, @Count, @Lang);
                    """, list, t);
            t.Commit();
            var newCount = dapper.QueryFirstOrDefault<int>("SELECT COUNT(*) FROM GenshinGachaItem WHERE Uid = @Uid;", new { Uid = uid });
            progress?.Report($"Get {list.Count} records, add {newCount - oldCount} new records.");
        }
        return uid;
    }



    public override List<GachaTypeStats> GetGachaTypeStats(int uid)
    {
        var statsList = new List<GachaTypeStats>();
        using var dapper = DatabaseService.Instance.CreateConnection();
        var alllist = GetGachaLogItemEx(uid);
        if (alllist.Count > 0)
        {
            foreach (int type in new[] { 200, 301, 302, 100 })
            {
                var list = type switch
                {
                    301 => alllist.Where(x => x.GachaType == (GachaType)301 || x.GachaType == (GachaType)400).ToList(),
                    _ => alllist.Where(x => x.GachaType == (GachaType)type).ToList(),
                };
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



    public override int DeleteUid(int uid)
    {
        using var dapper = DatabaseService.Instance.CreateConnection();
        return dapper.Execute("DELETE FROM GenshinGachaItem WHERE Uid = @uid;", new { uid });
    }





}
