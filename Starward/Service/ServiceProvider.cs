using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Hoyolab;
using Starward.Core.Hoyolab.Genshin;
using Starward.Core.Hoyolab.StarRail;
using Starward.Core.Launcher;
using System;
using System.Net.Http;

namespace Starward.Service;

internal abstract class ServiceProvider
{


    private static readonly IServiceProvider _serviceProvider;




    static ServiceProvider()
    {
        var sc = new ServiceCollection();
#if DEBUG
        sc.AddLogging(configure => configure.AddDebug());
#endif
        sc.AddLogging(configure => configure.AddSimpleConsole());

        sc.AddSingleton(new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All }));
        sc.AddSingleton<WishRecordClient>();
        sc.AddSingleton<WarpRecordClient>();
        sc.AddSingleton<HoyolabClient>();
        sc.AddSingleton<HoyolabGenshinClient>();
        sc.AddSingleton<HoyolabStarRailClient>();
        sc.AddSingleton<LauncherClient>();

        sc.AddSingleton<WarpRecordService>();
        _serviceProvider = sc.BuildServiceProvider();
    }




    public static T GetService<T>()
    {
        return _serviceProvider.GetService<T>()!;
    }


    public static ILogger<T> GetLogger<T>()
    {
        return _serviceProvider.GetService<ILogger<T>>()!;
    }


}



