using System;
using System.Timers;

namespace Starward.RPC.Env;

internal static class GCTimer
{

    private static readonly Timer _timer;


    static GCTimer()
    {
        _timer = new Timer(TimeSpan.FromMinutes(1));
        _timer.Elapsed += _timer_Elapsed;
    }


    private static void _timer_Elapsed(object? sender, ElapsedEventArgs e)
    {
        GC.Collect();
    }


    public static void Start()
    {
        _timer.Start();
    }


    public static void Stop()
    {
        _timer.Stop();
    }


}
