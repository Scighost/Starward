using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using Starward.Core;
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


    public InstallGameSystemTrayPage InstallGameSystemTrayPage;




    public void Initialize(GameBiz gameBiz = GameBiz.None)
    {
        try
        {
            if (TaskbarIcon is not null)
            {
                return;
            }
            if (gameBiz is GameBiz.None && !AppInstance.FindOrRegisterForKey("").IsCurrent)
            {
                return;
            }
            TaskbarIcon = new TaskbarIcon
            {
                IconSource = new BitmapImage(new Uri($"ms-appx:///Assets/logo{(gameBiz is GameBiz.None ? "" : "_dl")}.ico")),
                ToolTipText = "Starward",
                NoLeftClickDelay = true,
                LeftClickCommand = ShowMainWindowCommand,
                RightClickCommand = OpenContextMenuCommand,
                Id = TrayIcon.CreateUniqueGuidFromString(gameBiz.ToString()),
            };
            TaskbarIcon.ForceCreate(false);
            TrayIcon = typeof(TaskbarIcon).GetProperty("TrayIcon", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(TaskbarIcon) as TrayIcon;
            if (gameBiz is GameBiz.None)
            {
                TrayWindow = new SystemTrayWindow(new MainMenuSystemTrayPage());
            }
            else
            {
                InstallGameSystemTrayPage = new InstallGameSystemTrayPage(gameBiz);
                TrayWindow = new SystemTrayWindow(InstallGameSystemTrayPage);
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
