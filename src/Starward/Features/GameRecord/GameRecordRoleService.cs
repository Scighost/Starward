using Dapper;
using Microsoft.Extensions.DependencyInjection;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.Passport;
using Starward.Features.Database;
using Starward.Features.GameRecord.BH3;
using Starward.Features.GameRecord.Genshin;
using Starward.Features.GameRecord.StarRail;
using Starward.Features.GameRecord.ZZZ;
using Starward.Features.Passport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord;

internal class GameRecordRoleService : GameRecordServiceBase
{

    private readonly PassportService _passportService;

    private readonly GameRecordBH3Service _bh3Service;

    private readonly GameRecordGenshinService _genshinService;

    private readonly GameRecordStarRailService _starRailService;

    private readonly GameRecordZZZService _zzzService;



    public GameRecordRoleService(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _passportService = serviceProvider.GetRequiredService<PassportService>();
        _bh3Service = serviceProvider.GetRequiredService<GameRecordBH3Service>();
        _genshinService = serviceProvider.GetRequiredService<GameRecordGenshinService>();
        _starRailService = serviceProvider.GetRequiredService<GameRecordStarRailService>();
        _zzzService = serviceProvider.GetRequiredService<GameRecordZZZService>();
    }



    /// <summary>
    /// 获取游戏账号
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameRecordRole>> GetGameRolesAsync(GameBiz gameBiz, string cookie, CancellationToken cancellationToken = default)
    {
        return gameBiz.Game switch
        {
            GameBiz.bh3 => _bh3Service.GetGameRolesAsync(gameBiz, cookie, cancellationToken),
            GameBiz.hk4e => _genshinService.GetGameRolesAsync(gameBiz, cookie, cancellationToken),
            GameBiz.hkrpg => _starRailService.GetGameRolesAsync(gameBiz, cookie, cancellationToken),
            GameBiz.nap => _zzzService.GetGameRolesAsync(gameBiz, cookie, cancellationToken),
            _ => throw new NotSupportedException($"GameBiz '{gameBiz}' is not supported."),
        };
    }


    /// <summary>
    /// 更新单个游戏角色，失败时通过通行证刷新cookie
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task UpdateGameRoleAsync(GameRecordRole role, CancellationToken cancellationToken = default)
    {
        try
        {
            var list = await GetGameRolesAsync(role.GameBiz, role.Cookie!, cancellationToken).ConfigureAwait(false);
            AddGameRoles(list);
        }
        catch (miHoYoApiException ex) when (ex.ReturnCode is -100 or 10001)
        {
            await UpdateGameRoleInfoAsync(role, cancellationToken).ConfigureAwait(false);
        }
    }



    private async Task UpdateGameRoleInfoAsync(GameRecordRole role, CancellationToken cancellationToken)
    {
        var passport = PassportService.GetPassportUserInfo(role.Aid);
        if (passport is null || passport.ErrorType is not PassportErrorType.Success || string.IsNullOrWhiteSpace(passport.Stoken))
        {
            return;
        }
        var roles = await UpdateGameRolesByPassportAsync(passport, cancellationToken);
        role = roles.FirstOrDefault(x => x.Uid == role.Uid)!;
        if (role is not null)
        {
            var list = await GetGameRolesAsync(role.GameBiz, role.Cookie!, cancellationToken).ConfigureAwait(false);
            AddGameRoles(list);
        }
    }



    public async Task<List<GameRecordRole>> UpdateGameRolesByPassportAsync(PassportUserInfo passport, CancellationToken cancellation = default)
    {
        var roles = await _passportService.UpdateGameRolesAsync(passport, await GetDeviceInfoAsync(passport.Region, cancellation), cancellation).ConfigureAwait(false);
        AddGameRoles(roles);
        return roles;
    }



    /// <summary>
    /// 保存游戏角色
    /// </summary>
    /// <param name="roles"></param>
    public static void AddGameRoles(IEnumerable<GameRecordRole> roles)
    {
        var list = roles as IList<GameRecordRole> ?? roles.ToList();
        if (list.Count == 0)
        {
            return;
        }
        using var dapper = DatabaseService.CreateConnection();
        using var t = dapper.BeginTransaction();
        dapper.Execute("""
            INSERT INTO GameRecordRole (Uid, GameBiz, Nickname, Level, Region, RegionName, Cookie, HeadIcon, Aid)
            VALUES (@Uid, @GameBiz, @Nickname, @Level, @Region, @RegionName, @Cookie, @HeadIcon, @Aid)
            ON CONFLICT(Uid, GameBiz) DO UPDATE SET Nickname = excluded.Nickname,
                                                    Level = excluded.Level,
                                                    Region = excluded.Region,
                                                    RegionName = excluded.RegionName,
                                                    Cookie = COALESCE(excluded.Cookie, GameRecordRole.Cookie),
                                                    HeadIcon = COALESCE(excluded.HeadIcon, GameRecordRole.HeadIcon),
                                                    Aid = COALESCE(NULLIF(excluded.Aid, 0), GameRecordRole.Aid);
            """, list, t);
        t.Commit();
    }



    public static List<GameRecordRole> GetGameRoles(GameBiz gameBiz)
    {
        using var dapper = DatabaseService.CreateConnection();
        var list = dapper.Query<GameRecordRole>("SELECT * FROM GameRecordRole WHERE GameBiz = @gameBiz;", new { gameBiz });
        return list.ToList();
    }



    public static GameRecordRole? GetLastSelectGameRecordRoleOrTheFirstOne(GameBiz gameBiz)
    {
        using var dapper = DatabaseService.CreateConnection();
        var role = dapper.QueryFirstOrDefault<GameRecordRole>("""
            SELECT r.* FROM GameRecordRole r INNER JOIN Setting s ON s.Value = r.Uid WHERE r.GameBiz = @gameBiz AND s.Key = @key AND s.GameId = @gameId LIMIT 1;
            """, new { gameBiz, key = "LastSelectGameRecordRole", gameId = gameBiz.Value });
        return role ??= dapper.QueryFirstOrDefault<GameRecordRole>("SELECT * FROM GameRecordRole WHERE GameBiz = @gameBiz LIMIT 1;", new { gameBiz });
    }




    /// <summary>
    /// 删除游戏角色
    /// </summary>
    /// <param name="role"></param>
    public static void DeleteGameRole(GameRecordRole role)
    {
        using var dapper = DatabaseService.CreateConnection();
        dapper.Execute("DELETE FROM GameRecordRole WHERE GameBiz = @GameBiz AND Uid = @Uid;", role);
    }


}
