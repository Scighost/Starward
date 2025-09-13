using System;
using System.Diagnostics;
using System.Security.Principal;
using System.Text.Json.Serialization;
using Windows.Win32;
using dotnetCampus.Ipc.Context;
using dotnetCampus.Ipc.Pipes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Starward.Launcher;
using Starward.Launcher.Services;

var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
var principal = new WindowsPrincipal(WindowsIdentity.GetCurrent());
var isAdmin = principal.IsInRole(WindowsBuiltInRole.Administrator);
if (AppContext.BaseDirectory.StartsWith(programFiles) && !isAdmin)
{
    var psi = new ProcessStartInfo(
        Environment.ProcessPath!,
        args
    )
    {
        Verb = "runas",
        UseShellExecute = true
    };
    Process.Start(psi);
    Environment.Exit(0);
}

if (!string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("Starward_Launcher_Console"))) PInvoke.AllocConsole();

#if DEBUG
var builder = Host.CreateApplicationBuilder(args);
#else
var builder = Host.CreateEmptyApplicationBuilder(new HostApplicationBuilderSettings()
{
    Args = args,
    ContentRootPath = Environment.CurrentDirectory,
});
Microsoft.Extensions.Configuration.EnvironmentVariablesExtensions.AddEnvironmentVariables(builder.Configuration);
builder.Services.AddLogging(logging =>
{
    logging
        .AddConfiguration(builder.Configuration.GetSection("Logging"))
        .AddConsole()
        .Configure(options => options.ActivityTrackingOptions =
 ActivityTrackingOptions.SpanId | ActivityTrackingOptions.TraceId | ActivityTrackingOptions.ParentId);
});
#endif

#pragma warning disable IL3050, IL2026
builder.Services
    .Configure<IpcOptions>(builder.Configuration.GetSection("Starward:Launcher:TrayIcon"))
#pragma warning restore IL3050, IL2026
    .AddSingleton(principal)
    .AddHostedService<Worker>()
    .AddSingleton<FindExecutableService>()
    .AddSingleton<LaunchService>()
    .AddSingleton<TrayIconService>()
    .AddSingleton<VersionService>()
    .AddSingleton<IpcService>()
    .AddSingleton<IpcProvider>(sp => new IpcProvider(
        Guid.NewGuid().ToString("N"),
        new IpcConfiguration
        {
            IpcLoggerProvider = name => new IpcLoggerImpl(name, sp.GetRequiredService<ILogger<IpcProvider>>())
        }.UseSystemTextJsonIpcObjectSerializer(SourceGenerationContext.Default)));
var host = builder.Build();
host.Run();

internal class IpcOptions : IOptions<IpcOptions>
{
    public string Ipc { get; set; } = Guid.NewGuid().ToString("N");
    public IpcOptions Value => this;
}

[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(IpcService.GetIconPathResponse))]
internal partial class SourceGenerationContext : JsonSerializerContext;