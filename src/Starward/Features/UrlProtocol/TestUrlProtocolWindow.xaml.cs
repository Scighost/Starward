using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Starward.Frameworks;
using System;
using System.Diagnostics;
using System.Linq;


namespace Starward.Features.UrlProtocol;

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
        if (!ShouldAppsUseDarkMode())
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


}
