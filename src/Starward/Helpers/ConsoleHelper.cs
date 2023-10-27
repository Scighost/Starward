using System;
using Vanara.PInvoke;

namespace Starward.Helpers;

internal static class ConsoleHelper
{

    private static HWND HWND;


    public static void Alloc()
    {
        Kernel32.AllocConsole();
        HWND = Kernel32.GetConsoleWindow();
        Console.Title = "Starward Console Output";
    }


    public static void Show()
    {
        if (HWND != IntPtr.Zero)
        {
            User32.ShowWindow(HWND, ShowWindowCommand.SW_SHOWNORMAL);
        }
    }


    public static void Hide()
    {
        if (HWND != IntPtr.Zero)
        {
            User32.ShowWindow(HWND, ShowWindowCommand.SW_HIDE);
        }
    }


}
