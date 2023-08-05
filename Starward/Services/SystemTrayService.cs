using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using Starward.Pages.SystemTray;
using System;
using System.Reflection;
using Vanara.PInvoke;

namespace Starward.Services;

internal partial class SystemTrayService : ObservableObject, IDisposable
{



    private readonly ILogger<SystemTrayService> _logger;

    public SystemTrayService(ILogger<SystemTrayService> logger)
    {
        _logger = logger;
    }



    public bool IsCreated => TrayIcon?.IsCreated ?? false;


    private TrayIcon? TrayIcon;

    private TaskbarIcon? TaskbarIcon;

    private SystemTrayWindow? TrayWindow;



    public void Initialize(bool dlIcon = false)
    {
        try
        {
            if (TaskbarIcon is not null)
            {
                return;
            }
            var instance = AppInstance.FindOrRegisterForKey("");
            if (!instance.IsCurrent)
            {
                return;
            }
            TaskbarIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(new Uri($"ms-appx:///Assets/logo{(dlIcon ? "_dl" : "")}.ico")),
                ToolTipText = "Starward",
                NoLeftClickDelay = true,
                LeftClickCommand = ShowMainWindowCommand,
                RightClickCommand = OpenContextMenuCommand,
            };
            TaskbarIcon.ForceCreate(false);
            TrayIcon = typeof(TaskbarIcon).GetProperty("TrayIcon", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(TaskbarIcon) as TrayIcon;
            if (dlIcon)
            {
                TrayWindow = new SystemTrayWindow(new InstallGameSystemTrayPage());
            }
            else
            {
                TrayWindow = new SystemTrayWindow(new MainMenuSystemTrayPage());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize system tray");
        }
    }




    [RelayCommand]
    private void OpenContextMenu()
    {
        TrayWindow ??= new SystemTrayWindow();
        TrayWindow.Show();
    }



    [RelayCommand]
    private void ShowMainWindow()
    {
        User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWNORMAL);
        User32.SetForegroundWindow(MainWindow.Current.HWND);
    }



    public void HideTrayWindow()
    {
        TrayWindow?.Hide();
    }



    public void Dispose()
    {
        TrayIcon?.Dispose();
        TaskbarIcon?.Dispose();
        TrayWindow?.Dispose();
        TrayIcon = null;
        TaskbarIcon = null;
        TrayWindow = null;
        AppInstance.GetCurrent().UnregisterKey();
    }






}
