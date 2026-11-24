using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.BH3;
using Starward.Core.GameRecord.BH3.DailyNote;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameRecord.BH3;

internal class GameRecordBH3Service : GameRecordServiceBase
{


    private readonly ILogger<GameRecordBH3Service> _logger;

    private readonly HyperionBH3Client _hyperionBH3Client;

    private readonly HoyolabBH3Client _hoyolabBH3Client;



    public GameRecordBH3Service(IServiceProvider serviceProvider) : base(serviceProvider)
    {
        _logger = serviceProvider.GetRequiredService<ILogger<GameRecordBH3Service>>();
        _hyperionBH3Client = serviceProvider.GetRequiredService<HyperionBH3Client>();
        _hoyolabBH3Client = serviceProvider.GetRequiredService<HoyolabBH3Client>();
    }



    private async Task<IGameRecordBH3Client> GetClientAsync(GameBiz gameBiz, CancellationToken cancellation = default)
    {
        ServerRegion region = GameBizToServerRegion(gameBiz);
        IGameRecordBH3Client client = region switch
        {
            ServerRegion.miHoYo => _hyperionBH3Client,
            ServerRegion.HoYoverse => _hoyolabBH3Client,
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
    /// BH3 实时便笺
    /// </summary>
    /// <param name="role"></param>
    /// <param name="forceUpdate"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public async Task<BH3DailyNote> GetBH3DailyNoteAsync(GameRecordRole role, bool forceUpdate = false, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(role.GameBiz, cancellationToken);
        string key = $"{nameof(BH3DailyNote)}_{role.Region}_{role.Uid}";
        if (forceUpdate || !_cache.TryGetValue(key, out BH3DailyNote? note))
        {
            note = await client.GetBH3DailyNoteAsync(role, cancellationToken);
            _cache.Set(key, note, TimeSpan.FromMinutes(5));
        }
        return note!;
    }


}
