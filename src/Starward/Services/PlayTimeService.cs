using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using Starward.Core;
using Starward.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.Services;

internal class PlayTimeService
{

    private readonly ILogger<PlayTimeService> _logger;

    private readonly DatabaseService _database;


    public PlayTimeService(ILogger<PlayTimeService> logger, DatabaseService database)
    {
        _logger = logger;
        _database = database;
    }



    public async Task LogPlayTimeAsync(GameBiz biz, int pid)
    {
        try
        {
            var instance = AppInstance.FindOrRegisterForKey($"playtime_{pid}");
            if (!instance.IsCurrent)
            {
                _logger.LogWarning("Game process ({biz}, {gamePid}) has been recorded by process ({playtimePid})", biz, pid, instance.ProcessId);
                return;
            }
            Log(biz, pid, PlayTimeItem.PlayState.None, 0, "Ready to log time");
            var process = Process.GetProcessById(pid);
            Log(biz, pid, PlayTimeItem.PlayState.Start, new DateTimeOffset(process.StartTime).ToUnixTimeMilliseconds(), process.ProcessName);
            var sw = Stopwatch.StartNew();
            long last = 0;
            while (true)
            {
                await Task.Delay(Random.Shared.Next(800, 1200));
                if (process.HasExited)
                {
                    Log(biz, pid, PlayTimeItem.PlayState.Stop);
                    break;
                }
                else
                {
                    if (sw.ElapsedMilliseconds - last > 30000)
                    {
                        Log(biz, pid, PlayTimeItem.PlayState.Play);
                        last = sw.ElapsedMilliseconds;
                    }
                }
            }
            _database.SetValue($"playtime_total_{biz}", GetPlayTimeTotal(biz));
            _database.SetValue($"playtime_month_{biz}", GetPlayCurrentMonth(biz));
            _database.SetValue($"playtime_week_{biz}", GetPlayCurrentWeek(biz));
            _database.SetValue($"playtime_day_{biz}", GetPlayCurrentDay(biz));
            _database.SetValue($"startup_count_{biz}", GetStartUpCount(biz));
        }
        catch (Exception ex)
        {
            Log(biz, pid, PlayTimeItem.PlayState.Error, 0, ex.Message);
            _logger.LogError(ex, "Log play time: GameBiz {biz}, Pid {pid}", biz, pid);
        }
    }




