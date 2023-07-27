// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Pages;
using Starward.Pages.Welcome;
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
    }



    public MainWindow(string action)
    {
        Current = this;
        this.InitializeComponent();
        InitializeMainWindow(action);
    }




    private void InitializeMainWindow(string? action = null)
    {
        HWND = WindowNative.GetWindowHandle(this);
        var titleBar = AppWindow.TitleBar;
        var len = (int)(48 * UIScale);
        titleBar.ExtendsContentIntoTitleBar = true;
        SetDragRectangles(new RectInt32(0, 0, 100000, len));
        ChangeTitleBarButtonColor();
        if (action is "download")
        {
            Title = "Starward - Download Game";
            ResizeToCertainSize(720, 410);
        }
        else
        {
            Title = "Starward";
            ResizeToCertainSize();
        }
        if (action is "download")
        {
            MainWindow_Frame.Content = new DownloadGamePage();
        }
        else
        {
            if (AppConfig.UserDataFolder is null)
            {
                Console.WriteLine($"Config Directory is NULL");
                MainWindow_Frame.Content = new WelcomePage();
            }
            else
            {
                Console.WriteLine($"Config Directory is '{AppConfig.UserDataFolder}'");
                MainWindow_Frame.Content = new MainPage();
            }
        }
    }



    public void ResizeToCertainSize(int width = 0, int height = 0)
    {
        var display = DisplayArea.GetFromWindowId(AppWindow.Id, DisplayAreaFallback.Primary);
        var scale = UIScale;
        if (width * height == 0)
        {
            if (AppConfig.WindowSizeMode == 0)
            {
                width = (int)(1280 * scale);
                height = (int)(768 * scale);
            }
            else
            {
                width = (int)(1064 * scale);
                height = (int)(648 * scale);
            }
        }
        else
        {
            width = (int)(width * scale);
            height = (int)(height * scale);

        }
        var x = (display.WorkArea.Width - width) / 2;
        var y = (display.WorkArea.Height - height) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, width, height));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
    }




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
        MainPage.Current?.PauseVideo();
        Frame_Overlay.Visibility = Visibility.Visible;
        Frame_Overlay.Navigate(page, parameter!, infoOverride);
        var len = (int)(48 * UIScale);
        SetDragRectangles(new RectInt32[] { new RectInt32(0, 0, 100000, len) });
    }


    public void CloseOverlayPage()
    {
        MainPage.Current?.PlayVideo();
        Frame_Overlay.Visibility = Visibility.Collapsed;
        Frame_Overlay.Content = null;
        MainPage.Current?.UpdateDragRectangles();
    }




    public void ChangeAccentColor(Color? backColor = null, Color? foreColor = null)
    {
        try
        {
            var colors = new Color[14];
            if (backColor != null && foreColor != null)
            {
                Func<Color, Color, double, Color> mix = (Color input, Color blend, double percent) =>
                    Color.FromArgb(255,
                                   (byte)(input.R * percent + blend.R * (1 - percent)),
                                   (byte)(input.G * percent + blend.G * (1 - percent)),
                                   (byte)(input.B * percent + blend.B * (1 - percent)));

                colors[0] = backColor.Value;
                for (int i = 1; i < 4; i++)
                {
                    double percent = 1 - 0.2 * i;
                    colors[i] = mix(backColor.Value, Colors.White, percent);
                }
                for (int i = 4; i < 7; i++)
                {
                    double percent = 1 - 0.2 * (i - 3);
                    colors[i] = mix(backColor.Value, Colors.Black, percent);
                }

                colors[7] = foreColor.Value;
                for (int i = 8; i < 11; i++)
                {
                    double percent = 1 - 0.2 * (i - 7);
                    colors[i] = mix(foreColor.Value, Colors.White, percent);
                }
                for (int i = 11; i < 14; i++)
                {
                    double percent = 1 - 0.2 * (i - 10);
                    colors[i] = mix(foreColor.Value, Colors.Black, percent);
                }

                AppConfig.AccentColor = backColor.Value.ToHex() + foreColor.Value.ToHex();
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
                colors[7] = colors[0];
                colors[8] = colors[1];
                colors[9] = colors[2];
                colors[10] = colors[3];
                colors[11] = colors[4];
                colors[12] = colors[5];
                colors[13] = colors[6];
            }
            if (RootGrid.ActualTheme is ElementTheme.Dark)
            {
                Application.Current.Resources["SystemAccentColor"] = colors[0];
                Application.Current.Resources["SystemAccentColorLight1"] = colors[1];
                Application.Current.Resources["SystemAccentColorLight2"] = colors[2];
                Application.Current.Resources["SystemAccentColorLight3"] = colors[3];
                Application.Current.Resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(colors[10]);
                Application.Current.Resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(colors[10]);
                Application.Current.Resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(colors[9]);
                RootGrid.RequestedTheme = ElementTheme.Light;
                RootGrid.RequestedTheme = ElementTheme.Default;
            }
            if (RootGrid.ActualTheme is ElementTheme.Light)
            {
                Application.Current.Resources["SystemAccentColor"] = colors[0];
                Application.Current.Resources["SystemAccentColorDark1"] = colors[4];
                Application.Current.Resources["SystemAccentColorDark2"] = colors[5];
                Application.Current.Resources["SystemAccentColorDark3"] = colors[6];
                Application.Current.Resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(colors[12]);
                Application.Current.Resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(colors[13]);
                Application.Current.Resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(colors[11]);
                RootGrid.RequestedTheme = ElementTheme.Dark;
                RootGrid.RequestedTheme = ElementTheme.Default;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Change Accent Color\r\n{ex}");
        }
    }




}
