using System;

namespace Starward.Features.ViewHost;

/// <summary>
/// 主窗口状态变化消息
/// </summary>
internal class MainWindowStateChangedMessage
{

    public bool Activate { get; set; }


    public bool Hide { get; set; }


    public bool SessionLock { get; set; }


    public DateTimeOffset CurrentTime { get; set; }


    public DateTimeOffset LastActivatedTime { get; set; }


    public bool IsCrossingHour => CrossingHour(LastActivatedTime.LocalDateTime, CurrentTime.LocalDateTime);


    public bool ElapsedOver(TimeSpan timeSpan) => CurrentTime - LastActivatedTime > timeSpan;


    private static bool CrossingHour(DateTime lastTime, DateTime currentTime)
    {
        // 获取上次激活时间的小时起点和当前激活时间的小时起点
        DateTime lastHourStart = new DateTime(lastTime.Year, lastTime.Month, lastTime.Day, lastTime.Hour, 0, 0);
        DateTime currentHourStart = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, currentTime.Hour, 0, 0);

        // 如果两个时间的小时起点不同，说明跨过了整点
        return currentHourStart > lastHourStart;
    }


}
