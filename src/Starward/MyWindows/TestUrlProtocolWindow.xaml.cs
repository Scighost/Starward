using Microsoft.UI;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestUrlProtocolWindow : Window
{
    public TestUrlProtocolWindow()
    {
        this.InitializeComponent();
        this.AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        if (!ShouldSystemUseDarkMode())
        {
            RootGrid.RequestedTheme = ElementTheme.Light;
        }
        if (MicaController.IsSupported())
        {
            this.SystemBackdrop = new MicaBackdrop();
        }
        else if (DesktopAcrylicController.IsSupported())
        {
            this.SystemBackdrop = new DesktopAcrylicBackdrop();
        }
        ChangeTitleBarButtonColor();
        ResizeToCertainSize();
        TextBlock_ExePath.Text = Process.GetCurrentProcess().MainModule?.FileName;
        TextBlock_Argu.Text = string.Join(' ', Environment.GetCommandLineArgs().Skip(1));
    }


    private void ChangeTitleBarButtonColor()
    {
        var titleBar = AppWindow.TitleBar;
        titleBar.ButtonBackgroundColor = Colors.Transparent;
        titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
        switch (RootGrid.ActualTheme)
        {
            case ElementTheme.Default:
                break;
            case ElementTheme.Light:
                titleBar.ButtonForegroundColor = Colors.Black;
                titleBar.ButtonHoverForegroundColor = Colors.Black;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0x00, 0x00, 0x00);
                break;
            case ElementTheme.Dark:
                titleBar.ButtonForegroundColor = Colors.White;
                titleBar.ButtonHoverForegroundColor = Colors.White;
                titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
                titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                break;
            default:
                break;
        }
    }



    public void ResizeToCertainSize()
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var scale = User32.GetDpiForWindow(WindowNative.GetWindowHandle(this)) / 96d;
        int width = (int)(480 * scale);
        int height = (int)(400 * scale);
        var x = (display.WorkArea.Width - width) / 2;
        var y = (display.WorkArea.Height - height) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }



    [DllImport("UXTheme.dll", SetLastError = true, EntryPoint = "#138")]
    private static extern bool ShouldSystemUseDarkMode();


}
