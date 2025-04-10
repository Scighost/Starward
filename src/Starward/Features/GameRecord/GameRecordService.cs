using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.BH3.DailyNote;
using Starward.Core.GameRecord.Genshin.DailyNote;
using Starward.Core.GameRecord.Genshin.ImaginariumTheater;
using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Core.GameRecord.StarRail.ApocalypticShadow;
using Starward.Core.GameRecord.StarRail.DailyNote;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
using Starward.Core.GameRecord.StarRail.PureFiction;
using Starward.Core.GameRecord.StarRail.SimulatedUniverse;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;
using Starward.Core.GameRecord.ZZZ.DailyNote;
using Starward.Core.GameRecord.ZZZ.DeadlyAssault;
using Starward.Core.GameRecord.ZZZ.InterKnotReport;
using Starward.Core.GameRecord.ZZZ.ShiyuDefense;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord;

internal class GameRecordService
{


    private readonly ILogger<GameRecordService> _logger;

    private readonly HyperionClient _hyperionClient;

    private readonly HoyolabClient _hoyolabClient;

    private GameRecordClient _gameRecordClient;

    private readonly IMemoryCache _memoryCache;


    public string Language { get => _hoyolabClient.Language; set => _hoyolabClient.Language = value; }


    private bool isHoyolab;
    public bool IsHoyolab
    {
        get => isHoyolab;
        set
        {
            if (value)
            {
                _gameRecordClient = _hoyolabClient;
            }
            else
            {
                _gameRecordClient = _hyperionClient;
            }
            isHoyolab = value;
        }
    }


    public GameRecordService(ILogger<GameRecordService> logger, HyperionClient hyperionClient, HoyolabClient hoyolabClient, IMemoryCache memoryCache)
    {
        _logger = logger;
        _hyperionClient = hyperionClient;
        _hoyolabClient = hoyolabClient;
        _gameRecordClient = hyperionClient;
        _memoryCache = memoryCache;
    }





    /// <summary>
    /// 更新设备指纹信息
    /// </summary>
    /// <param name="forceUpdate"></param>
    /// <returns></returns>
    public async Task UpdateDeviceFpAsync(bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        if (IsHoyolab)
        {
            return;
        }
        string? id = AppConfig.HyperionDeviceId;
        string? fp = AppConfig.HyperionDeviceFp;
        DateTimeOffset lastUpdateTime = AppConfig.HyperionDeviceFpLastUpdateTime;
        if (!forceUpdate && !string.IsNullOrWhiteSpace(id) && !string.IsNullOrWhiteSpace(fp))
        {
            _gameRecordClient.DeviceId = id;
            _gameRecordClient.DeviceFp = fp;
        }
        if (forceUpdate || DateTimeOffset.Now - lastUpdateTime > TimeSpan.FromDays(3))
        {
            await _gameRecordClient.GetDeviceFpAsync(cancellationToken);
            AppConfig.HyperionDeviceId = _gameRecordClient.DeviceId;
            AppConfig.HyperionDeviceFp = _gameRecordClient.DeviceFp;
            AppConfig.HyperionDeviceFpLastUpdateTime = DateTimeOffset.Now;
        }
    }




    private BattleChronicleWindow? _battleChronicleWindow;


    public void ShowBattleChronicleWindow(GameBiz gameBiz, GameRecordRole? gameRecordRole)
    {
        // 窗口关闭后 AppWindow is null
        if (_battleChronicleWindow?.AppWindow is null)
        {
            _battleChronicleWindow = new BattleChronicleWindow
            {
                CurrentRole = gameRecordRole,
            };
        }
        else if (_battleChronicleWindow.CurrentRole != gameRecordRole)
        {
            _battleChronicleWindow.CurrentRole = gameRecordRole;
        }
        _battleChronicleWindow.Activate();
    }





    #region Game Role



