using Microsoft.Data.Sqlite;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Starward.Core.GameNotice;
using Starward.Core.HoYoPlay;
using Starward.Features.Background;
using Starward.Features.Database;
using Starward.Features.GameLauncher;
using Starward.Features.HoYoPlay;
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
            var logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Starward\log");
            Directory.CreateDirectory(logFolder);
            LogFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
            Log.Logger = new LoggerConfiguration().WriteTo.File(path: LogFile, outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}")
                                                  .Enrich.FromLogContext()
                                                  .CreateLogger();
            Log.Information($"Welcome to Starward v{AppSetting.AppVersion}\r\nSystem: {Environment.OSVersion}\r\nCommand Line: {Environment.CommandLine}");

            var sc = new ServiceCollection();
            sc.AddLogging(c => c.AddSerilog(Log.Logger));
            sc.AddTransient(_ =>
            {
                var client = new HttpClient(new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All }) { DefaultRequestVersion = HttpVersion.Version20 };
                client.DefaultRequestHeaders.Add("User-Agent", $"Starward/{AppSetting.AppVersion}");
                return client;
            });

            sc.AddSingleton<HoYoPlayClient>();
            sc.AddSingleton<GameNoticeClient>();
            sc.AddSingleton<HoYoPlayService>();
            sc.AddSingleton<BackgroundService>();
            sc.AddSingleton<GameLauncherService>();
            sc.AddSingleton<GamePackageService>();

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
