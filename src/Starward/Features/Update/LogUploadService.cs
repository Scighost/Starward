using System;
using System.Diagnostics;
using System.Timers;

namespace Starward.Features.Update;

internal static class LogUploadService
{

    private static LogUploadClient _client;

    private static Timer _timer;

    private static DateTimeOffset _startTime;

    private static bool _started;


    public static void Start()
    {
        try
        {
            _timer = new Timer(TimeSpan.FromMinutes(5));
            _timer.Elapsed += UploadLog;
            _timer.Start();
            _startTime = DateTimeOffset.Now;
            UploadLog(null, null!);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    private static async void UploadLog(object? sender, ElapsedEventArgs e)
    {
        try
        {
            if (_client is null)
            {
                _client = AppConfig.GetService<LogUploadClient>();
#if DEBUG
                _client.AppName = "Starward.Debug";
#else
                _client.AppName = "Starward";
#endif
                _client.AppVersion = AppConfig.AppVersion;
                _client.DeviceId = AppConfig.DeviceId.ToString();
                _client.SessionId = AppConfig.SessionId.ToString();
            }
            if (!_started)
            {
                await _client.UploadLogAsync(new LogUploadRequestBase("Runtime", "Start") { Time = _startTime });
                _started = true;
            }
            await _client.UploadLogAsync(new LogUploadRequestBase("Runtime", "Running"));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


}
