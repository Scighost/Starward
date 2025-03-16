using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Starward.Features.UrlProtocol;
using Starward.Features.ViewHost;
using Starward.Frameworks;
using System;
using System.IO;


namespace Starward;

public partial class App : Application
{

    private readonly DispatcherQueue _uiDispatcherQueue;


    public static new App Current => (App)Application.Current;


    public App()
    {
        this.InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
        _uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UnhandledException += App_UnhandledException;
        _ = AppSetting.Language;
    }


    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var folder = Path.Combine(AppSetting.CacheFolder, "crash");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(file, e.Exception.ToString());
    }


    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        instance = AppInstance.GetCurrent();
        instance.Activated += AppInstance_Activated;
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            if (Uri.TryCreate(args[1], UriKind.Absolute, out Uri? uri))
            {
                if (uri.Host is "test")
                {
                    new TestUrlProtocolWindow().Activate();
                    return;
                }
            }
        }
        var main = AppInstance.FindOrRegisterForKey("main");
        if (!main.IsCurrent)
        {
            await main.RedirectActivationToAsync(instance.GetActivatedEventArgs());
            this.Exit();
            return;
        }
        if (Environment.GetCommandLineArgs().Contains("--hide"))
        {
            m_SystemTrayWindow = new SystemTrayWindow();
        }
        else
        {
            m_MainWindow = new MainWindow();
            m_MainWindow.Activate();
        }
    }



    private AppInstance instance;

    private MainWindow m_MainWindow;

    private SystemTrayWindow m_SystemTrayWindow;



    public void EnsureMainWindow()
    {
        m_MainWindow ??= new MainWindow();
        m_MainWindow.Activate();
        m_MainWindow.Show();
    }


    public void EnsureSystemTray()
    {
        m_SystemTrayWindow ??= new SystemTrayWindow();
    }



    private void AppInstance_Activated(object? sender, AppActivationArguments e)
    {
        _uiDispatcherQueue.TryEnqueue(EnsureMainWindow);
    }



    public static AppInstance? FindInstanceForKey(string key)
    {
        foreach (var item in AppInstance.GetInstances())
        {
            if (item.Key == key)
            {
                return item;
            }
        }
        return null;
    }



    public new void Exit()
    {
        m_MainWindow?.Close();
        m_SystemTrayWindow?.Close();
        Application.Current.Exit();
    }



}
