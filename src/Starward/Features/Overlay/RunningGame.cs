using Starward.Core;
using System.Diagnostics;
using Vanara.PInvoke;

namespace Starward.Features.Overlay;

public class RunningGame
{

    public GameBiz GameBiz { get; set; }

    public Process Process { get; set; }

    public int Pid { get; set; }

    public string Name { get; set; }

    public nint WindowHandle
    {
        get
        {
            if (field == 0)
            {
                field = Process.MainWindowHandle;
            }
            return field;
        }
        set { field = value; }
    }


    public User32.HWINEVENTHOOK WinEventHook { get; set; }



    public RunningGame(GameBiz gameBiz, Process process)
    {
        GameBiz = gameBiz;
        Process = process;
        Pid = process.Id;
        Name = process.ProcessName;
        WindowHandle = process.MainWindowHandle;
    }




}
