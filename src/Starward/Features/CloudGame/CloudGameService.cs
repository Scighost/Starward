using Starward.Core;
using Starward.Core.HoYoPlay;
using System;
using System.Diagnostics;
using System.Linq;

namespace Starward.Features.CloudGame;

public class CloudGameService
{


    public static Process? GetCloudGameProcess(GameId gameId)
    {
        string exeName = gameId.GameBiz.Value switch
        {
            GameBiz.hk4e_cn => "Genshin Impact Cloud Game",
            GameBiz.hk4e_global => "Genshin Impact Cloud",
            GameBiz.nap_cn => "Zenless Zone Zero Cloud",
            _ => throw new NotSupportedException($"Unsupported game biz: {gameId.GameBiz}")
        };
        int sessionId = Process.GetCurrentProcess().SessionId;
        return Process.GetProcessesByName(exeName).FirstOrDefault(x => x.SessionId == sessionId);
    }


}
