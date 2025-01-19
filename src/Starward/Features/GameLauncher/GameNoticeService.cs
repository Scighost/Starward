using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.GameNotice;
using Starward.Features.GameRecord;
using System.Globalization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Starward.Features.GameLauncher;

internal class GameNoticeService
{


    private readonly ILogger<GameNoticeService> _logger;


    private readonly GameRecordService _gameRecordService;


    private readonly GameNoticeClient _gameNoticeClient;



    public GameNoticeService(ILogger<GameNoticeService> logger, GameRecordService gameRecordService, GameNoticeClient gameNoticeClient)
    {
        _logger = logger;
        _gameRecordService = gameRecordService;
        _gameNoticeClient = gameNoticeClient;
    }




    public string GetGameNoticeUrl(GameBiz biz)
    {
        long uid = GetLastUid(biz);
        return GameNoticeClient.GetGameNoticeUrl(biz, uid, CultureInfo.CurrentUICulture.Name);
    }




    public async Task<bool> IsNoticeAlertAsync(GameBiz gameBiz, CancellationToken cancellationToken = default)
    {
        long uid = GetLastUid(gameBiz);
        if (uid is 0)
        {
            return false;
        }
        return await _gameNoticeClient.IsNoticeAlertAsync(gameBiz, uid, CultureInfo.CurrentUICulture.Name, cancellationToken);
    }




    private long GetLastUid(GameBiz gameBiz)
    {
        var role = _gameRecordService.GetLastSelectGameRecordRoleOrTheFirstOne(gameBiz);
        if (role is not null)
        {
            return role.Uid;
        }
        return GetUidFromRegistry(gameBiz);
    }




    private static long GetUidFromRegistry(GameBiz gameBiz)
    {
        string key = gameBiz.GetGameRegistryKey();
        if (gameBiz.Game is GameBiz.bh3)
        {
            if (Registry.GetValue(key, "GENERAL_DATA_V2_LastLoginUserId_h47158221", null) is int uid)
            {
                return uid;
            }
        }
        else if (gameBiz.Game is GameBiz.hk4e)
        {
            if (Registry.GetValue(key, "__LastUid___h2153286551", null) is byte[] bytes)
            {
                string str = Encoding.UTF8.GetString(bytes).Trim();
                if (long.TryParse(str, out long uid))
                {
                    return uid;
                }
            }
        }
        else if (gameBiz.Game is GameBiz.hkrpg)
        {
            if (Registry.GetValue(key, "App_LastUserID_h2841727341", null) is int uid)
            {
                return uid;
            }
        }
        return 0;
    }





}
