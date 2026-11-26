global using Starward.Language;
using Microsoft.Extensions.Configuration;
using Microsoft.Windows.AppLifecycle;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.RPC;
using Starward.Features.UrlProtocol;
using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Starward;

#if DISABLE_XAML_GENERATED_MAIN

/// <summary>
/// Program class
/// </summary>
public static class Program
{


    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2609")]
    //[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.STAThreadAttribute]
    static void Main(string[] args)
    {
        if (args.Length > 0 && args[0] == Features.Hypergryph.EndfieldLauncher.Command)
        {
            Environment.ExitCode = Features.Hypergryph.EndfieldLauncher.RunHelper(args);
            return;
        }
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.Normal;

        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        AppConfig.CheckEnviroment();
        HandleStartupAsync(args).GetAwaiter().GetResult();

        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }


    private static async Task HandleStartupAsync(string[] args)
    {
        if (args.Length is 0 || AppConfig.UserDataFolder is null)
        {
            return;
        }
        string firstArg = args[0].ToLower();
        IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
        if (firstArg is "rpc")
        {
            RpcRunner.Run(args);
            Environment.Exit(0);
        }
        if (firstArg is "playtime")
        {
            int pid = config.GetValue<int>("pid");
            GameBiz biz = (GameBiz)config.GetValue<string>("biz");
            if (pid > 0)
            {
                var playtime = AppConfig.GetService<Features.PlayTime.PlayTimeRecordService>();
                await playtime.LogPlayTimeAsync(biz, pid);
            }
            Environment.Exit(0);
        }

        if (firstArg is "startgame")
        {
            GameBiz biz = (GameBiz)config.GetValue<string>("biz");
            GameId? gameId = GameId.FromGameBiz(biz);
            if (gameId is not null)
            {
                await AppConfig.GetService<GameLauncherService>().StartGameAsync(gameId);
            }
            Environment.Exit(0);
        }

        if (firstArg.StartsWith("starward://"))
        {
            if (await UrlProtocolService.HandleUrlProtocolAsync(args[0]))
            {
                Environment.Exit(0);
            }
        }

        if (firstArg.StartsWith("starward-hyp-login://"))
        {
            try
            {
                var main = AppInstance.FindOrRegisterForKey("Starward.Main");
                if (!main.IsCurrent)
                {
                    await main.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
                }
            }
            finally
            {
                Environment.Exit(0);
            }
        }
    }




    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string logFile = AppConfig.LogFile;
        if (string.IsNullOrWhiteSpace(logFile))
        {
            string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward", "log");
            Directory.CreateDirectory(logFolder);
            logFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
        }
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Program Crash:");
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
}

#endif


