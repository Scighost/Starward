// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Service;
using Starward.UI;
using Starward.UI.Welcome;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.UI;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{


    public static new MainWindow Current { get; private set; }

    public IntPtr HWND { get; private set; }


    public double UIScale => User32.GetDpiForWindow(HWND) / 96d;


    private readonly ILogger<MainWindow> _logger = AppConfig.GetLogger<MainWindow>();


    public MainWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeMainWindow();
        if (AppConfig.ConfigDirectory is null)
        {
            _logger.LogInformation($"{nameof(AppConfig.ConfigDirectory)} is null, navigate to {nameof(WelcomePage)}");
            MainWindow_Frame.Content = new WelcomePage();
        }
        else
        {
            _logger.LogInformation($"{nameof(AppConfig.ConfigDirectory)} is '{AppConfig.ConfigDirectory}'");
            MainWindow_Frame.Content = new MainPage();
        }
    }




    private void InitializeMainWindow()
    {
        HWND = WindowNative.GetWindowHandle(this);
        var titleBar = this.AppWindow.TitleBar;
        var len = (int)(48 * UIScale);
        titleBar.ExtendsContentIntoTitleBar = true;
        titleBar.SetDragRectangles(new RectInt32[] { new RectInt32(0, 0, 100000, len) });
        ChangeTitleBarButtonColor();
        ResizeToCertainSize();
    }



    private void ResizeToCertainSize()
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var scale = UIScale;
        var width = (int)(1280 * scale);
        var height = (int)(768 * scale);
        var x = (display.WorkArea.Width - width) / 2;
        var y = (display.WorkArea.Height - height) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }


    // todo change accent color


    private void RootGrid_ActualThemeChanged(FrameworkElement sender, object args)
    {
        ChangeTitleBarButtonColor();
    }


    private void ChangeTitleBarButtonColor()
    {
        if (AppWindowTitleBar.IsCustomizationSupported())
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
                    break;
                default:
                    break;
            }
        }
    }



    private void MainWindow_Closed(object sender, WindowEventArgs args)
    {

    }



    public void SetDragRectangles(params RectInt32[] value)
    {
        AppWindow.TitleBar.SetDragRectangles(value);
    }



    public void NavigateTo(Type page, object parameter, NavigationTransitionInfo infoOverride)
    {
        MainWindow_Frame.Navigate(page, parameter, infoOverride);
    }





}
