using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestUrlProtocolWindow : WindowEx
{

    public TestUrlProtocolWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
    }


    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        if (!ShouldSystemUseDarkMode())
        {
            RootGrid.RequestedTheme = ElementTheme.Light;
        }
        if (MicaController.IsSupported())
        {
            SystemBackdrop = new MicaBackdrop();
        }
        else if (DesktopAcrylicController.IsSupported())
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        CenterInScreen(480, 400);
        AdaptTitleBarButtonColorToActuallTheme();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
        TextBlock_ExePath.Text = Process.GetCurrentProcess().MainModule?.FileName;
        TextBlock_Argu.Text = string.Join(' ', Environment.GetCommandLineArgs().Skip(1));
    }



    public override nint WindowSubclassProc(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_SYSCOMMAND)
        {
            // SC_MAXIMIZE
            if (wParam == 0xF030)
            {
                return IntPtr.Zero;
            }
        }
        return base.WindowSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }



    [return: MarshalAs(UnmanagedType.Bool)]
    [LibraryImport("uxtheme.dll", EntryPoint = "#138", SetLastError = true)]
    private static partial bool ShouldSystemUseDarkMode();


}
