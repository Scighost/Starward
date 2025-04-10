using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using Starward.Features.GameSelector;
using Starward.Features.Setting;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using Vanara.PInvoke;
using Windows.Foundation;


namespace Starward.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class SystemTrayWindow : WindowEx
{

    private readonly ILogger<SystemTrayWindow> _logger = AppConfig.GetLogger<SystemTrayWindow>();

    private readonly GameLauncherService _gameLauncherService = AppConfig.GetService<GameLauncherService>();


    public SystemTrayWindow()
    {
        this.InitializeComponent();
        UpdateInstalledGames();
        InitializeWindow();
        SetTrayIcon();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        WeakReferenceMessenger.Default.Register<InstalledGameRefreshedMessage>(this, (_, _) => UpdateInstalledGames());
        WeakReferenceMessenger.Default.Register<GameInstallPathChangedMessage>(this, (_, _) => UpdateInstalledGames());
    }




    private unsafe void InitializeWindow()
    {
        new SystemBackdropHelper(this, SystemBackdropProperty.AcrylicDefault with
        {
            TintColorLight = 0xFFE7E7E7,
            TintColorDark = 0xFF404040
        }).TrySetAcrylic(true);

        AppWindow.IsShownInSwitchers = false;
        AppWindow.Closing += (s, e) => e.Cancel = true;
        this.Activated += SystemTrayWindow_Activated;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsMinimizable = false;
            presenter.IsResizable = false;
            presenter.IsAlwaysOnTop = true;
        }

        var flag = User32.GetWindowLongPtr(WindowHandle, User32.WindowLongFlags.GWL_STYLE);
        flag &= ~(nint)User32.WindowStyles.WS_CAPTION;
        flag &= ~(nint)User32.WindowStyles.WS_BORDER;
        User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, flag);
        var p = DwmApi.DWM_WINDOW_CORNER_PREFERENCE.DWMWCP_ROUND;
        DwmApi.DwmSetWindowAttribute(WindowHandle, DwmApi.DWMWINDOWATTRIBUTE.DWMWA_WINDOW_CORNER_PREFERENCE, (nint)(&p), sizeof(DwmApi.DWM_WINDOW_CORNER_PREFERENCE));

        Show();
        Hide();
    }



    private void SetTrayIcon()
    {
        try
        {
            string icon = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.ico");
            if (File.Exists(icon))
            {
                trayIcon.Icon = new(icon);
            }
        }
        catch { }
    }



    public ObservableCollection<GameBizIcon> InstalledGames { get; set; } = new();


    private void UpdateInstalledGames()
    {
        InstalledGames.Clear();
        foreach (var display in GetGameServerArea(GameLauncherService.GetCachedGameInfos()))
        {
            foreach (var server in display.Servers)
            {
                var path = GameLauncherService.GetGameInstallPath(server.GameId);
                if (Directory.Exists(path))
                {
                    server.MaskOpacity = 0;
                    InstalledGames.Add(server);
                }
            }
        }
    }


    private List<GameBizDisplay> GetGameServerArea(List<GameInfo> gameInfos)
    {
        var list = new List<GameBizDisplay>();

        try
        {
            if (LanguageUtil.FilterLanguage(CultureInfo.CurrentUICulture.Name) is "zh-cn")
            {
                // 当前语言为简体中文时，游戏信息显示从中国官服获取的内容
                foreach (var info in gameInfos)
                {
                    if (info.GameBiz.IsChinaServer() && !info.IsBilibiliServer())
                    {
                        list.Add(new GameBizDisplay { GameInfo = info });
                    }
                }
            }
            else
            {
                // 当前语言不为简体中文时，游戏信息显示从国际服获取的内容
                foreach (var info in gameInfos)
                {
                    if (info.GameBiz.IsGlobalServer())
                    {
                        list.Add(new GameBizDisplay { GameInfo = info });
                    }
                }
            }

            // 分类每个游戏的服务器信息
            foreach (var item in list)
            {
                string game = item.GameInfo.GameBiz.Game;
                foreach (string suffix in (string[])["_cn", "_global", "_bilibili"])
                {
                    GameBiz biz = game + suffix;
                    if (biz.IsKnown() || gameInfos.FirstOrDefault(x => x.GameBiz == biz) is GameInfo info)
                    {
                        item.Servers.Add(new GameBizIcon(biz));
                    }
                }
            }
        }
        catch { }
        return list;
    }



    private void SystemTrayWindow_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (args.WindowActivationState is WindowActivationState.Deactivated)
        {
            Hide();
        }
    }



    [RelayCommand]
    public override void Show()
    {
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        RootGrid.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        SIZE windowSize = new()
        {
            Width = (int)(RootGrid.DesiredSize.Width * UIScale),
            Height = (int)(RootGrid.DesiredSize.Height * UIScale)
        };
        User32.GetCursorPos(out POINT point);
        User32.CalculatePopupWindowPosition(point, windowSize, User32.TrackPopupMenuFlags.TPM_RIGHTALIGN | User32.TrackPopupMenuFlags.TPM_BOTTOMALIGN | User32.TrackPopupMenuFlags.TPM_WORKAREA, null, out RECT windowPos);
        User32.MoveWindow(WindowHandle, windowPos.X, windowPos.Y, windowPos.Width, windowPos.Height, true);
        base.Show();
    }



    [RelayCommand]
    public override void Hide()
    {
        base.Hide();
    }



    [RelayCommand]
    public void ShowMainWindow()
    {
        App.Current.EnsureMainWindow();
    }


    [RelayCommand]
    private void Exit()
    {
        App.Current.Exit();
    }


    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        trayIcon?.Dispose();
    }


    private bool CheckGameExited(Process process)
    {
        try
        {
            if (process is null || process.HasExited)
                return false;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private async void Button_StartGame_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button && button.Tag is GameBizIcon icon)
            {
                var process = await _gameLauncherService.StartGameAsync(GameId.FromGameBiz(icon.GameBiz)!)!;
                if (process != null)
                {
                    button.IsEnabled = false;
                    icon.MaskOpacity = 1;
                    if (button.Content is Grid grid && VisualTreeHelper.GetChild(grid, 1) is TextBlock serverName)
                    {
                        serverName.Foreground = Application.Current.Resources["TextFillColorSecondaryBrush"] as SolidColorBrush;
                        WeakReferenceMessenger.Default.Send(new GameStartedMessage());

                        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
                        timer.Tick += (s, args) =>
                        {
                            if (!CheckGameExited(process))
                            {
                                timer.Stop();
                                icon.MaskOpacity = 0;
                                serverName.Foreground = new SolidColorBrush(Colors.White);
                                button.IsEnabled = true;
                            }
                        };
                        timer.Start();
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start game");
        }
    }
}
