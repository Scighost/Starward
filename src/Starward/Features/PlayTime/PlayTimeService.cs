using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.Database;
using Starward.Features.GameLauncher;
using Starward.Features.HoYoPlay;
using System;
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

    private const long SplitSessionInterval = 60_000;

    private const int MaxConsecutiveUpdateFailures = 3;



    /// <summary>
    /// 记录游戏进程的游戏时间
    /// </summary>
    public async Task LogPlayTimeAsync(GameBiz biz, int pid)
    {
        long? startTimeStamp = null;
        try
        {
            var instance = AppInstance.FindOrRegisterForKey($"playtime_{pid}");
            if (!instance.IsCurrent)
            {
                _logger.LogWarning("Game process ({biz}, {gamePid}) has been recorded by process ({playtimePid})", biz, pid, instance.ProcessId);
                return;
            }
            _logger.LogInformation("Start to log playtime ({biz}, {pid})", biz, pid);
            CloseStaleSessions();
            var process = Process.GetProcessById(pid);
            startTimeStamp = LogStartState(biz, process);
            long lastUpdateTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            int consecutiveUpdateFailures = 0;
            void CountUpdateFailure()
            {
                if (++consecutiveUpdateFailures >= MaxConsecutiveUpdateFailures)
                {
                    throw new InvalidOperationException($"Update playtime session ({biz}, {pid}) failed {MaxConsecutiveUpdateFailures} times consecutively");
                }
            }
            using var connection = DatabaseService.CreateConnection();
            while (true)
            {
                await Task.Delay(Random.Shared.Next(4500, 5500));
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                if (now - lastUpdateTime > SplitSessionInterval)
                {
                    // 间隔过大（如系统休眠或挂起）视为会话中断，关闭当前会话并开启新会话，避免将休眠时间计入游戏时长
                    if (Update(connection, biz, pid, startTimeStamp, PlayTimeState.Stopped, lastUpdateTime) == 0)
                    {
                        CountUpdateFailure();
                    }
                    if (process.HasExited)
                    {
                        break;
                    }
                    startTimeStamp = LogStartState(biz, process, now);
                    lastUpdateTime = now;
                    consecutiveUpdateFailures = 0;
                }
                else if (process.HasExited)
                {
                    if (Update(connection, biz, pid, startTimeStamp, PlayTimeState.Stopped, now) == 0)
                    {
                        _logger.LogWarning("Close playtime session ({biz}, {pid}) failed at {time}", biz, pid, now);
                    }
                    break;
                }
                else
                {
                    if (Update(connection, biz, pid, startTimeStamp, PlayTimeState.Running, now) != 0)
                    {
                        lastUpdateTime = now;
                        consecutiveUpdateFailures = 0;
                    }
                    else
                    {
                        CountUpdateFailure();
                    }
                }
            }
            DatabaseService.SetValue($"playtime_total_{biz}", GetPlayTimeTotal(biz));
            DatabaseService.SetValue($"playtime_month_{biz}", GetPlayCurrentMonth(biz));
            DatabaseService.SetValue($"playtime_week_{biz}", GetPlayCurrentWeek(biz));
            DatabaseService.SetValue($"playtime_day_{biz}", GetPlayCurrentDay(biz));
            DatabaseService.SetValue($"startup_count_{biz}", GetStartUpCount(biz));
            _logger.LogInformation("End log playtime ({biz}, {pid})", biz, pid);
        }
        catch (Exception ex)
        {
            using var connection = DatabaseService.CreateConnection();
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (startTimeStamp is not null)
            {
                Update(connection, biz, pid, startTimeStamp, PlayTimeState.Error, now, ex.Message);
            }
            _logger.LogError(ex, "Log play time: GameBiz {biz}, Pid {pid}", biz, pid);
        }
    }





    /// <summary>
    /// 开始记录游戏时间，返回会话开始时间戳
    /// </summary>
    private long LogStartState(GameBiz biz, Process process, long? startTime = null)
    {
        long processStartTimeStamp = new DateTimeOffset(process.StartTime).ToUnixTimeMilliseconds();
        long startTimeStamp = startTime ?? processStartTimeStamp;
        long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        using var dapper = DatabaseService.CreateConnection();
        // 重新挂接时续接同一进程最近一段会话，补全记录器中断期间的时间
        if (startTime is null)
        {
            long latest = dapper.ExecuteScalar<long>(
                "SELECT COALESCE(MAX(StartTimeStamp), 0) FROM PlayTimeInfo WHERE GameBiz = @biz AND Pid = @pid AND StartTimeStamp >= @processStartTimeStamp;",
                new { biz, pid = process.Id, processStartTimeStamp });
            if (latest > 0)
            {
                dapper.Execute($"""
                    UPDATE PlayTimeInfo
                    SET LatestTimeStamp = @now,
                        LatestTime = strftime('%Y/%m/%d %H:%M:%S', @now / 1000, 'unixepoch', 'localtime'),
                        State = {(int)PlayTimeState.Running},
                        Duration = @now - StartTimeStamp,
                        PlayTime = printf('%02d:%02d:%02d', (@now - StartTimeStamp) / 3600000, ((@now - StartTimeStamp) % 3600000) / 60000, ((@now - StartTimeStamp) % 60000) / 1000)
                    WHERE GameBiz = @biz AND Pid = @pid AND StartTimeStamp = @latest;
                    """, new { now, biz, pid = process.Id, latest });
                return latest;
            }
        }
        // 若进程启动时间戳已被占用（与其他记录的主键冲突），则从当前时间开始新会话
        if (dapper.ExecuteScalar<int>("SELECT COUNT(*) FROM PlayTimeInfo WHERE StartTimeStamp = @startTimeStamp;", new { startTimeStamp }) > 0)
        {
            startTimeStamp = now;
        }
        var info = new PlayTimeInfo
        {
            StartTimeStamp = startTimeStamp,
            LatestTimeStamp = now,
            GameBiz = biz,
            Pid = process.Id,
            State = PlayTimeState.Running,
            Duration = now - startTimeStamp,
        };
        dapper.Execute("""
            INSERT INTO PlayTimeInfo (StartTimeStamp, LatestTimeStamp, GameBiz, Pid, State, Duration, Message, StartTime, LatestTime, PlayTime)
            VALUES (@StartTimeStamp, @LatestTimeStamp, @GameBiz, @Pid, @State, @Duration, @Message,
                    strftime('%Y/%m/%d %H:%M:%S', @StartTimeStamp / 1000, 'unixepoch', 'localtime'),
                    strftime('%Y/%m/%d %H:%M:%S', @LatestTimeStamp / 1000, 'unixepoch', 'localtime'),
                    printf('%02d:%02d:%02d', @Duration / 3600000, (@Duration % 3600000) / 60000, (@Duration % 60000) / 1000));
            """, info);
        return startTimeStamp;
    }





    /// <summary>
    /// 更新当前游戏会话的记录状态、时长和消息，返回受影响的行数
    /// </summary>
    private int Update(SqliteConnection connection, GameBiz biz, int pid, long? startTimeStamp, PlayTimeState state, long latestTimeStamp, string? message = null)
    {
        try
        {
            // 仅更新进行中的会话，避免覆盖已正常结束的记录
            return connection.Execute($"""
                UPDATE PlayTimeInfo
                SET LatestTimeStamp = @latestTimeStamp,
                    LatestTime = strftime('%Y/%m/%d %H:%M:%S', @latestTimeStamp / 1000, 'unixepoch', 'localtime'),
                    State = @state,
                    Duration = @latestTimeStamp - StartTimeStamp,
                    PlayTime = printf('%02d:%02d:%02d', (@latestTimeStamp - StartTimeStamp) / 3600000, ((@latestTimeStamp - StartTimeStamp) % 3600000) / 60000, ((@latestTimeStamp - StartTimeStamp) % 60000) / 1000),
                    Message = CASE WHEN @state = {(int)PlayTimeState.Error} THEN @message ELSE Message END
                WHERE GameBiz = @biz AND Pid = @pid AND State = {(int)PlayTimeState.Running} AND StartTimeStamp = @startTimeStamp;
                """, new { latestTimeStamp, state, message, biz, pid, startTimeStamp });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update play time: GameBiz {biz}, Pid {pid}, State {state}", biz, pid, state);
            return 0;
        }
    }



    /// <summary>
    /// 关闭所有已无对应游戏进程的进行中会话（记录进程崩溃或强制退出后残留的会话）
    /// </summary>
    public void CloseStaleSessions()
    {
        try
        {
            using var dapper = DatabaseService.CreateConnection();
            var running = dapper.Query<PlayTimeInfo>($"SELECT * FROM PlayTimeInfo WHERE State = {(int)PlayTimeState.Running};").ToList();
            if (running.Count == 0)
            {
                return;
            }
            var stale = running.Where(item => !IsProcessAlive(item.Pid, item.StartTimeStamp)).Select(item => item.StartTimeStamp).ToList();
            if (stale.Count > 0)
            {
                dapper.Execute($"UPDATE PlayTimeInfo SET State = {(int)PlayTimeState.Stopped} WHERE State = {(int)PlayTimeState.Running} AND StartTimeStamp IN @stale;", new { stale });
                _logger.LogInformation("Close {count} stale playtime session(s)", stale.Count);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Close stale playtime sessions");
        }
    }



    private static bool IsProcessAlive(int pid, long sessionStartTimeStamp)
    {
        try
        {
            using var process = Process.GetProcessById(pid);
            // PID 可能已被复用，进程启动时间晚于会话开始时间视为会话已失效
            return !process.HasExited && new DateTimeOffset(process.StartTime).ToUnixTimeMilliseconds() <= sessionStartTimeStamp;
        }
        catch
        {
            return false;
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
        return dapper.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM PlayTimeInfo WHERE GameBiz = @biz;", new { biz });
    }



    /// <summary>
    /// 获取最后一次游戏时间
    /// </summary>
    /// <param name="biz"></param>
    /// <returns></returns>
    public (DateTimeOffset Time, TimeSpan Span) GetLastPlayTime(GameBiz biz)
    {
        using var dapper = DatabaseService.CreateConnection();
        var item = dapper.QueryFirstOrDefault<PlayTimeInfo>("SELECT * FROM PlayTimeInfo WHERE GameBiz = @biz ORDER BY LatestTimeStamp DESC LIMIT 1;", new { biz });
        if (item != null)
        {
            return (DateTimeOffset.FromUnixTimeMilliseconds(item.StartTimeStamp), TimeSpan.FromMilliseconds(item.Duration));
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
        long ms = dapper.QuerySingleOrDefault<long>("""
            SELECT COALESCE(SUM(MAX(0, MIN(LatestTimeStamp, @ts_end) - MAX(StartTimeStamp, @ts_start))), 0)
            FROM PlayTimeInfo
            WHERE GameBiz = @biz AND StartTimeStamp <= @ts_end AND LatestTimeStamp >= @ts_start;
            """, new { ts_start, ts_end, biz });
        return TimeSpan.FromMilliseconds(ms);
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
                        FileName = AppConfig.StarwardExecutePath,
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

            Process? p = Process.Start(new ProcessStartInfo
            {
                FileName = AppConfig.StarwardExecutePath,
                Arguments = $"playtime --biz {biz} --pid {process.Id}",
                CreateNoWindow = true,
            });
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
        string? name = GameLauncherService.GetGameExeName(gameId.GameBiz);
        if (string.IsNullOrWhiteSpace(name))
        {
            var config = await _hoYoPlayService.GetGameConfigAsync(gameId);
            name = config?.ExeFileName;
        }
        return name?.Replace(".exe", "") ?? throw new ArgumentOutOfRangeException($"Unknown game ({gameId.Id}, {gameId.GameBiz}).");
    }



    #endregion




}