    public async Task<GameRecordUser> AddRecordUserAsync(string cookie, CancellationToken cancellationToken = default)
    {
        var user = await _gameRecordClient.GetGameRecordUserAsync(cookie, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO GameRecordUser (Uid, IsHoyolab, Nickname, Avatar, Introduce, Gender, AvatarUrl, Pendant, Cookie)
            VALUES (@Uid, @IsHoyolab, @Nickname, @Avatar, @Introduce, @Gender, @AvatarUrl, @Pendant, @Cookie);
            """, user);
        return user;
    }



    public List<GameRecordUser> GetRecordUsers()
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GameRecordUser>("SELECT * FROM GameRecordUser WHERE IsHoyolab = @IsHoyolab;", new { IsHoyolab });
        return list.ToList();
    }



    public async Task<List<GameRecordRole>> AddGameRolesAsync(string cookie, CancellationToken cancellationToken = default)
    {
        var list = await _gameRecordClient.GetAllGameRolesAsync(cookie, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("""
            INSERT OR REPLACE INTO GameRecordRole (Uid, GameBiz, Nickname, Level, Region, RegionName, Cookie, HeadIcon)
            VALUES (@Uid, @GameBiz, @Nickname, @Level, @Region, @RegionName, @Cookie, @HeadIcon);
            """, list, t);
        t.Commit();
        return list;
    }




    public List<GameRecordRole> GetGameRoles(GameBiz gameBiz)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GameRecordRole>("SELECT * FROM GameRecordRole WHERE GameBiz = @gameBiz;", new { gameBiz });
        return list.ToList();
    }



    public GameRecordRole? GetLastSelectGameRecordRoleOrTheFirstOne(GameBiz gameBiz)
    {
        using var dapper = DatabaseService.CreateConnection();
        var role = dapper.QueryFirstOrDefault<GameRecordRole>("""
            SELECT r.* FROM GameRecordRole r INNER JOIN Setting s ON s.Value = r.Uid WHERE r.GameBiz = @gameBiz AND s.Key = @key LIMIT 1;
            """, new { gameBiz, key = $"last_select_game_record_role_{gameBiz}" });
        return role ??= dapper.QueryFirstOrDefault<GameRecordRole>("SELECT * FROM GameRecordRole WHERE GameBiz = @gameBiz LIMIT 1;", new { gameBiz });
    }



    public void SetLastSelectGameRecordRole(GameBiz gameBiz, GameRecordRole role)
    {
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("INSERT OR REPLACE INTO Setting (Key, Value) VALUES (@key, @value);", new { key = $"last_select_game_record_role_{gameBiz}", value = role.Uid.ToString() });
    }


    public GameRecordUser? GetGameRecordUser(GameRecordRole? role)
    {
        if (role is null)
        {
            return null;
        }
        using var dapper = DatabaseService.CreateConnection();
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



    public async Task UpdateGameRoleHeadIconAsync(GameRecordRole role)
    {
        string key = $"game_record_role_head_icon_{role.GameBiz}_{role.Region}_{role.Uid}";
        if (!_memoryCache.TryGetValue(key, out bool _))
        {
            role = await _gameRecordClient.UpdateGameRoleHeadIconAsync(role);
            using var dapper = DatabaseService.CreateConnection();
            dapper.Execute("""
                INSERT OR REPLACE INTO GameRecordRole (Uid, GameBiz, Nickname, Level, Region, RegionName, Cookie, HeadIcon)
                VALUES (@Uid, @GameBiz, @Nickname, @Level, @Region, @RegionName, @Cookie, @HeadIcon);
                """, role);
            _memoryCache.Set(key, true, TimeSpan.FromMinutes(5));
        }
    }



    /// <summary>
    /// 删除游戏角色，返回是否删除全部账号
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public bool DeleteGameRole(GameRecordRole role)
    {
        bool deletedUser = false;
        using var dapper = DatabaseService.CreateConnection();
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




    #region Spiral Abyss


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
        using var dapper = DatabaseService.CreateConnection();
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
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<SpiralAbyssInfo>("""
            SELECT Uid, ScheduleId, StartTime, EndTime, TotalBattleCount, TotalWinCount, MaxFloor, TotalStar FROM SpiralAbyssInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public SpiralAbyssInfo? GetSpiralAbyssInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM SpiralAbyssInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var info = JsonSerializer.Deserialize<SpiralAbyssInfo>(value);
        if (info != null)
        {
            info.Floors = info.Floors.Where(x => x.Index > 8).OrderByDescending(x => x.Index).ToList();
        }
        return info;
    }


    #endregion




    #region Traveler's Diary



    public async Task<TravelersDiarySummary> GetTravelersDiarySummaryAsync(GameRecordRole role, int month = 0)
    {
        var summary = await _gameRecordClient.GetTravelsDiarySummaryAsync(role, month);
        if (summary.MonthData is null)
        {
            return summary;
        }
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO TravelersDiaryMonthData
            (Uid, Year, Month, CurrentPrimogems, CurrentMora, LastPrimogems, LastMora, CurrentPrimogemsLevel, PrimogemsChangeRate, MoraChangeRate, PrimogemsGroupBy)
            VALUES (@Uid, @Year, @Month, @CurrentPrimogems, @CurrentMora, @LastPrimogems, @LastMora, @CurrentPrimogemsLevel, @PrimogemsChangeRate, @MoraChangeRate, @PrimogemsGroupBy);
            """, summary.MonthData);
        return summary;
    }


    public List<TravelersDiaryMonthData> GetTravelersDiaryMonthDataList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<TravelersDiaryMonthData>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<TravelersDiaryMonthData>("SELECT * FROM TravelersDiaryMonthData WHERE Uid = @Uid ORDER BY Year DESC, Month DESC;", new { role.Uid });
        return list.ToList();
    }



    public async Task<int> GetTravelersDiaryDetailAsync(GameRecordRole role, int month, int type, int limit = 100)
    {
        var detail = await _gameRecordClient.GetTravelsDiaryDetailAsync(role, month, type, limit);
        if (detail.List is null || detail.List.Count == 0)
        {
            return 0;
        }
        var list = detail.List;
        using var dapper = DatabaseService.CreateConnection();
        var existCount = dapper.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM TravelersDiaryAwardItem WHERE Uid=@Uid AND Year=@Year AND Month=@Month AND Type=@Type;", list.FirstOrDefault());
        if (existCount == list.Count)
        {
            return 0;
        }
        using var t = dapper.BeginTransaction();
        dapper.Execute($"DELETE FROM TravelersDiaryAwardItem WHERE Uid=@Uid AND Year=@Year AND Month=@Month AND Type=@Type;", list.FirstOrDefault(), t);
        dapper.Execute("""
                INSERT INTO TravelersDiaryAwardItem (Uid, Year, Month, Type, ActionId, ActionName, Time, Number)
                VALUES (@Uid, @Year, @Month, @Type, @ActionId, @ActionName, @Time, @Number);
                """, list, t);
        t.Commit();
        return list.Count - existCount;
    }



    public List<TravelersDiaryAwardItem> GetTravelersDiaryDetailItems(long uid, int year, int month, int type)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<TravelersDiaryAwardItem>("SELECT * FROM TravelersDiaryAwardItem WHERE Uid=@uid AND Year=@year AND Month=@month AND Type=@type ORDER BY Time;", new { uid, year, month, type }).ToList();
    }





    #endregion




    #region Imaginarium Theater



    /// <summary>
    /// 幻想真境剧诗
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task RefreshImaginariumTheaterInfoAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        var infos = await _gameRecordClient.GetImaginariumTheaterInfosAsync(role, cancellationToken);
        if (infos.Count == 0)
        {
            return;
        }
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        foreach (var info in infos)
        {
            var obj = new
            {
                info.Uid,
                info.ScheduleId,
                info.StartTime,
                info.EndTime,
                info.DifficultyId,
                info.MaxRoundId,
                info.Heraldry,
                info.MedalNum,
                Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
            };
            dapper.Execute("""
            INSERT OR REPLACE INTO ImaginariumTheaterInfo (Uid, ScheduleId, StartTime, EndTime, DifficultyId, MaxRoundId, Heraldry, MedalNum, Value)
            VALUES (@Uid, @ScheduleId, @StartTime, @EndTime, @DifficultyId, @MaxRoundId, @Heraldry, @MedalNum, @Value);
            """, obj, t);
        }
        t.Commit();
    }




    public List<ImaginariumTheaterInfo> GetImaginariumTheaterInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<ImaginariumTheaterInfo>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<ImaginariumTheaterInfo>("""
            SELECT Uid, ScheduleId, StartTime, EndTime, DifficultyId, MaxRoundId, Heraldry, MedalNum FROM ImaginariumTheaterInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public ImaginariumTheaterInfo? GetImaginariumTheaterInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM ImaginariumTheaterInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ImaginariumTheaterInfo>(value);
    }




    #endregion




    #region Simulated Universe



    public async Task<SimulatedUniverseInfo> GetSimulatedUniverseInfoAsync(GameRecordRole role, bool detail)
    {
        var info = await _gameRecordClient.GetSimulatedUniverseInfoAsync(role, detail);
        if (detail)
        {
            using var dapper = DatabaseService.CreateConnection();
            using var t = dapper.BeginTransaction();
            var obj = new
            {
                role.Uid,
                info.LastRecord.Basic.ScheduleId,
                info.LastRecord.Basic.FinishCount,
                info.LastRecord.Basic.ScheduleBegin,
                info.LastRecord.Basic.ScheduleEnd,
                info.LastRecord.HasData,
                Value = JsonSerializer.Serialize(info.LastRecord, AppConfig.JsonSerializerOptions),
            };
            dapper.Execute("""
                INSERT OR REPLACE INTO SimulatedUniverseRecord (Uid, ScheduleId, FinishCount, ScheduleBegin, ScheduleEnd, HasData, Value)
                VALUES (@Uid, @ScheduleId, @FinishCount, @ScheduleBegin, @ScheduleEnd, @HasData, @Value);
                """, obj, t);
            obj = new
            {
                role.Uid,
                info.CurrentRecord.Basic.ScheduleId,
                info.CurrentRecord.Basic.FinishCount,
                info.CurrentRecord.Basic.ScheduleBegin,
                info.CurrentRecord.Basic.ScheduleEnd,
                info.CurrentRecord.HasData,
                Value = JsonSerializer.Serialize(info.CurrentRecord, AppConfig.JsonSerializerOptions),
            };
            dapper.Execute("""
                INSERT OR REPLACE INTO SimulatedUniverseRecord (Uid, ScheduleId, FinishCount, ScheduleBegin, ScheduleEnd, HasData, Value)
                VALUES (@Uid, @ScheduleId, @FinishCount, @ScheduleBegin, @ScheduleEnd, @HasData, @Value);
                """, obj, t);
            t.Commit();
        }
        return info;
    }



    public List<SimulatedUniverseRecordBasic> GetSimulatedUniverseRecordBasics(GameRecordRole role)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<SimulatedUniverseRecordBasic>("""
            SELECT ScheduleId, FinishCount, ScheduleBegin, ScheduleEnd FROM SimulatedUniverseRecord WHERE Uid=@Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid }).ToList();
    }



    public SimulatedUniverseRecord? GetSimulatedUniverseRecord(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM SimulatedUniverseRecord WHERE Uid=@Uid AND ScheduleId=@scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<SimulatedUniverseRecord>(value);
    }



    #endregion




    #region Forgotten Hall



    public async Task<ForgottenHallInfo> RefreshForgottenHallInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var info = await _gameRecordClient.GetForgottenHallInfoAsync(role, schedule);
        var obj = new
        {
            info.Uid,
            info.ScheduleId,
            info.BeginTime,
            info.EndTime,
            info.StarNum,
            info.MaxFloor,
            info.BattleNum,
            info.HasData,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO ForgottenHallInfo (Uid, ScheduleId, BeginTime, EndTime, StarNum, MaxFloor, BattleNum, HasData, Value)
            VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @StarNum, @MaxFloor, @BattleNum, @HasData, @Value);
            """, obj);
        return info;
    }