    private void Log(GameBiz biz, int pid, PlayTimeItem.PlayState state, long ts = 0, string? message = null)
    {
        try
        {
            using var dapper = _database.CreateConnection();
            User32.GetCursorPos(out var pos);
            var item = new PlayTimeItem
            {
                TimeStamp = ts == 0 ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : ts,
                GameBiz = biz,
                Pid = pid,
                State = state,
                CursorPos = (((long)pos.X) << 32) | (long)pos.Y,
                Message = message,
            };
            dapper.Execute("INSERT OR REPLACE INTO PlayTimeItem (TimeStamp, GameBiz, Pid, State, CursorPos, Message) VALUES (@TimeStamp, @GameBiz, @Pid, @State, @CursorPos, @Message);", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log play time: GameBiz {biz}, Pid {pid}, State {state}, Message {message}", biz, pid, state, message);
        }
    }



    public TimeSpan GetPlayTimeTotal(GameBiz biz)
    {
        return GetPlayTime(biz);
    }


    public TimeSpan GetPlayCurrentMonth(GameBiz biz)
    {
        var now = DateTimeOffset.Now;
        var month = now.Add(-now.TimeOfDay).AddDays(1 - now.Day);
        return GetPlayTime(biz, month, now);
    }


    public TimeSpan GetPlayCurrentWeek(GameBiz biz)
    {
        var now = DateTimeOffset.Now;
        var week = now.Add(-now.TimeOfDay).AddDays(-(((int)now.DayOfWeek + 6) % 7));
        return GetPlayTime(biz, week, now);
    }


    public TimeSpan GetPlayCurrentDay(GameBiz biz)
    {
        var now = DateTimeOffset.Now;
        var day = now.Add(-now.TimeOfDay);
        return GetPlayTime(biz, day, now);
    }


    public int GetStartUpCount(GameBiz biz)
    {
        using var dapper = _database.CreateConnection();
        return dapper.ExecuteScalar<int>("SELECT COUNT(*) FROM PlayTimeItem WHERE GameBiz = @biz AND State = @state;", new { biz, state = PlayTimeItem.PlayState.Start });
    }


    public (DateTimeOffset Time, TimeSpan Span) GetLastPlayTime(GameBiz biz)
    {
        using var dapper = _database.CreateConnection();
        var start_item = dapper.QueryFirstOrDefault<PlayTimeItem>("SELECT * FROM PlayTimeItem WHERE GameBiz = @biz AND State = 1 ORDER BY TimeStamp DESC LIMIT 1;", new { biz });
        if (start_item != null)
        {
            var last_item = dapper.QueryFirstOrDefault<PlayTimeItem>("SELECT * FROM PlayTimeItem WHERE GameBiz = @biz AND Pid = @Pid ORDER BY TimeStamp DESC LIMIT 1;", new { biz, start_item.Pid });
            if (last_item != null)
            {
                return (DateTimeOffset.FromUnixTimeMilliseconds(start_item.TimeStamp), TimeSpan.FromMilliseconds(last_item.TimeStamp - start_item.TimeStamp));
            }
        }
        return (DateTimeOffset.MinValue, TimeSpan.Zero);
    }



    public TimeSpan GetPlayTime(GameBiz biz, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        long ts_start = start?.ToUnixTimeMilliseconds() ?? 0;
        long ts_end = end?.ToUnixTimeMilliseconds() ?? long.MaxValue;
        using var dapper = _database.CreateConnection();
        var items = dapper.Query<PlayTimeItemStruct>("SELECT * FROM PlayTimeItem WHERE GameBiz = @biz AND TimeStamp >= @ts_start AND TimeStamp <= @ts_end ORDER BY TimeStamp;", new { biz, ts_start, ts_end }).ToList();
        return ComputePlayTime(items, start, end);
    }



    private static TimeSpan ComputePlayTime(List<PlayTimeItemStruct> items, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        if (items.Count == 0)
        {
            return TimeSpan.Zero;
        }

        const long MAX_INTERVAL = 60_000;

        long ts_total = 0;
        long ts_start = start?.ToUnixTimeMilliseconds() ?? items[0].TimeStamp;
        long ts_end = end?.ToUnixTimeMilliseconds() ?? items[^1].TimeStamp;

        if (items.Count == 1)
        {
            if (items[0].State is PlayTimeItem.PlayState.Start)
            {
                ts_total += Math.Clamp(items[0].TimeStamp - ts_start, 0, MAX_INTERVAL);
            }
            else if (items[0].State is PlayTimeItem.PlayState.Stop)
            {
                ts_total += Math.Clamp(ts_end - items[0].TimeStamp, 0, MAX_INTERVAL);
            }
            else if (items[0].State is PlayTimeItem.PlayState.Play)
            {
                ts_total += Math.Clamp(ts_end - ts_start, 0, MAX_INTERVAL);
            }
        }
        else
        {
            var dic_start_time = new Dictionary<int, long>();
            var dic_last_time = new Dictionary<int, long>();

            if (items[0].State is PlayTimeItem.PlayState.Play or PlayTimeItem.PlayState.Stop && items[0].TimeStamp - ts_start <= MAX_INTERVAL)
            {
                dic_start_time[items[0].Pid] = ts_start;
                dic_last_time[items[0].Pid] = ts_start;
            }

            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                int pid = item.Pid;
                long ts_last_time = dic_last_time.GetValueOrDefault(pid);
                if (item.TimeStamp - ts_last_time > MAX_INTERVAL)
                {
                    // 距离上一个时间记录点超过 MAX_INTERVAL，认为是新的一次游戏
                    long ts_start_time = dic_start_time.GetValueOrDefault(pid);
                    if (ts_last_time != 0 && ts_start_time != 0)
                    {
                        ts_total += Math.Clamp(ts_last_time - ts_start_time, 0, long.MaxValue);
                    }
                    if (item.State is not PlayTimeItem.PlayState.Stop or PlayTimeItem.PlayState.Error)
                    {
                        dic_last_time[pid] = item.TimeStamp;
                        dic_start_time[pid] = item.TimeStamp;

                    }
                    else
                    {
                        dic_start_time[pid] = 0;
                        dic_last_time[pid] = 0;
                    }
                }
                else
                {
                    if (item.State is PlayTimeItem.PlayState.Start)
                    {
                        long ts_start_time = dic_start_time.GetValueOrDefault(pid);
                        if (ts_start_time != 0)
                        {
                            ts_total += Math.Clamp(ts_last_time - ts_start_time, 0, long.MaxValue);
                        }
                        dic_start_time[pid] = item.TimeStamp;
                        dic_last_time[pid] = item.TimeStamp;
                    }
                    else if (item.State is PlayTimeItem.PlayState.Stop or PlayTimeItem.PlayState.Error)
                    {
                        long ts_start_time = dic_start_time.GetValueOrDefault(pid);
                        if (ts_start_time != 0)
                        {
                            ts_total += item.TimeStamp - ts_start_time;
                        }
                        dic_start_time[pid] = 0;
                        dic_last_time[pid] = 0;
                    }
                    else
                    {
                        dic_last_time[pid] = item.TimeStamp;
                    }
                }
            }

            // 计算在截至时间点仍在进行的游戏时间
            foreach (var (pid, ts_start_time) in dic_start_time)
            {
                long ts_last_time = dic_last_time.GetValueOrDefault(pid);
                if (ts_start_time != 0 && ts_end - ts_last_time <= MAX_INTERVAL)
                {
                    ts_total += Math.Clamp(ts_end - ts_start_time, 0, long.MaxValue);
                }
            }
        }
        return TimeSpan.FromMilliseconds(ts_total);
    }




