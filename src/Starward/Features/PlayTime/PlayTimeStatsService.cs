using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Starward.Features.PlayTime;

internal sealed class PlayTimeStatsService
{

    private readonly ILogger<PlayTimeStatsService> _logger;

    private const long MAX_INTERVAL = 60_000;



    public PlayTimeStatsService(ILogger<PlayTimeStatsService> logger)
    {
        _logger = logger;
    }



    public int ConvertItemToStats()
    {
        try
        {
            long ago = DateTimeOffset.Now.AddDays(-1).ToUnixTimeMilliseconds();
            using var dapper = DatabaseService.CreateConnection();
            List<PlayTimeItemStruct> items = dapper.Query<PlayTimeItemStruct>("SELECT * FROM PlayTimeItem ORDER BY TimeStamp;").ToList();
            List<PlayTimeStats> stats = GetPlayTimeStats(items);
            List<PlayTimeStats> filteredStats = stats.Where(x => x.EndTime < ago).ToList();
            if (filteredStats.Count > 0)
            {
                using var t = dapper.BeginTransaction();
                dapper.Execute("INSERT OR IGNORE INTO PlayTimeStats (GameBiz, Pid, StartTime, EndTime, Interruption, Type) VALUES (@GameBiz, @Pid, @StartTime, @EndTime, @Interruption, @Type);", filteredStats, t);
                dapper.Execute("DELETE FROM PlayTimeItem WHERE GameBiz = @GameBiz AND Pid = @Pid AND TimeStamp >= @StartTime AND TimeStamp <= @EndTime;", filteredStats, t);
                t.Commit();
            }
            return stats.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Convert playtime items to stats");
            return 0;
        }
    }



    public List<PlayTimeStats> GetPlayTimeStats(List<PlayTimeItemStruct> items)
    {
        List<PlayTimeStats> list = new();
        Dictionary<(GameBiz, int), PlayTimeStats> statsSession = new();
        foreach (var item in items)
        {
            var key = (item.GameBiz, item.Pid);
            if (statsSession.TryGetValue((item.GameBiz, item.Pid), out PlayTimeStats? stats))
            {
                if (item.State is PlayTimeState.Start)
                {
                    stats.Interruption = true;
                    list.Add(stats);
                    statsSession[key] = new PlayTimeStats
                    {
                        GameBiz = item.GameBiz,
                        Pid = item.Pid,
                        StartTime = item.TimeStamp,
                        EndTime = item.TimeStamp,
                    };
                    continue;
                }
                if (item.TimeStamp - stats.EndTime > MAX_INTERVAL)
                {
                    stats.Interruption = true;
                    list.Add(stats);
                    statsSession.Remove((item.GameBiz, item.Pid));
                    if (item.State is PlayTimeState.Play)
                    {
                        statsSession[key] = new PlayTimeStats
                        {
                            GameBiz = item.GameBiz,
                            Pid = item.Pid,
                            StartTime = item.TimeStamp,
                            EndTime = item.TimeStamp,
                        };
                    }
                }
                else if (item.State is PlayTimeState.Play)
                {
                    stats.EndTime = item.TimeStamp;
                    continue;
                }
                else if (item.State is PlayTimeState.Stop or PlayTimeState.Error)
                {
                    stats.Interruption = item.State is PlayTimeState.Error;
                    stats.EndTime = item.TimeStamp;
                    list.Add(stats);
                    statsSession.Remove((item.GameBiz, item.Pid));
                    continue;
                }
            }
            else if (item.State is PlayTimeState.Start or PlayTimeState.Play)
            {
                statsSession[key] = new PlayTimeStats
                {
                    GameBiz = item.GameBiz,
                    Pid = item.Pid,
                    StartTime = item.TimeStamp,
                    EndTime = item.TimeStamp,
                };
            }
        }
        foreach (var item in statsSession)
        {
            item.Value.Interruption = true;
            list.Add(item.Value);
        }
        statsSession.Clear();
        return list;
    }



    /// <summary>
    /// 获取总游戏时间（全历史范围）
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public TimeSpan GetPlayTimeTotal(GameBiz biz)
    {
        biz = biz.IsBilibili() ? $"{biz.Game}_cn" : biz;
        long total = 0;
        foreach (var session in GetPlayTimeInRange(biz, DateTimeOffset.FromUnixTimeMilliseconds(0), DateTimeOffset.Now))
        {
            total += session.EndTime - session.StartTime;
        }
        return TimeSpan.FromMilliseconds(total);
    }



    /// <summary>
    /// 统计 [start, end] 时间范围内的游戏时间，同时查询 PlayTimeStats 与 PlayTimeItem，
    /// 返回裁剪到窗口内的会话区间列表（按 StartTime 升序）。
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public List<PlayTimeStats> GetPlayTimeInRange(GameBiz biz, DateTimeOffset start, DateTimeOffset end)
    {
        biz = biz.IsBilibili() ? $"{biz.Game}_cn" : biz;
        List<PlayTimeStats> result = new();
        long ts_start = start.ToUnixTimeMilliseconds();
        long ts_end = end.ToUnixTimeMilliseconds();
        if (ts_end <= ts_start)
        {
            return result;
        }

        using var dapper = DatabaseService.CreateConnection();

        var stats = dapper.Query<PlayTimeStats>(
            "SELECT * FROM PlayTimeStats WHERE GameBiz = @biz AND EndTime >= @ts_start AND StartTime < @ts_end AND StartTime >= 0 AND EndTime >= StartTime;",
            new { biz, ts_start, ts_end });
        foreach (var stat in stats)
        {
            stat.StartTime = Math.Max(stat.StartTime, ts_start);
            stat.EndTime = Math.Min(stat.EndTime, ts_end);
            result.Add(stat);
        }

        var items = dapper.Query<PlayTimeItemStruct>(
            "SELECT * FROM PlayTimeItem WHERE GameBiz = @biz AND TimeStamp >= @ts_start AND TimeStamp < @ts_end ORDER BY TimeStamp;",
            new { biz = biz.ToString(), ts_start, ts_end }).ToList();
        stats = GetPlayTimeStats(items);
        foreach (var stat in stats)
        {
            stat.StartTime = Math.Max(stat.StartTime, ts_start);
            stat.EndTime = Math.Min(stat.EndTime, ts_end);
            result.Add(stat);
        }

        return result.OrderBy(x => x.StartTime).ToList();
    }


}
