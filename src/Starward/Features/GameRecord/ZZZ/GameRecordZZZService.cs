using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.ZZZ;
using Starward.Core.GameRecord.ZZZ.DailyNote;
using Starward.Core.GameRecord.ZZZ.DeadlyAssault;
using Starward.Core.GameRecord.ZZZ.GachaRecord;
using Starward.Core.GameRecord.ZZZ.InterKnotReport;
using Starward.Core.GameRecord.ZZZ.ShiyuDefense;
using Starward.Features.Database;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord.ZZZ;

internal class GameRecordZZZService : GameRecordServiceBase
{


    private readonly ILogger<GameRecordZZZService> _logger;

    private readonly HyperionZZZClient _hyperionZZZClient;

    private readonly HoyolabZZZClient _hoyolabZZZClient;



    public GameRecordZZZService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GameRecordZZZService>>();
        _hyperionZZZClient = serviceProvider.GetRequiredService<HyperionZZZClient>();
        _hoyolabZZZClient = serviceProvider.GetRequiredService<HoyolabZZZClient>();
    }



    private async Task<IGameRecordZZZClient> GetClientAsync(GameBiz gameBiz, CancellationToken cancellation = default)
    {
        ServerRegion region = GameBizToServerRegion(gameBiz);
        IGameRecordZZZClient client = region switch
        {
            ServerRegion.miHoYo => _hyperionZZZClient,
            ServerRegion.HoYoverse => _hoyolabZZZClient,
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
    /// 绝区零实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="forceUpdate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<ZZZDailyNote> GetZZZDailyNoteAsync(GameRecordRole role, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        string key = $"{nameof(ZZZDailyNote)}_{role.Region}_{role.Uid}";
        if (forceUpdate || !_cache.TryGetValue(key, out ZZZDailyNote? note))
        {
            note = await client.GetZZZDailyNoteAsync(role, cancellationToken);
            _cache.Set(key, note, TimeSpan.FromMinutes(5));
        }
        return note!;
    }



    #region 绳网月报



    public async Task<InterKnotReportSummary> GetInterKnotReportSummaryAsync(GameRecordRole role, string month = "", CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var summary = await client.GetInterKnotReportSummaryAsync(role, month, cancellationToken);
        using var dapper = DatabaseService.CreateConnection();
        var obj = new
        {
            summary.Uid,
            summary.DataMonth,
            Value = JsonSerializer.Serialize(summary),
        };
        dapper.Execute("""
            INSERT OR REPLACE INTO ZZZInterKnotReportSummary (Uid, DataMonth, Value) VALUES (@Uid, @DataMonth, @Value);
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
        var values = dapper.Query<string>("SELECT Value FROM ZZZInterKnotReportSummary WHERE Uid = @Uid ORDER BY DataMonth DESC;", new { role.Uid });
        return values.Select(v => JsonSerializer.Deserialize<InterKnotReportSummary>(v)!).ToList();
    }


    public InterKnotReportSummary? GetInterKnotReportSummary(InterKnotReportSummary summary)
    {
        if (summary is null)
        {
            return null;
        }
        using var dapper = DatabaseService.CreateConnection();
        string? value = dapper.QueryFirstOrDefault<string>("SELECT Value FROM ZZZInterKnotReportSummary WHERE Uid = @Uid AND DataMonth = @DataMonth LIMIT 1;", summary);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        return JsonSerializer.Deserialize<InterKnotReportSummary>(value);
    }



    public async Task<int> GetInterKnotReportDetailAsync(GameRecordRole role, string month, string type, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        int total = (await client.GetInterKnotReportDetailByPageAsync(role, month, type, 1, 1, cancellationToken)).Total;
        if (total == 0)
        {
            return 0;
        }
        using var dapper = DatabaseService.CreateConnection();
        var existCount = dapper.QuerySingleOrDefault<int>("SELECT COUNT(*) FROM ZZZInterKnotReportDetailItem WHERE Uid = @Uid AND DataMonth = @month AND DataType = @type;", new { role.Uid, month, type });
        if (existCount == total)
        {
            return 0;
        }
        var detail = await client.GetInterKnotReportDetailAsync(role, month, type, 100, cancellationToken);
        var list = detail.List;
        using var t = dapper.BeginTransaction();
        dapper.Execute($"DELETE FROM ZZZInterKnotReportDetailItem WHERE Uid = @Uid AND DataMonth = @DataMonth AND DataType = @DataType;", list.FirstOrDefault(), t);
        dapper.Execute("""
                INSERT OR REPLACE INTO ZZZInterKnotReportDetailItem (Uid, Id, DataMonth, DataType, Action, Time, Number)
                VALUES (@Uid, @Id, @DataMonth, @DataType, @Action, @Time, @Number);
                """, list, t);
        t.Commit();
        return total - existCount;
    }



    public List<InterKnotReportDetailItem> GetInterKnotReportDetailItems(long uid, string month, string type)
    {
        using var dapper = DatabaseService.CreateConnection();
        return dapper.Query<InterKnotReportDetailItem>("SELECT * FROM ZZZInterKnotReportDetailItem WHERE Uid=@uid AND DataMonth=@month AND DataType=@type ORDER BY Time;", new { uid, month, type }).ToList();
    }



    public async Task<ZZZGachaRecordData> GetZZZGachaRecordAsync(GameRecordRole role, int gachaType, long? endId = null, string? language = null, CancellationToken cancellationToken = default)
    {
        if (role is null)
        {
            throw new ArgumentNullException(nameof(role));
        }
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        if (!string.IsNullOrWhiteSpace(language))
        {
            client.Language = language;
        }
        return await client.GetZZZGachaRecordAsync(role, gachaType, endId, language, cancellationToken);
    }




    #endregion




    #region 式舆防卫战



    public async Task<ShiyuDefenseWrapper> RefreshShiyuDefenseInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var wrapper = await client.GetShiyuDefenseInfoAsync(role, schedule, cancellationToken);
        if (wrapper.HadalVer is "v1" && wrapper.InfoV1 is not null)
        {
            var info = wrapper.InfoV1;
            if (info.HasData)
            {
                var obj = new
                {
                    role.Uid,
                    info.ScheduleId,
                    info.BeginTime,
                    info.EndTime,
                    info.Version,
                    info.HasData,
                    info.MaxRating,
                    info.MaxRatingTimes,
                    info.MaxLayer,
                    Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
                };
                using var dapper = DatabaseService.CreateConnection();
                dapper.Execute("""
                    INSERT OR REPLACE INTO ZZZShiyuDefenseInfo (Uid, ScheduleId, BeginTime, EndTime, Version, HasData, MaxRating, MaxRatingTimes, MaxLayer, Value)
                    VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @Version, @HasData, @MaxRating, @MaxRatingTimes, @MaxLayer, @Value);
                    """, obj);
            }
        }
        else if (wrapper.HadalVer is "v2" && wrapper.InfoV2 is not null)
        {
            if (wrapper.InfoV2.Brief is not null)
            {
                var info = wrapper.InfoV2;
                if (info.PassFifthFloor)
                {
                    var obj = new
                    {
                        role.Uid,
                        info.ScheduleId,
                        info.BeginTime,
                        info.EndTime,
                        info.Version,
                        info.HasData,
                        info.MaxRating,
                        info.V2Score,
                        Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
                    };
                    using var dapper = DatabaseService.CreateConnection();
                    dapper.Execute("""
                        INSERT OR REPLACE INTO ZZZShiyuDefenseInfo (Uid, ScheduleId, BeginTime, EndTime, Version, HasData, MaxRating, V2Score, Value)
                        VALUES (@Uid, @ScheduleId, @BeginTime, @EndTime, @Version, @HasData, @MaxRating, @V2Score, @Value);
                        """, obj);
                }
            }
        }
        return wrapper;
    }



    public List<ShiyuDefenseInfo> GetShiyuDefenseInfoList(GameRecordRole role)
    {
        if (role is null)
        {
            return new List<ShiyuDefenseInfo>();
        }
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<ShiyuDefenseInfo>("""
            SELECT Uid, ScheduleId, BeginTime, EndTime, Version, HasData, MaxRating, MaxRatingTimes, MaxLayer, V2Score FROM ZZZShiyuDefenseInfo WHERE Uid = @Uid ORDER BY ScheduleId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public ShiyuDefenseInfoBase? GetShiyuDefenseInfo(GameRecordRole role, int scheduleId)
    {
        using var dapper = DatabaseService.CreateConnection();
        (string version, string value) = dapper.QueryFirstOrDefault<(string Version, string Value)>("""
            SELECT Version, Value FROM ZZZShiyuDefenseInfo WHERE Uid = @Uid And ScheduleId = @scheduleId LIMIT 1;
            """, new { role.Uid, scheduleId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        if (version is "v1")
        {
            return JsonSerializer.Deserialize<ShiyuDefenseInfo>(value);
        }
        else if (version is "v2")
        {
            return JsonSerializer.Deserialize<ShiyuDefenseInfoV2>(value);
        }
        return null;
    }



    #endregion




    #region 危局强袭战



    public async Task<DeadlyAssaultInfo> RefreshDeadlyAssaultInfoAsync(GameRecordRole role, int schedule, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        var info = await client.GetDeadlyAssaultInfoAsync(role, schedule, cancellationToken);
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
            info.HasHard,
            HardTotalScore = info.HardNodes?.Sum(x => x.Score) ?? 0,
            HardTotalStar = info.HardNodes?.Sum(x => x.Star) ?? 0,
            Value = JsonSerializer.Serialize(info, AppConfig.JsonSerializerOptions),
        };
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("""
            INSERT OR REPLACE INTO ZZZDeadlyAssaultInfo (Uid, ZoneId, StartTime, EndTime, HasData, RankPercent, TotalScore, TotalStar, HasHard, HardTotalScore, HardTotalStar, Value)
            VALUES (@Uid, @ZoneId, @StartTime, @EndTime, @HasData, @RankPercent, @TotalScore, @TotalStar, @HasHard, @HardTotalScore, @HardTotalStar, @Value);
            """, obj);
        info.HardTotalScore = info.HardNodes?.Sum(x => x.Score) ?? 0;
        info.HardTotalStar = info.HardNodes?.Sum(x => x.Star) ?? 0;
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
            SELECT Uid, ZoneId, StartTime, EndTime, HasData, RankPercent, TotalScore, TotalStar, HasHard, HardTotalScore, HardTotalStar FROM ZZZDeadlyAssaultInfo WHERE Uid = @Uid ORDER BY ZoneId DESC;
            """, new { role.Uid });
        return list.ToList();
    }



    public DeadlyAssaultInfo? GetDeadlyAssaultInfo(GameRecordRole role, int zoneId)
    {
        using var dapper = DatabaseService.CreateConnection();
        var value = dapper.QueryFirstOrDefault<string>("""
            SELECT Value FROM ZZZDeadlyAssaultInfo WHERE Uid = @Uid And ZoneId = @zoneId LIMIT 1;
            """, new { role.Uid, zoneId });
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        var info = JsonSerializer.Deserialize<DeadlyAssaultInfo>(value);
        if (info is not null)
        {
            info.HardTotalScore = info.HardNodes?.Sum(x => x.Score) ?? 0;
            info.HardTotalStar = info.HardNodes?.Sum(x => x.Star) ?? 0;
        }
        return info;
    }



    #endregion


}