    public async Task<Process?> StartProcessToLogAsync(GameBiz biz)
    {
        try
        {
            var name = GameResourceService.GetGameExeName(biz).Replace(".exe", "");
            for (int i = 0; i < 10; i++)
            {
                await Task.Delay(2000);
                var processes = Process.GetProcessesByName(name);
                if (processes.Length == 0)
                {
                    if (i < 5)
                    {
                        continue;
                    }
                    return null;
                }
                foreach (var process in processes)
                {
                    if (biz.ToGame() == GameBiz.bh3 && process.MainWindowHandle == 0)
                    {
                        _logger.LogInformation("Game process ({biz}, {gamePid}) has no window", biz, process.Id);
                        continue;
                    }
                    var instance = App.FindInstanceForKey($"playtime_{process.Id}");
                    if (instance != null)
                    {
                        _logger.LogInformation("Game process ({biz}, {gamePid}) has been recorded by process ({playtimePid})", biz, process.Id, instance.ProcessId);
                        continue;
                    }
                    _logger.LogInformation("Start to log playtime ({biz}, {pid})", biz, process.Id);
                    var exe = Process.GetCurrentProcess().MainModule?.FileName ?? Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                    if (File.Exists(exe))
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = exe,
                            Arguments = $"playtime --biz {biz} --pid {process.Id}",
                            CreateNoWindow = true,
                        });
                    }
                    return process;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start process to log play time");
        }
        return null;
    }





    private struct PlayTimeItemStruct
    {


        public long TimeStamp { get; set; }


        public GameBiz GameBiz { get; set; }


        public int Pid { get; set; }


        public PlayTimeItem.PlayState State { get; set; }


        public long CursorPos { get; set; }


    }



}
