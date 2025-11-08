using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Starward.Core.HoYoPlay;
using Starward.RPC;
using Starward.RPC.Env;
using Starward.RPC.GameInstall;
using Starward.RPC.Update;
using System;
using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Threading;


AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
{
    var logFolder = Path.Combine(AppConfig.CacheFolder, "log");
    Directory.CreateDirectory(logFolder);
    var logFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
    var sb = new StringBuilder();
    sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] RPC Crash:");
    sb.AppendLine(e.ExceptionObject.ToString());
    if (e.ExceptionObject is Exception { Data.Count: > 0 } ex)
    {
        foreach (DictionaryEntry item in ex.Data)
        {
            sb.AppendLine($"{item.Key}: {item.Value}");
        }
    }
    using var fs = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
    using var sw = new StreamWriter(fs);
    sw.Write(sb);
}

var builder = WebApplication.CreateBuilder(args);

builder.Configuration["Serilog:MinimumLevel:Override:Microsoft.AspNetCore"] = "Warning";

var logFolder = Path.Combine(AppConfig.CacheFolder, "log");
Directory.CreateDirectory(logFolder);
var logFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
Log.Logger = new LoggerConfiguration().WriteTo.File(path: logFile, shared: true, outputTemplate: $$"""[{Timestamp:HH:mm:ss.fff}] [{Level:u4}] [{{Path.GetFileName(Environment.ProcessPath)}} ({{Environment.ProcessId}})] {SourceContext}{NewLine}{Message}{NewLine}{Exception}{NewLine}""")
                                      .Enrich.FromLogContext()
                                      .ReadFrom.Configuration(builder.Configuration)
                                      .CreateLogger();
Log.Information($"Welcome to Starward RPC v{AppConfig.AppVersion}\r\nSystem: {Environment.OSVersion}\r\nCommand Line: {Environment.CommandLine}");


using Mutex mutex = new Mutex(true, AppConfig.MutexAndPipeName, out bool createdNew);
if (!createdNew)
{
    Log.Warning("Another instance is running, exit process!");
    return;
}

if (args.FirstOrDefault() is not AppConfig.StartupMagic)
{
    Log.Warning("Start magic is wrong, exit process!");
    return;
}

if (!AppConfig.IsAdmin)
{
    Log.Error("Start without administrator, exit process!");
    return;
}

if (args.Length > 1 && int.TryParse(args[1], out int processId))
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
        client.DefaultRequestHeaders.Add("User-Agent", $"Starward.RPC/{AppConfig.AppVersion}");
        client.DefaultRequestVersion = HttpVersion.Version20;
        client.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrHigher;
    });
    config.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
    {
        AutomaticDecompression = DecompressionMethods.All,
        EnableMultipleHttp2Connections = true,
        EnableMultipleHttp3Connections = true,
    });
});
builder.Services.AddHttpClient<HoYoPlayClient>().AddPolicyHandler(GetHttpRetryPolicy());


builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);


builder.Services.AddScoped<MetadataClient>();
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


static IAsyncPolicy<HttpResponseMessage> GetHttpRetryPolicy()
{
    return HttpPolicyExtensions.HandleTransientHttpError().WaitAndRetryAsync(3, i => TimeSpan.FromSeconds(i));
}

