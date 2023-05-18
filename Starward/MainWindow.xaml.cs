// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.UI;
using Starward.UI.Welcome;
using System;
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



    public MainWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeMainWindow();
        if (AppConfig.ConfigDirectory is null)
        {
            MainWindow_Frame.Content = new WelcomePage(true);
        }
        else
        {
            MainWindow_Frame.Content = new MainPage();
        }
    }




    private void InitializeMainWindow()
    {
        Title = "Starward";
        HWND = WindowNative.GetWindowHandle(this);
        var titleBar = this.AppWindow.TitleBar;
        var len = (int)(48 * UIScale);
        titleBar.ExtendsContentIntoTitleBar = true;
        SetDragRectangles(new RectInt32(0, 0, 100000, len));
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



    public void NavigateTo(Type page, object? parameter, NavigationTransitionInfo infoOverride)
    {
        MainWindow_Frame.Navigate(page, parameter!, infoOverride);
    }



    public void OverlayFrameNavigateTo(Type page, object? parameter, NavigationTransitionInfo infoOverride)
    {
        Frame_Overlay.Visibility = Visibility.Visible;
        Frame_Overlay.Navigate(page, parameter!, infoOverride);
        var len = (int)(48 * UIScale);
        SetDragRectangles(new RectInt32[] { new RectInt32(0, 0, 100000, len) });
    }


    public void CloseOverlayPage()
    {
        Frame_Overlay.Visibility = Visibility.Collapsed;
        Frame_Overlay.Content = null;
        MainPage.Current?.UpdateDragRectangles();
    }




    public void ChangeAccentColor(Color? color = null)
    {
        var colors = new Color[7];
        if (color != null)
        {
            Func<byte, byte, double, byte> mix = (byte input, byte blend, double percent) => (byte)(input * percent + blend * (1 - percent));
            var primaryColor = color.Value;
            colors[0] = primaryColor;
            for (int i = 1; i < 4; i++)
            {
                double percent = 1 - 0.2 * i;
                colors[i] = Color.FromArgb(255, mix(primaryColor.R, 255, percent), mix(primaryColor.G, 255, percent), mix(primaryColor.B, 255, percent));
            }
            for (int i = 4; i < 7; i++)
            {
                double percent = 1 - 0.2 * (i - 3);
                colors[i] = Color.FromArgb(255, mix(primaryColor.R, 0, percent), mix(primaryColor.G, 0, percent), mix(primaryColor.B, 0, percent));
            }
            AppConfig.AccentColor = primaryColor.ToHex();
        }
        else
        {
            var setting = new Windows.UI.ViewManagement.UISettings();
            colors[0] = setting.GetColorValue(Windows.UI.ViewManagement.UIColorType.Accent);
            colors[1] = setting.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentLight1);
            colors[2] = setting.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentLight2);
            colors[3] = setting.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentLight3);
            colors[4] = setting.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentDark1);
            colors[5] = setting.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentDark2);
            colors[6] = setting.GetColorValue(Windows.UI.ViewManagement.UIColorType.AccentDark3);
        }
        Application.Current.Resources["SystemAccentColor"] = colors[0];
        Application.Current.Resources["SystemAccentColorLight1"] = colors[1];
        Application.Current.Resources["SystemAccentColorLight2"] = colors[2];
        Application.Current.Resources["SystemAccentColorLight3"] = colors[3];
        Application.Current.Resources["SystemAccentColorDark1"] = colors[4];
        Application.Current.Resources["SystemAccentColorDark2"] = colors[5];
        Application.Current.Resources["SystemAccentColorDark3"] = colors[6];
        RootGrid.RequestedTheme = ElementTheme.Light;
        RootGrid.RequestedTheme = ElementTheme.Default;
    }




}
