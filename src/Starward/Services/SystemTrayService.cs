using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using H.NotifyIcon;
using H.NotifyIcon.Core;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Pages.SystemTray;
using System;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Vanara.PInvoke;

namespace Starward.Services;

internal partial class SystemTrayService : ObservableObject, IDisposable
{



    private readonly ILogger<SystemTrayService> _logger;

    public SystemTrayService(ILogger<SystemTrayService> logger)
    {
        _logger = logger;
    }


    private Mutex? _mutex;


    public bool IsCreated => TrayIcon?.IsCreated ?? false;


    private TrayIcon? TrayIcon;

    private TaskbarIcon? TaskbarIcon;

    private SystemTrayWindow? TrayWindow;


    public InstallGameSystemTrayPage InstallGameSystemTrayPage;




    public void Initialize(GameBiz gameBiz = GameBiz.None)
    {
        var sync = GetSyncMutex();
        try
        {
            if (TaskbarIcon is not null)
            {
                return;
            }
            sync.WaitOne(1000);
            if (gameBiz is GameBiz.None && IsSignalMutexExisting())
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
                _mutex = GetSignalMutex();
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
        finally
        {
            sync.ReleaseMutex();
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
        _mutex?.Dispose();
        TrayIcon = null;
        TaskbarIcon = null;
        TrayWindow = null;
        _mutex = null;
    }






    public static Mutex GetSyncMutex()
    {
        string name = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory + "sync")));
        return new Mutex(false, name);
    }



    public static Mutex GetSignalMutex()
    {
        string name = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory + "signal")));
        return new Mutex(false, name);
    }


    public static bool IsSignalMutexExisting()
    {
        string name = Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(AppContext.BaseDirectory + "signal")));
        return Mutex.TryOpenExisting(name, out _);
    }



}
