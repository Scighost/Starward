using Starward.Core;

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
