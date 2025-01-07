using Dapper;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.Database;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace Starward.Features.PlayTime;

internal class PlayTimeService
{

    private readonly ILogger<PlayTimeService> _logger;

    private readonly HoYoPlayService _hoYoPlayService;



    public PlayTimeService(ILogger<PlayTimeService> logger, HoYoPlayService hoYoPlayService)
    {
        _logger = logger;
        _hoYoPlayService = hoYoPlayService;
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
            _logger.LogInformation("Start to log playtime ({biz}, {pid})", biz, pid);
            Log(biz, pid, PlayTimeState.None, message: "Ready to log time");
            var process = Process.GetProcessById(pid);
            LogStartState(biz, process);
            var sw = Stopwatch.StartNew();
            long last = 0;
            while (true)
            {
                await Task.Delay(Random.Shared.Next(800, 1200));
                if (process.HasExited)
                {
                    var now = DateTimeOffset.Now;
                    Log(biz, pid, PlayTimeState.Stop, now.ToUnixTimeMilliseconds(), $"{process.ProcessName} [{now}]");
                    break;
                }
                else
                {
                    if (sw.ElapsedMilliseconds - last > 30000)
                    {
                        Log(biz, pid, PlayTimeState.Play);
                        last = sw.ElapsedMilliseconds;
                    }
                }
            }
            DatabaseService.SetValue($"playtime_total_{biz}", GetPlayTimeTotal(biz));
            DatabaseService.SetValue($"startup_count_{biz}", GetStartUpCount(biz));
            _logger.LogInformation("End log playtime ({biz}, {pid})", biz, pid);
        }
        catch (Exception ex)
        {
            Log(biz, pid, PlayTimeState.Error, 0, ex.Message);
            _logger.LogError(ex, "Log play time: GameBiz {biz}, Pid {pid}", biz, pid);
        }
    }





    private void LogStartState(GameBiz biz, Process process)
    {
        var time = new DateTimeOffset(process.StartTime);
        Log(biz, process.Id, PlayTimeState.Start, time.ToUnixTimeMilliseconds(), $"{process.ProcessName} [{time}]");
        var now = DateTimeOffset.Now;
        if (now - process.StartTime >= TimeSpan.FromSeconds(60))
        {
            while (true)
            {
                // 补全从开始游戏到开始记录游戏时间之间的记录
                time = time.AddMilliseconds(Random.Shared.Next(30_000, 32_000));
                if (time < now)
                {
                    Log(biz, process.Id, PlayTimeState.Play, time.ToUnixTimeMilliseconds());
                }
                else
                {
                    break;
                }
            }
        }
    }





    private void Log(GameBiz biz, int pid, PlayTimeState state, long ts = 0, string? message = null)
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            var item = new PlayTimeItem
            {
                TimeStamp = ts == 0 ? DateTimeOffset.Now.ToUnixTimeMilliseconds() : ts,
                GameBiz = biz,
                Pid = pid,
                State = state,
                Message = message,
            };
            dapper.Execute("INSERT OR REPLACE INTO PlayTimeItem (TimeStamp, GameBiz, Pid, State, CursorPos, Message) VALUES (@TimeStamp, @GameBiz, @Pid, @State, @CursorPos, @Message);", item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log play time: GameBiz {biz}, Pid {pid}, State {state}, Message {message}", biz, pid, state, message);
        }
    }





    #region Calculate Play Time




    /// <summary>
    /// 获取总游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public TimeSpan GetPlayTimeTotal(GameBiz biz)
    {
        return CalculatePlayTime(biz);
    }


    /// <summary>
    /// 获取本月游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public TimeSpan GetPlayCurrentMonth(GameBiz biz)
    {
        var now = DateTimeOffset.Now;
        var month = now.Add(-now.TimeOfDay).AddDays(1 - now.Day);
        return CalculatePlayTime(biz, month, now);
    }


    /// <summary>
    /// 获取本周游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public TimeSpan GetPlayCurrentWeek(GameBiz biz)
    {
        var now = DateTimeOffset.Now;
        var week = now.Add(-now.TimeOfDay).AddDays(-(((int)now.DayOfWeek + 6) % 7));
        return CalculatePlayTime(biz, week, now);
    }


    /// <summary>
    /// 获取当天游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public TimeSpan GetPlayCurrentDay(GameBiz biz)
    {
        var now = DateTimeOffset.Now;
        var day = now.Add(-now.TimeOfDay);
        return CalculatePlayTime(biz, day, now);
    }



    /// <summary>
    /// 获取最近 7 天游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public TimeSpan GetPlayTimeLast7Days(GameBiz biz)
    {
        var now = DateTimeOffset.Now;
        var week = now.Add(-now.TimeOfDay).AddDays(-7);
        return CalculatePlayTime(biz, week, now);
    }


    /// <summary>
    /// 获取启动次数
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public int GetStartUpCount(GameBiz biz)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM PlayTimeItem WHERE GameBiz = @biz AND State = @state;", new { biz, state = PlayTimeState.Start });
    }



    /// <summary>
    /// 获取最后一次游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public (DateTimeOffset Time, TimeSpan Span) GetLastPlayTime(GameBiz biz)
    {
        using var dapper = DatabaseService.CreateConnection();
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



    /// <summary>
    /// 计算游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    public TimeSpan CalculatePlayTime(GameBiz biz, DateTimeOffset? start = null, DateTimeOffset? end = null)
    {
        long ts_start = start?.ToUnixTimeMilliseconds() ?? 0;
        long ts_end = end?.ToUnixTimeMilliseconds() ?? long.MaxValue;
        using var dapper = DatabaseService.CreateConnection();
        var items = dapper.Query<PlayTimeItemStruct>("SELECT * FROM PlayTimeItem WHERE GameBiz = @biz AND TimeStamp >= @ts_start AND TimeStamp <= @ts_end ORDER BY TimeStamp;", new { biz, ts_start, ts_end }).ToList();
        return CalculatePlayTime(items, start, end);
    }


    /// <summary>
    /// 计算游戏时间
    /// </summary>
    /// <param name="items"></param>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <returns></returns>
    private static TimeSpan CalculatePlayTime(List<PlayTimeItemStruct> items, DateTimeOffset? start = null, DateTimeOffset? end = null)
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
            if (items[0].State is PlayTimeState.Start)
            {
                ts_total += Math.Clamp(items[0].TimeStamp - ts_start, 0, MAX_INTERVAL);
            }
            else if (items[0].State is PlayTimeState.Stop)
            {
                ts_total += Math.Clamp(ts_end - items[0].TimeStamp, 0, MAX_INTERVAL);
            }
            else if (items[0].State is PlayTimeState.Play)
            {
                ts_total += Math.Clamp(ts_end - ts_start, 0, MAX_INTERVAL);
            }
        }
        else
        {
            var dic_start_time = new Dictionary<int, long>();
            var dic_last_time = new Dictionary<int, long>();

            if (items[0].State is PlayTimeState.Play or PlayTimeState.Stop && items[0].TimeStamp - ts_start <= MAX_INTERVAL)
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
                    if (item.State is not PlayTimeState.Stop or PlayTimeState.Error)
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
                    if (item.State is PlayTimeState.Start)
                    {
                        long ts_start_time = dic_start_time.GetValueOrDefault(pid);
                        if (ts_start_time != 0)
                        {
                            ts_total += Math.Clamp(ts_last_time - ts_start_time, 0, long.MaxValue);
                        }
                        dic_start_time[pid] = item.TimeStamp;
                        dic_last_time[pid] = item.TimeStamp;
                    }
                    else if (item.State is PlayTimeState.Stop or PlayTimeState.Error)
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

            // 计算因意外或正在运行，没有停止记录的游戏时间
            foreach (var (pid, ts_start_time) in dic_start_time)
            {
                long ts_last_time = dic_last_time.GetValueOrDefault(pid);
                if (ts_start_time != 0 && ts_last_time != 0)
                {
                    ts_total += Math.Clamp(ts_last_time - ts_start_time, 0, long.MaxValue);
                }
            }
        }
        return TimeSpan.FromMilliseconds(ts_total);
    }



    #endregion





    #region Start process to log playtime



    /// <summary>
    /// 启动进程记录游戏时间，返回游戏进程
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<Process?> StartProcessToLogAsync(GameId gameId)
    {
        try
        {
            var biz = gameId.GameBiz;
            string name = await GetGameExeNameWithoutExtensionAsync(gameId);
            for (int i = 0; i < 15; i++)
            {
                await Task.Delay(2000);
                var processes = Process.GetProcessesByName(name);
                if (processes.Length == 0)
                {
                    if (i < 5)
                    {
                        continue;
                    }
                    // 未找到游戏进程
                    return null;
                }
                foreach (var process in processes)
                {
                    var instance = App.FindInstanceForKey($"playtime_{process.Id}");
                    if (instance != null)
                    {
                        // 已经有进程在记录该游戏
                        _logger.LogInformation("Game process ({biz}, {gamePid}) has been recorded by process ({playtimePid})", biz, process.Id, instance.ProcessId);
                        continue;
                    }
                    if (process.SessionId != Process.GetCurrentProcess().SessionId)
                    {
                        // 游戏进程不在当前会话
                        _logger.LogWarning("Game process ({biz}, {gamePid}) is not in the current session", biz, process.Id);
                        continue;
                    }
                    _logger.LogInformation("Start to log playtime ({biz}, {pid})", biz, process.Id);
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = AppSetting.StarwardExecutePath,
                        Arguments = $"playtime --biz {biz} --pid {process.Id}",
                        CreateNoWindow = true,
                    });
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


    /// <summary>
    /// 启动进程记录游戏时间
    /// </summary>
    /// <param name="gameId"></param>
    /// <param name="pid"></param>
    /// <returns></returns>
    public async Task StartProcessToLogAsync(GameId gameId, int pid)
    {
        try
        {
            Process process = Process.GetProcessById(pid);
            var biz = gameId.GameBiz;
            string name = await GetGameExeNameWithoutExtensionAsync(gameId);
            if (process.ProcessName != name)
            {
                _logger.LogWarning("Game process ({biz}, {gamePid}) is not the expected process ({name})", biz, pid, process.ProcessName);
                return;
            }
            var instance = App.FindInstanceForKey($"playtime_{pid}");
            if (instance != null)
            {
                _logger.LogWarning("Game process ({biz}, {gamePid}) has been recorded by process ({playtimePid})", biz, pid, instance.ProcessId);
                return;
            }

            var p = Process.Start(new ProcessStartInfo
            {
                FileName = AppSetting.StarwardExecutePath,
                Arguments = $"playtime --biz {biz} --pid {process.Id}",
                CreateNoWindow = true,
            });
            _logger.LogInformation(p.StartInfo.Arguments);
            _logger.LogInformation("Start process to log play time: GameBiz {biz}, Pid {pid}, ProcessId {processId}", biz, pid, p?.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start process to log play time: GameBiz {biz}, Pid {pid}", gameId.GameBiz, pid);
        }
    }




    /// <summary>
    /// 游戏进程名，不带 .exe 扩展名
    /// </summary>
    /// <param name="gameId"></param>
    /// <returns></returns>
    public async Task<string> GetGameExeNameWithoutExtensionAsync(GameId gameId)
    {
        string? name = gameId.GameBiz.Value switch
        {
            GameBiz.hk4e_cn or GameBiz.hk4e_bilibili => "YuanShen.exe",
            GameBiz.hk4e_global => "GenshinImpact.exe",
            _ => gameId.GameBiz.Game switch
            {
                GameBiz.hkrpg => "StarRail.exe",
                GameBiz.bh3 => "BH3.exe",
                GameBiz.nap => "ZenlessZoneZero.exe",
                _ => null,
            },
        };
        if (string.IsNullOrWhiteSpace(name))
        {
            var config = await _hoYoPlayService.GetGameConfigAsync(gameId);
            name = config?.ExeFileName;
        }
        return name?.Replace(".exe", "") ?? throw new ArgumentOutOfRangeException($"Unknown game ({gameId.Id}, {gameId.GameBiz}).");
    }



    #endregion




    private struct PlayTimeItemStruct
    {

        public long TimeStamp { get; set; }


        public GameBiz GameBiz { get; set; }


        public int Pid { get; set; }


        public PlayTimeState State { get; set; }

    }



}
