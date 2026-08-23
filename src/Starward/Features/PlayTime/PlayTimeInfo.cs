using Starward.Core;

namespace Starward.Features.PlayTime;

public class PlayTimeInfo
{


    public long StartTimeStamp { get; set; }


    public long LatestTimeStamp { get; set; }


    public GameBiz GameBiz { get; set; }


    public int Pid { get; set; }


    public PlayTimeState State { get; set; }


    public long Duration { get; set; }


    public string? Message { get; set; }


    public string? StartTime { get; set; }


    public string? LatestTime { get; set; }


    public string? PlayTime { get; set; }


}
