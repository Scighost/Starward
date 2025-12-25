using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Starward.Core.HoYoPlay;
using Starward.RPC.Env;
using Starward.RPC.GameInstall;
using Starward.RPC.Update;
using Starward.Setup.Core;
using System;
using System.IO;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;


namespace Starward.RPC;

public static class RpcRunner
{


    public static void Run(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning";

        var logFolder = Path.Combine(AppConfig.CacheFolder, "log");
        Directory.CreateDirectory(logFolder);
        var logFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
        Log.Logger = new LoggerConfiguration().WriteTo.File(path: logFile, shared: true, outputTemplate: $$"""[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] [Starward RPC ({{Environment.ProcessId}})] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}""")
                                          .Enrich.FromLogContext()
                                          .ReadFrom.Configuration(builder.Configuration)
                                          .CreateLogger();
        Log.Information($"Welcome to Starward RPC v{AppConfig.AppVersion}\r\nSystem: {Environment.OSVersion}\r\nCommand Line: {Environment.CommandLine}");

        if (args.Length < 2 || args[1] is not AppConfig.StartupMagic)
        {
            Log.Warning("Start magic is wrong, exit process!");
            return;
        }

        using Mutex mutex = new Mutex(true, AppConfig.MutexAndPipeName, out bool createdNew);
        if (!createdNew)
        {
            Log.Warning("Another instance is running, exit process!");
            return;
        }


        if (!AppConfig.IsAdmin)
        {
            Log.Error("Start without administrator, exit process!");
            return;
        }

        if (args.Length > 2 && int.TryParse(args[2], out int processId))
        {
            LifecycleManager.SetParentProcess(processId, false);
        }

        GCTimer.Start();


        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenNamedPipe(AppConfig.MutexAndPipeName, listenOptions =>
            {
                listenOptions.Protocols = HttpProtocols.Http2;
            });
        }).UseNamedPipes(options =>
        {
            var defaultSecurity = new PipeSecurity();
            var usersGroup = new SecurityIdentifier(WellKnownSidType.BuiltinUsersSid, null);
            defaultSecurity.AddAccessRule(new PipeAccessRule(usersGroup, PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
            options.PipeSecurity = defaultSecurity;
            options.CurrentUserOnly = false;
        });


        builder.Services.AddGrpc(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaxReceiveMessageSize = 64 << 20;
        });

        builder.Services.AddHttpClient().ConfigureHttpClientDefaults(config =>
        {
            config.RemoveAllLoggers();
            config.ConfigureHttpClient(client =>
            {
                client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
                client.DefaultRequestHeaders.Add("User-Agent", $"Starward.RPC/{AppConfig.AppVersion}");
            });
            config.ConfigurePrimaryHttpMessageHandler(GetDefaultSocketsHttpHandler);
        });
        builder.Services.AddHttpClient<HoYoPlayClient>().AddPolicyHandler(GetHttpRetryPolicy());


        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);


        builder.Services.AddScoped<ReleaseClient>();
        builder.Services.AddScoped<UpdateService>();
        //builder.Services.AddScoped<HoYoPlayClient>();
        builder.Services.AddScoped<GamePackageService>();
        builder.Services.AddSingleton<GameInstallService>();
        builder.Services.AddSingleton<GameInstallHelper>();
        builder.Services.AddScoped<GameUninstallService>();



        var app = builder.Build();


        app.MapGrpcService<EnviromentController>();
        app.MapGrpcService<UpdateController>();
        app.MapGrpcService<GameInstallController>();

        app.Run();
    }


    private static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
    {
        return HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));
    }


    private static SocketsHttpHandler GetDefaultSocketsHttpHandler()
    {
        return new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(5),
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
        };
    }


}

