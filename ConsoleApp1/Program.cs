using System.Timers;



var timer = new System.Timers.Timer();
timer.Interval = 8;
DateTime time = DateTime.Now;
timer.Elapsed += Timer_Elapsed;
timer.Start();



while (true)
{
    await Task.Delay(10000);
}




void Timer_Elapsed(object? sender, ElapsedEventArgs e)
{
    var now = DateTime.Now;
    Console.WriteLine(now - time);
    time = now;
}