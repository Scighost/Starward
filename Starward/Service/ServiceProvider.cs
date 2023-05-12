using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Hyperion;
using Starward.Core.Hyperion.Genshin;
using Starward.Core.Hyperion.StarRail;
using Starward.Core.Launcher;
using Starward.Core.Metadata;
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

        sc.AddTransient(_ => new HttpClient(new HttpClientHandler { AutomaticDecompression = System.Net.DecompressionMethods.All }));

        sc.AddSingleton<GenshinGachaClient>();
        sc.AddSingleton<StarRailGachaClient>();
        sc.AddSingleton<HyperionClient>();
        sc.AddSingleton<HyperionGenshinClient>();
        sc.AddSingleton<HyperionStarRailClient>();
        sc.AddSingleton<LauncherClient>();
        sc.AddSingleton<MetadataClient>();

        sc.AddSingleton<DatabaseService>();
        sc.AddSingleton<GachaLogService>();
        sc.AddSingleton<LauncherService>();

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



