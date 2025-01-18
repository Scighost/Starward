using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Starward.RPC;
using Starward.RPC.Lifecycle;
using Starward.RPC.Update;
using System.IO.Pipes;
using System.Net;
using System.Security.AccessControl;


var builder = WebApplication.CreateBuilder(args);


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
    LifecycleManager.AssociateProcesses(processId, false);
}



builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.ListenNamedPipe(AppConfig.MutexAndPipeName, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
}).UseNamedPipes(options =>
{
    var defaultSecurity = new PipeSecurity();
    defaultSecurity.AddAccessRule(new PipeAccessRule("Users", PipeAccessRights.ReadWrite | PipeAccessRights.CreateNewInstance, AccessControlType.Allow));
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
    config.ConfigureHttpClient(client => client.DefaultRequestHeaders.Add("User-Agent", AppConfig.MutexAndPipeName));
    config.ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AutomaticDecompression = DecompressionMethods.All });
});

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger, dispose: true);


builder.Services.AddScoped<MetadataClient>();
builder.Services.AddScoped<UpdateService>();



var app = builder.Build();


app.MapGrpcService<LifecycleController>();
app.MapGrpcService<UpdateController>();


app.Run();
