using Starward.Core.DeviceFp;

namespace Starward.Core.GameRecord;

public interface IGameRecordClient
{

    public AndroidDeviceInfo? DeviceInfo { get; set; }


    public string Language { get; set; }


    /// <summary>
    /// 获取游戏账号信息
    /// </summary>
    /// <param name="gameBiz"></param>
    /// <param name="cookie"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<List<GameRecordRole>> GetGameRolesAsync(GameBiz gameBiz, string cookie, CancellationToken cancellationToken = default);


    /// <summary>
    /// 获取游戏账号头像
    /// </summary>
    /// <param name="role"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    public Task<string> GetGameRoleHeadIconAsync(GameRecordRole role, CancellationToken cancellationToken = default);
}