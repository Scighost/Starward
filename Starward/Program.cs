global using Starward.Language;
using Microsoft.Extensions.Configuration;
using Starward.Core;
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
            var config = new ConfigurationBuilder().AddCommandLine(args).Build();
            if (args[0].ToLower() is "playtime")
            {
                var str_biz = config["biz"];
                var str_pid = config["pid"];
                int.TryParse(str_pid, out int pid);
                Enum.TryParse(str_biz, out GameBiz biz);
                if (pid > 0 && biz > 0)
                {
                    var playtime = AppConfig.GetService<PlayTimeService>();
                    playtime.LogPlayTimeAsync(biz, pid).GetAwaiter().GetResult();
                }
                return;
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


