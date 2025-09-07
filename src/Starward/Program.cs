global using Starward.Language;
using Microsoft.Extensions.Configuration;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.UrlProtocol;
using System;
using System.Collections;
using System.IO;
using System.Text;

namespace Starward;

#if DISABLE_XAML_GENERATED_MAIN

/// <summary>
/// Program class
/// </summary>
public static class Program
{


    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 3.0.0.2411")]
    //[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.STAThreadAttribute]
    static void Main(string[] args)
    {
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

        IConfiguration config = AppConfig.WithCommandLine(args);
        if (args.Length > 0)
        {
            if (args[0].ToLower() is "playtime")
            {
                int pid = config.GetValue<int>("pid");
                GameBiz biz = (GameBiz)config.GetValue<string>("biz");
                if (pid > 0)
                {
                    var playtime = AppConfig.GetService<Features.PlayTime.PlayTimeService>();
                    playtime.LogPlayTimeAsync(biz, pid).GetAwaiter().GetResult();
                }
                return;
            }

            if (args[0].ToLower() is "startgame")
            {
                GameBiz biz = (GameBiz)config.GetValue<string>("biz");
                GameId? gameId = GameId.FromGameBiz(biz);
                if (gameId is not null)
                {
                    AppConfig.GetService<GameLauncherService>().StartGameAsync(gameId).GetAwaiter().GetResult();
                }
                return;
            }

            if (args[0].ToLower().StartsWith("starward://"))
            {
                if (UrlProtocolService.HandleUrlProtocolAsync(args[0]).GetAwaiter().GetResult())
                {
                    return;
                }
            }
        }


        global::WinRT.ComWrappersSupport.InitializeComWrappers();
        global::Microsoft.UI.Xaml.Application.Start((p) =>
        {
            var context = new global::Microsoft.UI.Dispatching.DispatcherQueueSynchronizationContext(global::Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread());
            global::System.Threading.SynchronizationContext.SetSynchronizationContext(context);
            new App();
        });
    }

    private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        string logFile = AppConfig.LogFile;
        if (string.IsNullOrWhiteSpace(logFile))
        {
            var logFolder = Path.Combine(AppConfig.CacheFolder, "log");
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


