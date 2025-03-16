global using Starward.Language;
using Microsoft.Extensions.Configuration;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.UrlProtocol;
using Starward.Frameworks;

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
            IConfiguration config = new ConfigurationBuilder().AddCommandLine(args).Build();
            if (args[0].ToLower() is "playtime")
            {
                int pid = config.GetValue<int>("pid");
                GameBiz biz = (GameBiz)config.GetValue<string>("biz");
                if (pid > 0)
                {
                    var playtime = AppService.GetService<Features.PlayTime.PlayTimeService>();
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
                    AppService.GetService<GameLauncherService>().StartGameAsync(gameId).GetAwaiter().GetResult();
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


