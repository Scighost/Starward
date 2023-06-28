using Dapper;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Services;

internal class GameRecordService
{


    private readonly ILogger<GameRecordService> _logger;

    private readonly DatabaseService _databaseService;

    private readonly HyperionClient _hyperionClient;

    private readonly HoyolabClient _hoyolabClient;

    private GameRecordClient _gameRecordClient;


    public event EventHandler<GameRecordRole?> GameRecordRoleChanged;


    private bool isHoyolab;
    public bool IsHoyolab
    {
        get => isHoyolab;
        set
        {
            if (value)
            {
                _gameRecordClient = _hoyolabClient;
                _logger.LogInformation("Change region to Global.");
            }
            else
            {
                _gameRecordClient = _hyperionClient;
                _logger.LogInformation("Change region to China.");
            }
            isHoyolab = value;
        }
    }


    public GameRecordService(ILogger<GameRecordService> logger, DatabaseService databaseService, HyperionClient hyperionClient, HoyolabClient hoyolabClient)
    {
        _logger = logger;
        _databaseService = databaseService;
        _hyperionClient = hyperionClient;
        _hoyolabClient = hoyolabClient;
        _gameRecordClient = hyperionClient;
    }



    public void InvokeGameRecordRoleChanged(GameRecordRole? role)
    {
        GameRecordRoleChanged?.Invoke(this, role);
    }




    #region Game Role



    public async Task<GameRecordUser> AddRecordUserAsync(string cookie, CancellationToken cancellationToken = default)
    {
        var user = await _gameRecordClient.GetGameRecordUserAsync(cookie, cancellationToken);
        using var dapper = _databaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO GameRecordUser (Uid, IsHoyolab, Nickname, Avatar, Introduce, Gender, AvatarUrl, Pendant, Cookie)
            VALUES (@Uid, @IsHoyolab, @Nickname, @Avatar, @Introduce, @Gender, @AvatarUrl, @Pendant, @Cookie);
            """, user);
        return user;
    }



    public List<GameRecordUser> GetRecordUsers()
    {
        using var dapper = _databaseService.CreateConnection();
        var list = dapper.Query<GameRecordUser>("SELECT * FROM GameRecordUser WHERE IsHoyolab = @IsHoyolab;", new { IsHoyolab });
        return list.ToList();
    }



    public async Task<List<GameRecordRole>> AddGameRolesAsync(string cookie, CancellationToken cancellationToken = default)
    {
        var list = await _gameRecordClient.GetAllGameRolesAsync(cookie, cancellationToken);
        using var dapper = _databaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("""
            INSERT OR REPLACE INTO GameRecordRole (Uid, GameBiz, Nickname, Level, Region, RegionName, Cookie)
            VALUES (@Uid, @GameBiz, @Nickname, @Level, @Region, @RegionName, @Cookie);
            """, list, t);
        t.Commit();
        return list;
    }




    public List<GameRecordRole> GetGameRoles(GameBiz gameBiz)
    {
        using var dapper = _databaseService.CreateConnection();
        var list = dapper.Query<GameRecordRole>("SELECT * FROM GameRecordRole WHERE GameBiz = @gameBiz;", new { gameBiz = gameBiz.ToString() });
        return list.ToList();
    }



    public GameRecordRole? GetLastSelectGameRecordRole(GameBiz gameBiz)
    {
        using var dapper = _databaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<GameRecordRole>("""
            SELECT r.* FROM GameRecordRole r INNER JOIN Setting s ON s.Value = r.Uid WHERE r.GameBiz = @gameBiz AND s.Key = @key LIMIT 1;
            """, new { gameBiz = gameBiz.ToString(), key = $"last_select_game_record_role_{gameBiz}" });
    }



    public void SetLastSelectGameRecordRole(GameBiz gameBiz, GameRecordRole role)
    {
        using var dapper = _databaseService.CreateConnection();
        dapper.Execute("INSERT OR REPLACE INTO Setting (Key, Value) VALUES (@key, @value);", new { key = $"last_select_game_record_role_{gameBiz}", value = role.Uid.ToString() });
    }


    public GameRecordUser? GetGameRecordUser(GameRecordRole? role)
    {
        if (role is null)
        {
            return null;
        }
        using var dapper = _databaseService.CreateConnection();
        return dapper.QueryFirstOrDefault<GameRecordUser>("SELECT * FROM GameRecordUser WHERE Cookie = @Cookie LIMIT 1;", new { role.Cookie });
    }



    public async Task RefreshAllGameRolesInfoAsync()
    {
        var users = GetRecordUsers();
        foreach (var user in users)
        {
            await AddRecordUserAsync(user.Cookie!);
            await AddGameRolesAsync(user.Cookie!);
        }
    }


    public async Task RefreshGameRoleInfoAsync(GameRecordRole role)
    {
        await AddRecordUserAsync(role.Cookie!);
        await AddGameRolesAsync(role.Cookie!);
    }



    /// <summary>
    /// 删除游戏角色，返回是否删除全部账号
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public bool DeleteGameRole(GameRecordRole role)
    {
        bool deletedUser = false;
        using var dapper = _databaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("DELETE FROM GameRecordRole WHERE GameBiz = @GameBiz AND Uid = @Uid;", role, t);
        _logger.LogInformation("Deleted game roles with ({nickname}, {gameBiz}, {uid}).", role.Nickname, role.GameBiz, role.Uid);
        if (dapper.QueryFirstOrDefault<int>("SELECT Count(*) FROM GameRecordRole WHERE Cookie = @Cookie;", role, t) == 0)
        {
            dapper.Execute("DELETE FROM GameRecordUser WHERE Cookie = @Cookie;", role, t);
            _logger.LogInformation("Deleted all relative accounts of ({nickname}, {gameBiz}, {uid})", role.Nickname, role.GameBiz, role.Uid);
            deletedUser = true;
        }
        t.Commit();
        return deletedUser;
    }



    #endregion




    /// <summary>
    /// 深境螺旋
    /// </summary>
    /// <param name="role"></param>
    /// <param name="schedule">1当期，2上期</param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<SpiralAbyssInfo> RefreshSpiralAbyssInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var info = await _gameRecordClient.GetSpiralAbyssInfoAsync(role, schedule);
        var obj = new
        {
            info.Uid,
            info.ScheduleId,
            info.StartTime,
            info.EndTime,
            info.TotalBattleCount,
            info.TotalWinCount,
            info.MaxFloor,
            info.TotalStar,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = _databaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO SpiralAbyssInfo (Uid, ScheduleId, StartTime, EndTime, TotalBattleCount, TotalWinCount, MaxFloor, TotalStar, Value)
            VALUES (@Uid, @ScheduleId, @StartTime, @EndTime, @TotalBattleCount, @TotalWinCount, @MaxFloor, @TotalStar, @Value);
            """, obj);
        return info;
    }




    public List<SpiralAbyssInfo> GetSpiralAbyssInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<SpiralAbyssInfo>();
        }
        using var dapper = _databaseService.CreateConnection();
        var list = dapper.Query<SpiralAbyssInfo>("""
            SELECT Uid, ScheduleId, StartTime, EndTime, TotalBattleCount, TotalWinCount, MaxFloor, TotalStar FROM SpiralAbyssInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public SpiralAbyssInfo? GetSpiralAbyssInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = _databaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM SpiralAbyssInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<SpiralAbyssInfo>(value);
    }






}
