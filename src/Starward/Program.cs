global using Starward.Language;
global using Starward.MyWindows;
using Microsoft.Extensions.Configuration;
using Starward.Core;
using Starward.Frameworks;
using Starward.Models;
using Starward.Services;
using Starward.Services.Launcher;
using System;

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
        if (args.Length > 0)
        {
            if (args[0].ToLower() is "playtime")
            {
                int pid = AppConfig.Configuration.GetValue<int>("pid");
                GameBiz biz = (GameBiz)AppConfig.Configuration.GetValue<string>("biz");
                if (pid > 0 && biz.IsKnown())
                {
                    var playtime = AppService.GetService<Features.PlayTime.PlayTimeService>();
                    playtime.LogPlayTimeAsync(biz, pid).GetAwaiter().GetResult();
                }
                return;
            }

            if (args[0].ToLower() is "uninstall")
            {
                GameBiz biz = (GameBiz)AppConfig.Configuration.GetValue<string>("biz");
                string? loc = AppConfig.Configuration.GetValue<string>("loc");
                UninstallStep steps = AppConfig.Configuration.GetValue<UninstallStep>("steps");
                var gameService = AppConfig.GetService<GameService>();
                int result = gameService.UninstallGame(biz, loc!, steps);
                Environment.Exit(result);
                return;
            }

            if (args[0].ToLower() is "startgame")
            {
                GameBiz biz = (GameBiz)AppConfig.Configuration.GetValue<string>("biz");
                var p = AppConfig.GetService<GameLauncherService>().StartGame(biz, true);
                if (p != null)
                {
                    AppConfig.GetService<PlayTimeService>().StartProcessToLogAsync(biz).GetAwaiter().GetResult();
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


}

#endif


