using Microsoft.Extensions.Logging;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Services.Launcher;
using System.Net.Http;

namespace Starward.Services.InstallGame;

internal class ZZZInstallGameService : InstallGameService
{


    public override GameBiz CurrentGame => GameBiz.nap;


    public ZZZInstallGameService(ILogger<ZZZInstallGameService> logger, GameLauncherService gameLauncherService, GamePackageService gamePackageService, HoYoPlayClient hoyoPlayClient, HttpClient httpClient)
        : base(logger, gameLauncherService, gamePackageService, hoyoPlayClient, httpClient)
    {

    }


}
