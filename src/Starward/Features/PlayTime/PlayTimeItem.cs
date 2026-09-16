using Starward.Core;
using System;

namespace Starward.Features.PlayTime;

public class PlayTimeItem
{
    public long TimeStamp { get; set; }

    public GameBiz GameBiz { get; set; }

    public int Pid { get; set; }

    public PlayTimeState State { get; set; }

    public long CursorPos { get; set; }

    public string? Message { get; set; }
}


internal struct PlayTimeItemStruct
{
    public long TimeStamp { get; set; }

    public GameBiz GameBiz { get; set; }

    public int Pid { get; set; }

    public PlayTimeState State { get; set; }
}


public class PlayTimeStats
{
    public GameBiz GameBiz { get; set; }

    public int Pid { get; set; }

    public long StartTime { get; set; }

    public long EndTime { get; set; }

    public bool Interruption { get; set; }

    public int Type { get; set; }
}


public class PlayTimeDayItem
{
    public DateTimeOffset Date { get; set; }

    public TimeSpan PlayTime { get; set; }
}
