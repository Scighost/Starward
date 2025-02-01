using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Starward.Core.Gacha.Genshin;
using Starward.Core.Gacha.StarRail;
using Starward.Core.Gacha.ZZZ;
using Starward.Core.GameNotice;
using Starward.Core.GameRecord;
using Starward.Core.HoYoPlay;
using Starward.Core.SelfQuery;
using Starward.Features.Background;
using Starward.Features.Database;
using Starward.Features.Gacha;
using Starward.Features.GameInstall;
using Starward.Features.GameLauncher;
using Starward.Features.GameRecord;
using Starward.Features.GameSetting;
using Starward.Features.HoYoPlay;
using Starward.Features.PlayTime;
using Starward.Features.RPC;
using Starward.Features.SelfQuery;
using Starward.Features.Update;
using Starward.RPC.Update;
using System;
using System.IO;
using System.Net;
using System.Net.Http;

namespace Starward.Frameworks;

public static class AppService
{



    private static IServiceProvider _serviceProvider;


    public static string LogFile { get; private set; }



    public static void ResetServiceProvider()
    {
        AppSetting.ClearCache();
        _serviceProvider = null!;
    }


    private static void BuildServiceProvider()
    {
        if (_serviceProvider == null)
        {
            var logFolder = Path.Combine(AppSetting.CacheFolder, "log");
            Directory.CreateDirectory(logFolder);
            LogFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
            Log.Logger = new LoggerConfiguration().WriteTo.File(path: LogFile, shared: true, outputTemplate: $$"""[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] [{{Path.GetFileName(Environment.ProcessPath)}} ({{Environment.ProcessId}})] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}""")
                                                  .Enrich.FromLogContext()
                                                  .CreateLogger();
            Log.Information($"Welcome to Starward v{AppSetting.AppVersion}\r\nSystem: {Environment.OSVersion}\r\nCommand Line: {Environment.CommandLine}");

            var sc = new ServiceCollection();
            sc.AddMemoryCache();
            sc.AddLogging(c => c.AddSerilog(Log.Logger));
            sc.AddHttpClient().ConfigureHttpClientDefaults(config =>
            {
                config.RemoveAllLoggers();
                config.ConfigureHttpClient(client => client.DefaultRequestHeaders.Add("User-Agent", $"Starward/{AppSetting.AppVersion}"));
                config.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
            });

            sc.AddSingleton<HoYoPlayClient>();
            sc.AddSingleton<GameNoticeClient>();
            sc.AddSingleton<HoYoPlayService>();
            sc.AddSingleton<BackgroundService>();
            sc.AddSingleton<GameLauncherService>();
            sc.AddSingleton<GamePackageService>();
            sc.AddSingleton<PlayTimeService>();
            sc.AddSingleton<GameSettingService>();
            sc.AddSingleton<GameNoticeService>();

            sc.AddSingleton<GenshinGachaClient>();
            sc.AddSingleton<StarRailGachaClient>();
            sc.AddSingleton<ZZZGachaClient>();
            sc.AddSingleton<GenshinGachaService>();
            sc.AddSingleton<StarRailGachaService>();
            sc.AddSingleton<ZZZGachaService>();

            sc.AddSingleton<HoyolabClient>();
            sc.AddSingleton<HyperionClient>();
            sc.AddSingleton<GameRecordService>();

            sc.AddSingleton<SelfQueryClient>();
            sc.AddSingleton<SelfQueryService>();

            sc.AddTransient<MetadataClient>();
            sc.AddTransient<UpdateService>();

            sc.AddSingleton<RpcService>();

            _serviceProvider = sc.BuildServiceProvider();
        }
    }


    public static T GetService<T>()
    {
        BuildServiceProvider();
        return _serviceProvider.GetService<T>()!;
    }


    public static ILogger<T> GetLogger<T>()
    {
        BuildServiceProvider();
        return _serviceProvider.GetService<ILogger<T>>()!;
    }


    public static SqliteConnection CreateDatabaseConnection()
    {
        return DatabaseService.CreateConnection();
    }



}
