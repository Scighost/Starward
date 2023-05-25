using Starward.Core;

namespace Starward.Models;

public class PlayTimeItem
{


    public long TimeStamp { get; set; }


    public GameBiz GameBiz { get; set; }


    public int Pid { get; set; }


    public PlayState State { get; set; }


    public long CursorPos { get; set; }


    public string? Message { get; set; }


    public enum PlayState
    {

        None,

        Start,

        Play,

        Stop,

        Error,

    }

}