    public List<ForgottenHallInfo> GetForgottenHallInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<ForgottenHallInfo>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<ForgottenHallInfo>("""
            SELECT Uid, ScheduleId, BeginTime, EndTime, StarNum, MaxFloor, BattleNum, HasData FROM ForgottenHallInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public ForgottenHallInfo? GetForgottenHallInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM ForgottenHallInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ForgottenHallInfo>(value);
    }



    #endregion




    #region Pure Fiction



    public async Task<PureFictionInfo> RefreshPureFictionInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var info = await _gameRecordClient.GetPureFictionInfoAsync(role, schedule);
        if (info.ScheduleId == 0)
        {
            return info;
        }
        var obj = new
        {
            info.Uid,
            info.ScheduleId,
            info.BeginTime,
            info.EndTime,
            info.StarNum,
            info.MaxFloor,
            info.BattleNum,
            info.HasData,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO PureFictionInfo (Uid, ScheduleId, BeginTime, EndTime, StarNum, MaxFloor, BattleNum, HasData, Value)
            VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @StarNum, @MaxFloor, @BattleNum, @HasData, @Value);
            """, obj);
        return info;
    }



    public List<PureFictionInfo> GetPureFictionInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<PureFictionInfo>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<PureFictionInfo>("""
            SELECT Uid, ScheduleId, BeginTime, EndTime, StarNum, MaxFloor, BattleNum, HasData FROM PureFictionInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public PureFictionInfo? GetPureFictionInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM PureFictionInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<PureFictionInfo>(value);
    }



    #endregion




    #region Apocalyptic Shadow



    public async Task<ApocalypticShadowInfo> RefreshApocalypticShadowInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var info = await _gameRecordClient.GetApocalypticShadowInfoAsync(role, schedule);
        if (info.ScheduleId == 0)
        {
            return info;
        }
        var obj = new
        {
            info.Uid,
            info.ScheduleId,
            info.BeginTime,
            info.EndTime,
            info.UpperBossIcon,
            info.LowerBossIcon,
            info.StarNum,
            info.MaxFloor,
            info.BattleNum,
            info.HasData,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO ApocalypticShadowInfo (Uid, ScheduleId, BeginTime, EndTime, UpperBossIcon, LowerBossIcon, StarNum, MaxFloor, BattleNum, HasData, Value)
            VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @UpperBossIcon, @LowerBossIcon, @StarNum, @MaxFloor, @BattleNum, @HasData, @Value);
            """, obj);
        return info;
    }



    public List<ApocalypticShadowInfo> GetApocalypticShadowInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<ApocalypticShadowInfo>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<ApocalypticShadowInfo>("""
            SELECT Uid, ScheduleId, BeginTime, EndTime, UpperBossIcon, LowerBossIcon, StarNum, MaxFloor, BattleNum, HasData FROM ApocalypticShadowInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public ApocalypticShadowInfo? GetApocalypticShadowInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM ApocalypticShadowInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ApocalypticShadowInfo>(value);
    }



    #endregion




    #region Trailblaze Calendar



    public async Task<TrailblazeCalendarSummary> GetTrailblazeCalendarSummaryAsync(GameRecordRole role, string month = "")
    {
        var summary = await _gameRecordClient.GetTrailblazeCalendarSummaryAsync(role, month);
        if (summary.MonthData is null)
        {
            return summary;
        }
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO TrailblazeCalendarMonthData (Uid, Month, CurrentHcoin, CurrentRailsPass, LastHcoin, LastRailsPass, HcoinRate, RailsRate, GroupBy)
            VALUES (@Uid, @Month, @CurrentHcoin, @CurrentRailsPass, @LastHcoin, @LastRailsPass, @HcoinRate, @RailsRate, @GroupBy);
            """, summary.MonthData);
        return summary;
    }


    public List<TrailblazeCalendarMonthData> GetTrailblazeCalendarMonthDataList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<TrailblazeCalendarMonthData>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<TrailblazeCalendarMonthData>("SELECT * FROM TrailblazeCalendarMonthData WHERE Uid = @Uid ORDER BY Month DESC;", new { role.Uid });
        return list.ToList();
    }



    public async Task<int> GetTrailblazeCalendarDetailAsync(GameRecordRole role, string month, int type)
    {
        int total = (await _gameRecordClient.GetTrailblazeCalendarDetailByPageAsync(role, month, type, 1, 1)).Total;
        if (total == 0)
        {
            return 0;
        }
        using var dapper = DatabaseService.CreateConnection();
        var existCount = dapper.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM TrailblazeCalendarDetailItem WHERE Uid = @Uid AND Month = @month AND Type = @type;", new { role.Uid, month, type });
        if (existCount == total)
        {
            return 0;
        }
        var detail = await _gameRecordClient.GetTrailblazeCalendarDetailAsync(role, month, type);
        var list = detail.List;
        using var t = dapper.BeginTransaction();
        dapper.Execute($"DELETE FROM TrailblazeCalendarDetailItem WHERE Uid = @Uid AND Month = @Month AND Type = @Type;", list.FirstOrDefault(), t);
        dapper.Execute("""
                INSERT INTO TrailblazeCalendarDetailItem (Uid, Month, Type, Action, ActionName, Time, Number)
                VALUES (@Uid, @Month, @Type, @Action, @ActionName, @Time, @Number);
                """, list, t);
        t.Commit();
        return total - existCount;
    }



    public List<TrailblazeCalendarDetailItem> GetTrailblazeCalendarDetailItems(long uid, string month, int type)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<TrailblazeCalendarDetailItem>("SELECT * FROM TrailblazeCalendarDetailItem WHERE Uid=@uid AND Month=@month AND Type=@type ORDER BY Time;", new { uid, month, type }).ToList();
    }




    #endregion




    #region Inter Knot Report



    public async Task<InterKnotReportSummary> GetInterKnotReportSummaryAsync(GameRecordRole role, string month = "")
    {
        var summary = await _gameRecordClient.GetInterKnotReportSummaryAsync(role, month);
        using var dapper = DatabaseService.CreateConnection();
        var obj = new
        {
            summary.Uid,
            summary.DataMonth,
            Value = JsonSerializer.Serialize(summary),
        };
        dapper.Execute("""
            INSERT OR REPLACE INTO InterKnotReportSummary (Uid, DataMonth, Value) VALUES (@Uid, @DataMonth, @Value);
            """, obj);
        return summary;
    }


    public List<InterKnotReportSummary> GetInterKnotReportSummaryList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<InterKnotReportSummary>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<InterKnotReportSummary>("SELECT * FROM InterKnotReportSummary WHERE Uid = @Uid ORDER BY DataMonth DESC;", new { role.Uid });
        return list.ToList();
    }


    public InterKnotReportSummary? GetInterKnotReportSummary(InterKnotReportSummary summary)
    {
        if (summary is null)
        {
            return null;
        }
        using var dapper = DatabaseService.CreateConnection();
        string? value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM InterKnotReportSummary WHERE Uid = @Uid AND DataMonth = @DataMonth LIMIT 1;", summary);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<InterKnotReportSummary>(value);
    }



    public async Task<int> GetInterKnotReportDetailAsync(GameRecordRole role, string month, string type)
    {
        int total = (await _gameRecordClient.GetInterKnotReportDetailByPageAsync(role, month, type, 1, 1)).Total;
        if (total == 0)
        {
            return 0;
        }
        using var dapper = DatabaseService.CreateConnection();
        var existCount = dapper.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM InterKnotReportDetailItem WHERE Uid = @Uid AND DataMonth = @month AND DataType = @type;", new { role.Uid, month, type });
        if (existCount == total)
        {
            return 0;
        }
        var detail = await _gameRecordClient.GetInterKnotReportDetailAsync(role, month, type);
        var list = detail.List;
        using var t = dapper.BeginTransaction();
        dapper.Execute($"DELETE FROM InterKnotReportDetailItem WHERE Uid = @Uid AND DataMonth = @DataMonth AND DataType = @DataType;", list.FirstOrDefault(), t);
        dapper.Execute("""
                INSERT OR REPLACE INTO InterKnotReportDetailItem (Uid, Id, DataMonth, DataType, Action, Time, Number)
                VALUES (@Uid, @Id, @DataMonth, @DataType, @Action, @Time, @Number);
                """, list, t);
        t.Commit();
        return total - existCount;
    }



    public List<InterKnotReportDetailItem> GetInterKnotReportDetailItems(long uid, string month, string type)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<InterKnotReportDetailItem>("SELECT * FROM InterKnotReportDetailItem WHERE Uid=@uid AND DataMonth=@month AND DataType=@type ORDER BY Time;", new { uid, month, type }).ToList();
    }




    #endregion




    #region Shiyu Defense



    public async Task<ShiyuDefenseInfo> RefreshShiyuDefenseInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var info = await _gameRecordClient.GetShiyuDefenseInfoAsync(role, schedule);
        if (!info.HasData)
        {
            return info;
        }
        var obj = new
        {
            role.Uid,
            info.ScheduleId,
            info.BeginTime,
            info.EndTime,
            info.HasData,
            info.MaxRating,
            info.MaxRatingTimes,
            info.MaxLayer,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO ShiyuDefenseInfo (Uid, ScheduleId, BeginTime, EndTime, HasData, MaxRating, MaxRatingTimes, MaxLayer, Value)
            VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @HasData, @MaxRating, @MaxRatingTimes, @MaxLayer, @Value);
            """, obj);
        return info;
    }



    public List<ShiyuDefenseInfo> GetShiyuDefenseInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<ShiyuDefenseInfo>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<ShiyuDefenseInfo>("""
            SELECT Uid, ScheduleId, BeginTime, EndTime, HasData, MaxRating, MaxRatingTimes, MaxLayer FROM ShiyuDefenseInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public ShiyuDefenseInfo? GetShiyuDefenseInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM ShiyuDefenseInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ShiyuDefenseInfo>(value);
    }



    #endregion




    #region Deadly Assault



    public async Task<DeadlyAssaultInfo> RefreshDeadlyAssaultInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var info = await _gameRecordClient.GetDeadlyAssaultInfoAsync(role, schedule);
        if (!info.HasData)
        {
            return info;
        }
        var obj = new
        {
            role.Uid,
            info.ZoneId,
            info.StartTime,
            info.EndTime,
            info.HasData,
            info.RankPercent,
            info.TotalScore,
            info.TotalStar,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO DeadlyAssaultInfo (Uid, ZoneId, StartTime, EndTime, HasData, RankPercent, TotalScore, TotalStar, Value)
            VALUES (@Uid, @ZoneId, @StartTime, @EndTime, @HasData, @RankPercent, @TotalScore, @TotalStar, @Value);
            """, obj);
        return info;
    }



    public List<DeadlyAssaultInfo> GetDeadlyAssaultInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<DeadlyAssaultInfo>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<DeadlyAssaultInfo>("""
            SELECT Uid, ZoneId, StartTime, EndTime, HasData, RankPercent, TotalScore, TotalStar FROM DeadlyAssaultInfo WHERE Uid = @Uid ORDER BY ZoneId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public DeadlyAssaultInfo? GetDeadlyAssaultInfo(GameRecordRole role, int zoneId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM DeadlyAssaultInfo WHERE Uid = @Uid And ZoneId = @zoneId LIMIT 1;
            """, new { role.Uid, zoneId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<DeadlyAssaultInfo>(value);
    }



    #endregion




    #region Daily Note



    public async Task<BH3DailyNote> GetBH3DailyNoteAsync(GameRecordRole role, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        string key = $"{nameof(BH3DailyNote)}_{role.Region}_{role.Uid}";
        if (forceUpdate || !_memoryCache.TryGetValue(key, out BH3DailyNote? note))
        {
            note = await _gameRecordClient.GetBH3DailyNoteAsync(role, cancellationToken);
            _memoryCache.Set(key, note, TimeSpan.FromMinutes(5));
        }
        return note!;
    }



    public async Task<GenshinDailyNote> GetGenshinDailyNoteAsync(GameRecordRole role, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        string key = $"{nameof(GenshinDailyNote)}_{role.Region}_{role.Uid}";
        if (forceUpdate || !_memoryCache.TryGetValue(key, out GenshinDailyNote? note))
        {
            note = await _gameRecordClient.GetGenshinDailyNoteAsync(role, cancellationToken);
            _memoryCache.Set(key, note, TimeSpan.FromMinutes(5));
        }
        return note!;
    }



    public async Task<StarRailDailyNote> GetStarRailDailyNoteAsync(GameRecordRole role, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        string key = $"{nameof(StarRailDailyNote)}_{role.Region}_{role.Uid}";
        if (forceUpdate || !_memoryCache.TryGetValue(key, out StarRailDailyNote? note))
        {
            note = await _gameRecordClient.GetStarRailDailyNoteAsync(role, cancellationToken);
            _memoryCache.Set(key, note, TimeSpan.FromMinutes(5));
        }
        return note!;
    }


    public async Task<ZZZDailyNote> GetZZZDailyNoteAsync(GameRecordRole role, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        string key = $"{nameof(ZZZDailyNote)}_{role.Region}_{role.Uid}";
        if (forceUpdate || !_memoryCache.TryGetValue(key, out ZZZDailyNote? note))
        {
            note = await _gameRecordClient.GetZZZDailyNoteAsync(role, cancellationToken);
            _memoryCache.Set(key, note, TimeSpan.FromMinutes(5));
        }
        return note!;
    }




    #endregion



}
