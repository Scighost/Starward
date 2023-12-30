using Microsoft.UI.Windowing;
using System;
using System.IO;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class InstallGameWindow : WindowEx
{


    public static new InstallGameWindow Current { get; private set; }



    public InstallGameWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeWindow();
    }




    private void InitializeWindow()
    {
        Title = "Starward - Install Game";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, @"Assets\logo_dl.ico"));
        CenterInScreen(720, 416);
        AdaptTitleBarButtonColorToActuallTheme();
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
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





}
