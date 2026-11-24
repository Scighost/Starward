using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.StarRail;
using Starward.Core.GameRecord.StarRail.ApocalypticShadow;
using Starward.Core.GameRecord.StarRail.ChallengePeak;
using Starward.Core.GameRecord.StarRail.DailyNote;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
using Starward.Core.GameRecord.StarRail.PureFiction;
using Starward.Core.GameRecord.StarRail.SimulatedUniverse;
using Starward.Core.GameRecord.StarRail.TrailblazeCalendar;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord.StarRail;

internal class GameRecordStarRailService : GameRecordServiceBase
{


    private readonly ILogger<GameRecordStarRailService> _logger;

    private readonly HyperionStarRailClient _hyperionStarRailClient;

    private readonly HoyolabStarRailClient _hoyolabStarRailClient;



    public GameRecordStarRailService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GameRecordStarRailService>>();
        _hyperionStarRailClient = serviceProvider.GetRequiredService<HyperionStarRailClient>();
        _hoyolabStarRailClient = serviceProvider.GetRequiredService<HoyolabStarRailClient>();
    }



    private async Task<IGameRecordStarRailClient> GetClientAsync(GameBiz gameBiz, CancellationToken cancellation = default)
    {
        ServerRegion region = GameBizToServerRegion(gameBiz);
        IGameRecordStarRailClient client = region switch
        {
            ServerRegion.miHoYo => _hyperionStarRailClient,
            ServerRegion.HoYoverse => _hoyolabStarRailClient,
            _ => throw new NotSupportedException($"ServerRegion '{region}' is not supported.")
        };
        client.DeviceInfo = await GetDeviceInfoAsync(region, cancellation);
        client.Language = CultureInfo.CurrentUICulture.Name;
        return client;
    }



    /// <summary>
    /// 获取游戏账号信息
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="cookie"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<List<GameRecordRole>> GetGameRolesAsync(GameBiz gameBiz, string cookie, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(gameBiz, cancellationToken);
        return await client.GetGameRolesAsync(gameBiz, cookie, cancellationToken);
    }


    /// <summary>
    /// 获取游戏账号头像
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<string> GetGameRoleHeadIconAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        return await client.GetGameRoleHeadIconAsync(role, cancellationToken);
    }


    /// <summary>
    /// 星穹铁道实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="forceUpdate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<StarRailDailyNote> GetStarRailDailyNoteAsync(GameRecordRole role, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        string key = $"{nameof(StarRailDailyNote)}_{role.Region}_{role.Uid}";
        if (forceUpdate || !_cache.TryGetValue(key, out StarRailDailyNote? note))
        {
            note = await client.GetStarRailDailyNoteAsync(role, cancellationToken);
            _cache.Set(key, note, TimeSpan.FromMinutes(5));
        }
        return note!;
    }



    #region 模拟宇宙



    public async Task<SimulatedUniverseInfo> GetSimulatedUniverseInfoAsync(GameRecordRole role, bool detail, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var info = await client.GetSimulatedUniverseInfoAsync(role, detail, cancellationToken);
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
                INSERT OR REPLACE INTO StarRailSimulatedUniverseRecord (Uid, ScheduleId, FinishCount, ScheduleBegin, ScheduleEnd, HasData, Value)
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
                INSERT OR REPLACE INTO StarRailSimulatedUniverseRecord (Uid, ScheduleId, FinishCount, ScheduleBegin, ScheduleEnd, HasData, Value)
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
            SELECT ScheduleId, FinishCount, ScheduleBegin, ScheduleEnd FROM StarRailSimulatedUniverseRecord WHERE Uid=@Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid }).ToList();
    }



    public SimulatedUniverseRecord? GetSimulatedUniverseRecord(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM StarRailSimulatedUniverseRecord WHERE Uid=@Uid AND ScheduleId=@scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<SimulatedUniverseRecord>(value);
    }



    #endregion




    #region 忘却之庭



    public async Task<ForgottenHallInfo> RefreshForgottenHallInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var info = await client.GetForgottenHallInfoAsync(role, schedule, cancellationToken);
        var obj = new
        {
            info.Uid,
            info.ScheduleId,
            info.BeginTime,
            info.EndTime,
            info.StarNum,
            info.ExtraStarNum,
            info.MaxFloor,
            info.BattleNum,
            info.HasData,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO StarRailForgottenHallInfo (Uid, ScheduleId, BeginTime, EndTime, StarNum, ExtraStarNum, MaxFloor, BattleNum, HasData, Value)
            VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @StarNum, @ExtraStarNum, @MaxFloor, @BattleNum, @HasData, @Value);
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
            SELECT Uid, ScheduleId, BeginTime, EndTime, StarNum, ExtraStarNum, MaxFloor, BattleNum, HasData FROM StarRailForgottenHallInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public ForgottenHallInfo? GetForgottenHallInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM StarRailForgottenHallInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ForgottenHallInfo>(value);
    }



    #endregion




    #region 虚构叙事



    public async Task<PureFictionInfo> RefreshPureFictionInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var info = await client.GetPureFictionInfoAsync(role, schedule, cancellationToken);
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
            info.ExtraStarNum,
            info.MaxFloor,
            info.BattleNum,
            info.HasData,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO StarRailPureFictionInfo (Uid, ScheduleId, BeginTime, EndTime, StarNum, ExtraStarNum, MaxFloor, BattleNum, HasData, Value)
            VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @StarNum, @ExtraStarNum, @MaxFloor, @BattleNum, @HasData, @Value);
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
            SELECT Uid, ScheduleId, BeginTime, EndTime, StarNum, ExtraStarNum, MaxFloor, BattleNum, HasData FROM StarRailPureFictionInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public PureFictionInfo? GetPureFictionInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM StarRailPureFictionInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<PureFictionInfo>(value);
    }



    #endregion




    #region 末日幻影



    public async Task<ApocalypticShadowInfo> RefreshApocalypticShadowInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var info = await client.GetApocalypticShadowInfoAsync(role, schedule, cancellationToken);
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
            info.TierceBossIcon,
            info.StarNum,
            info.ExtraStarNum,
            info.MaxFloor,
            info.BattleNum,
            info.HasData,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO StarRailApocalypticShadowInfo (Uid, ScheduleId, BeginTime, EndTime, UpperBossIcon, LowerBossIcon, TierceBossIcon, StarNum, ExtraStarNum, MaxFloor, BattleNum, HasData, Value)
            VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @UpperBossIcon, @LowerBossIcon, @TierceBossIcon, @StarNum, @ExtraStarNum, @MaxFloor, @BattleNum, @HasData, @Value);
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
            SELECT Uid, ScheduleId, BeginTime, EndTime, UpperBossIcon, LowerBossIcon, TierceBossIcon, StarNum, ExtraStarNum, MaxFloor, BattleNum, HasData FROM StarRailApocalypticShadowInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public ApocalypticShadowInfo? GetApocalypticShadowInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM StarRailApocalypticShadowInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<ApocalypticShadowInfo>(value);
    }



    #endregion




    #region 开拓月历



    public async Task<TrailblazeCalendarSummary> GetTrailblazeCalendarSummaryAsync(GameRecordRole role, string month = "", CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var summary = await client.GetTrailblazeCalendarSummaryAsync(role, month, cancellationToken);
        if (summary.MonthData is null)
        {
            return summary;
        }
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO StarRailTrailblazeCalendarMonthData (Uid, Month, CurrentHcoin, CurrentRailsPass, LastHcoin, LastRailsPass, HcoinRate, RailsRate, GroupBy)
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
        var list = dapper.Query<TrailblazeCalendarMonthData>("SELECT * FROM StarRailTrailblazeCalendarMonthData WHERE Uid = @Uid ORDER BY Month DESC;", new { role.Uid });
        return list.ToList();
    }



    public async Task<int> GetTrailblazeCalendarDetailAsync(GameRecordRole role, string month, int type, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        int total = (await client.GetTrailblazeCalendarDetailByPageAsync(role, month, type, 1, 1, cancellationToken)).Total;
        if (total == 0)
        {
            return 0;
        }
        using var dapper = DatabaseService.CreateConnection();
        var existCount = dapper.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM StarRailTrailblazeCalendarDetailItem WHERE Uid = @Uid AND Month = @month AND Type = @type;", new { role.Uid, month, type });
        if (existCount == total)
        {
            return 0;
        }
        var detail = await client.GetTrailblazeCalendarDetailAsync(role, month, type, 100, cancellationToken);
        var list = detail.List;
        using var t = dapper.BeginTransaction();
        dapper.Execute($"DELETE FROM StarRailTrailblazeCalendarDetailItem WHERE Uid = @Uid AND Month = @Month AND Type = @Type;", list.FirstOrDefault(), t);
        dapper.Execute("""
                INSERT INTO StarRailTrailblazeCalendarDetailItem (Uid, Month, Type, Action, ActionName, Time, Number)
                VALUES (@Uid, @Month, @Type, @Action, @ActionName, @Time, @Number);
                """, list, t);
        t.Commit();
        return total - existCount;
    }



    public List<TrailblazeCalendarDetailItem> GetTrailblazeCalendarDetailItems(long uid, string month, int type)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<TrailblazeCalendarDetailItem>("SELECT * FROM StarRailTrailblazeCalendarDetailItem WHERE Uid=@uid AND Month=@month AND Type=@type ORDER BY Time;", new { uid, month, type }).ToList();
    }




    #endregion




    #region 异相仲裁




    public List<ChallengePeakData> GetStarRailChallengePeakDataList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<ChallengePeakData>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<ChallengePeakData>("""
            SELECT Uid, GroupId, GameVersion, BossStars, MobStars, BossIcon FROM StarRailChallengePeakData WHERE Uid = @Uid ORDER BY GroupId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public async Task RefreshStarRailChallengePeakDataAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        using var dapper = DatabaseService.CreateConnection();

        var client = await GetClientAsync(role.GameBiz, cancellationToken);

        var data = await client.GetStarRailChallengePeakDataAsync(role, 1, cancellationToken);
        if (data.ChallengePeakRecords?.Count == 1)
        {
            var record = data.ChallengePeakRecords[0];
            var obj = new
            {
                role.Uid,
                record.Group.GroupId,
                record.Group.GameVersion,
                record.BossStars,
                record.MobStars,
                BossIcon = record.BossInfo.Icon,
                Value = JsonSerializer.Serialize(data, AppConfig.JsonSerializerOptions),
            };
            dapper.Execute("""
                INSERT OR REPLACE INTO StarRailChallengePeakData (Uid, GroupId, GameVersion, BossStars, MobStars, BossIcon, Value)
                VALUES (@Uid, @GroupId, @GameVersion, @BossStars, @MobStars, @BossIcon, @Value);
                """, obj);
        }

        data = await client.GetStarRailChallengePeakDataAsync(role, 3, cancellationToken);
        foreach (var record in data.ChallengePeakRecords.ToList())
        {
            data.ChallengePeakRecords.Clear();
            var queryData = dapper.QueryFirstOrDefault<ChallengePeakData>("""
                SELECT BossStars, MobStars FROM StarRailChallengePeakData WHERE Uid = @Uid AND GroupId = @GroupId LIMIT 1;
                """, new { role.Uid, record.Group.GroupId });
            if (queryData is null)
            {
                data.ChallengePeakRecords.Add(record);
                data.ChallengePeakBestRecordBrief = new ChallengePeakBestRecordBrief
                {
                    BossStars = record.BossStars,
                    MobStars = record.MobStars,
                };
                var obj = new
                {
                    role.Uid,
                    record.Group.GroupId,
                    record.Group.GameVersion,
                    record.BossStars,
                    record.MobStars,
                    BossIcon = record.BossInfo.Icon,
                    Value = JsonSerializer.Serialize(data, AppConfig.JsonSerializerOptions),
                };
                dapper.Execute("""
                    INSERT OR REPLACE INTO StarRailChallengePeakData (Uid, GroupId, GameVersion, BossStars, MobStars, BossIcon, Value)
                    VALUES (@Uid, @GroupId, @GameVersion, @BossStars, @MobStars, @BossIcon, @Value);
                    """, obj);
            }
            else if (record.BossStars > queryData.BossStars || record.MobStars > queryData.MobStars)
            {
                var queryText = dapper.QueryFirstOrDefault<string>("""
                    SELECT Value FROM StarRailChallengePeakData WHERE Uid = @Uid AND GroupId = @GroupId LIMIT 1;
                    """, new { role.Uid, record.Group.GroupId });
                if (!string.IsNullOrWhiteSpace(queryText))
                {
                    var queryValue = JsonSerializer.Deserialize<ChallengePeakData>(queryText);
                    if (queryValue is not null)
                    {
                        queryValue.ChallengePeakRecords.Clear();
                        queryValue.ChallengePeakRecords.Add(record);
                        queryValue.ChallengePeakBestRecordBrief ??= new();
                        queryValue.ChallengePeakBestRecordBrief.BossStars = record.BossStars;
                        queryValue.ChallengePeakBestRecordBrief.MobStars = record.MobStars;

                        var obj = new
                        {
                            role.Uid,
                            record.Group.GroupId,
                            record.Group.GameVersion,
                            record.BossStars,
                            record.MobStars,
                            BossIcon = record.BossInfo.Icon,
                            Value = JsonSerializer.Serialize(queryValue),
                        };
                        dapper.Execute("""
                            INSERT OR REPLACE INTO StarRailChallengePeakData (Uid, GroupId, GameVersion, BossStars, MobStars, BossIcon, Value)
                            VALUES (@Uid, @GroupId, @GameVersion, @BossStars, @MobStars, @BossIcon, @Value);
                            """, obj);
                    }
                }
            }
        }
    }



    public ChallengePeakData? GetStarRailChallengePeakData(GameRecordRole role, int groupId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var queryText = dapper.QueryFirstOrDefault<string>("""
                    SELECT Value FROM StarRailChallengePeakData WHERE Uid = @Uid AND GroupId = @groupId LIMIT 1;
                    """, new { role.Uid, groupId });
        if (!string.IsNullOrWhiteSpace(queryText))
        {
            return JsonSerializer.Deserialize<ChallengePeakData>(queryText);
        }
        return null;
    }




    #endregion


}
