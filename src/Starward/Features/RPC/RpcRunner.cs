using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Windows.AppLifecycle;
using Serilog;
using Starward.Features.GameInstall;
using Starward.GameInstall;
using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.IO.Pipes;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using ZiggyCreatures.Caching.Fusion;

namespace Starward.Features.RPC;

public static class RpcRunner
{

    public static void Run(string[] args)
    {
        if (args.Length < 2 || args[1] is not RpcService.StartupMagic)
        {
            return;
        }
        if (!AppConfig.IsAdmin || !AppInstance.FindOrRegisterForKey("Starward.RPC").IsCurrent)
        {
            return;
        }

        bool parentProcess = false;
        if (args.Length > 2 && int.TryParse(args[2], out int pid))
        {
            try
            {
                var p = Process.GetProcessById(pid);
                p.EnableRaisingEvents = true;
                p.Exited += (_, _) => Environment.Exit(0);
            }
            catch { }
        }
        if (!parentProcess)
        {
            var instance = App.FindInstanceForKey("Starward.Main");
            if (instance is null)
            {
                return;
            }
            var p = Process.GetProcessById((int)instance.ProcessId);
            p.EnableRaisingEvents = true;
            p.Exited += (_, _) => Environment.Exit(0);
        }

        string? authorization = null;
        if (args.Length > 3)
        {
            authorization = args[3];
        }


        var builder = WebApplication.CreateBuilder(args);

        var logFolder = Path.Combine(AppConfig.CacheFolder, "log");
        Directory.CreateDirectory(logFolder);
        var logFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
        Log.Logger = new LoggerConfiguration().WriteTo.File(path: logFile, shared: true, outputTemplate: $$"""[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] [Starward RPC ({{Environment.ProcessId}})] {SourceContext} {ExtraContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}""")
                                              .Enrich.FromLogContext()
                                              .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
                                              .MinimumLevel.Override("Microsoft.Hosting.Lifetime", Serilog.Events.LogEventLevel.Warning)
                                              .MinimumLevel.Override("ZiggyCreatures.Caching.Fusion", Serilog.Events.LogEventLevel.Error)
                                              .CreateLogger();
        Log.Information($"Welcome to Starward RPC v{AppConfig.AppVersion}\r\nSystem: {Environment.OSVersion}\r\nCommand Line: {Environment.CommandLine}");

        builder.WebHost.ConfigureKestrel(serverOptions =>
        {
            serverOptions.ListenNamedPipe(RpcService.PipeName, listenOptions =>
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
            options.ResponseCompressionLevel = CompressionLevel.NoCompression;
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

        builder.Logging.ClearProviders();
        builder.Logging.AddSerilog(Log.Logger, dispose: true);

        builder.Services.AddMemoryCache();
        builder.Services.AddStarwardGameInstall();
        builder.Services.AddFusionCache().WithDefaultEntryOptions(op =>
                                         {
                                             op.Duration = TimeSpan.FromMinutes(5);
                                             op.EagerRefreshThreshold = 0.6f;
                                             op.AllowStaleOnReadOnly = true;
                                         });


        var app = builder.Build();

        if (!string.IsNullOrWhiteSpace(authorization))
        {
            app.Use(async (context, next) =>
            {
                if (context.Request.Headers.Authorization.ToString() != authorization)
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return;
                }
                await next(context);
            });
        }

        app.MapGrpcService<GameInstallController>();

        app.Run();
    }



    private static SocketsHttpHandler GetDefaultSocketsHttpHandler()
    {
        return new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = TimeSpan.FromMinutes(2),
            EnableMultipleHttp2Connections = true,
            EnableMultipleHttp3Connections = true,
        };
    }




}