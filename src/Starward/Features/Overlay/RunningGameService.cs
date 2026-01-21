using Starward.Core;
using Starward.Features.GamepadControl;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Timers;
using Vanara.PInvoke;

namespace Starward.Features.Overlay;

internal static class RunningGameService
{

    private static readonly Lock _runningGameLock = new();


    private static readonly List<RunningGame> _runningGames = new();


    private static readonly System.Timers.Timer _timer = new();


    private static RunningGame? _latestActiveGame;


    private static readonly User32.WinEventProc hookProc;


    static RunningGameService()
    {
        _timer.Elapsed += CheckGameExit;
        hookProc = new User32.WinEventProc(WinEventProc);
    }



    public static void AddRuninngGame(GameBiz gameBiz, Process process)
    {
        try
        {
            lock (_runningGameLock)
            {
                if (_runningGames.FirstOrDefault(x => x.Pid == process.Id) is not RunningGame runningGame)
                {
                    runningGame = new RunningGame(gameBiz, process);
                    runningGame.WinEventHook = User32.SetWinEventHook(User32.EventConstant.EVENT_SYSTEM_FOREGROUND, User32.EventConstant.EVENT_SYSTEM_FOREGROUND, HINSTANCE.NULL, hookProc, (uint)process.Id, 0, User32.WINEVENT.WINEVENT_OUTOFCONTEXT);
                    _runningGames.Add(runningGame);
                    _latestActiveGame = runningGame;
                    Debug.WriteLine($"Added running game: {runningGame.Name} ({runningGame.Pid})");
                }
                GamepadController.DisableGamepadGuideButtonForGameBarBecauseOfGameStart();
                _timer.Start();
            }
        }
        catch { }
    }



    public static RunningGame? GetLatestActiveGame()
    {
        return _latestActiveGame;
    }


    public static int GetRunningGameCount()
    {
        return _runningGames.Count;
    }


    private static void CheckGameExit(object? sender, ElapsedEventArgs e)
    {
        lock (_runningGameLock)
        {
            for (int i = 0; i < _runningGames.Count; i++)
            {
                RunningGame runningGame = _runningGames[i];
                if (runningGame.Process.HasExited)
                {
                    User32.UnhookWinEvent(runningGame.WinEventHook);
                    _runningGames.RemoveAt(i);
                    if (_latestActiveGame?.Pid == runningGame.Pid)
                    {
                        _latestActiveGame = null;
                    }
                    i--; // Adjust index after removal
                    Debug.WriteLine($"Running game exited: {runningGame.Name} ({runningGame.Pid})");
                }
            }
            if (_runningGames.Count == 0)
            {
                _overlayWindow?.DispatcherQueue.TryEnqueue(() =>
                {
                    if (_overlayWindow?.AppWindow is not null)
                    {
                        _overlayWindow.Close();
                    }
                });
                GamepadController.RestoreGamepadGuideButtonForGameBarBecauseOfGameExit();
            }
        }
    }



    private static OverlayWindow? _overlayWindow;


    public static bool OpenOverlayWindow()
    {
        return false;
        if (_latestActiveGame is null || _latestActiveGame.WindowHandle is 0 || User32.IsIconic(_latestActiveGame.WindowHandle))
        {
            return false;
        }
        if (_overlayWindow?.AppWindow is null)
        {
            _overlayWindow = new OverlayWindow();
        }
        _overlayWindow.ShowActive(_latestActiveGame);
        return true;
    }



    private static void WinEventProc(User32.HWINEVENTHOOK hWinEventHook, User32.EventConstant winEvent, HWND hwnd, User32.ObjectIdentifier idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        if (_runningGames.FirstOrDefault(x => x.WinEventHook == hWinEventHook) is RunningGame runningGame)
        {
            _latestActiveGame = runningGame;
            Debug.WriteLine($"Set to foreground: {runningGame.Name}");
        }
    }


}
