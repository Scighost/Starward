global using Starward.Language;
global using Starward.MyWindows;
using Microsoft.Extensions.Configuration;
using Starward.Core;
using Starward.Models;
using Starward.Services;
using System;

namespace Starward;

#if DISABLE_XAML_GENERATED_MAIN

/// <summary>
/// Program class
/// </summary>
public static class Program
{
    [global::System.Runtime.InteropServices.DllImport("Microsoft.ui.xaml.dll")]
    private static extern void XamlCheckProcessRequirements();

    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.UI.Xaml.Markup.Compiler", " 1.0.0.0")]
    //[global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.STAThreadAttribute]
    static void Main(string[] args)
    {

        if (args.Length > 0)
        {
            if (args[0].ToLower() is "playtime")
            {
                int pid = AppConfig.Configuration.GetValue<int>("pid");
                GameBiz biz = AppConfig.Configuration.GetValue<GameBiz>("biz");
                if (pid > 0 && biz > 0)
                {
                    var playtime = AppConfig.GetService<PlayTimeService>();
                    playtime.LogPlayTimeAsync(biz, pid).GetAwaiter().GetResult();
                }
                return;
            }

            if (args[0].ToLower() is "uninstall")
            {
                GameBiz biz = AppConfig.Configuration.GetValue<GameBiz>("biz");
                string? loc = AppConfig.Configuration.GetValue<string>("loc");
                UninstallStep steps = AppConfig.Configuration.GetValue<UninstallStep>("steps");
                var gameService = AppConfig.GetService<GameService>();
                int result = gameService.UninstallGame(biz, loc!, steps);
                Environment.Exit(result);
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


        XamlCheckProcessRequirements();

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


