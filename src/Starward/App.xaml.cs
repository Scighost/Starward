using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Starward.Features.Database;
using Starward.Features.GamepadControl;
using Starward.Features.Passport;
using Starward.Features.ViewHost;
using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using System.Timers;
using Vanara.PInvoke;
using Windows.ApplicationModel.Activation;


namespace Starward;

public partial class App : Application
{

    private readonly DispatcherQueue _uiDispatcherQueue;

    private readonly Timer _gcTimer = new(TimeSpan.FromSeconds(60));

    public static new App Current => (App)Application.Current;


    public App()
    {
        this.InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
        _uiDispatcherQueue = DispatcherQueue.GetForCurrentThread();
        UnhandledException += App_UnhandledException;
        _gcTimer.Elapsed += (_, _) => GC.Collect();
        _gcTimer.Start();
    }


    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        string logFile = AppConfig.LogFile;
        if (string.IsNullOrWhiteSpace(logFile))
        {
            string logFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward", "log");
            Directory.CreateDirectory(logFolder);
            logFile = Path.Combine(logFolder, $"Starward_{DateTime.Now:yyMMdd}.log");
        }
        var sb = new StringBuilder();
        sb.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] App Crash:");
        sb.AppendLine(e.Exception.ToString());
        if (e.Exception.Data.Count > 0)
        {
            foreach (DictionaryEntry item in e.Exception.Data)
            {
                sb.AppendLine($"{item.Key}: {item.Value}");
            }
        }
        using var fs = File.Open(logFile, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        using var sw = new StreamWriter(fs);
        sw.Write(sb);
    }


    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        string[] args = Environment.GetCommandLineArgs().Skip(1).ToArray();

        // Release builds can use the generated XAML entry point instead of Program.Main.
        // Handle the launch helper before initializing the UI, database or single-instance routing.
        if (args.Length > 0 && args[0] == Features.Hypergryph.EndfieldLauncher.Command)
        {
            int exitCode = await Task.Run(() => Features.Hypergryph.EndfieldLauncher.RunHelper(args));
            Environment.Exit(exitCode);
            return;
        }


        if (AppConfig.UserDataFolder is null)
        {
            if (await new WelcomeWindow().WaitAsync())
            {
                AppConfig.LoadConfiguration();
            }
            else
            {
                Environment.Exit(0);
            }
        }

        instance = AppInstance.FindOrRegisterForKey("Starward.Main");
        if (!instance.IsCurrent)
        {
            await instance.RedirectActivationToAsync(instance.GetActivatedEventArgs());
            Environment.Exit(0);
        }
        instance = AppInstance.GetCurrent();
        instance.Activated += AppInstance_Activated;

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
        try
        {
            _uiDispatcherQueue.TryEnqueue(EnsureMainWindow);
            if (e.Data is ILaunchActivatedEventArgs { Arguments: string arguments })
            {
                string?[] args = Shell32.CommandLineToArgvW(arguments);
                if (args.Length > 1)
                {
                    string? url = args[1];
                    if (url?.StartsWith("starward-hyp-login://") ?? false)
                    {
                        WeakReferenceMessenger.Default.Send(new HypThirdPartyLoginMessage(url));
                    }
                }
            }
        }
        catch { }
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



    public async void Exit(bool quickExit = false)
    {
        m_MainWindow?.Close();
        m_SystemTrayWindow?.Close();
        GamepadController.RestoreGamepadGuideButtonForGameBar();
        if (!quickExit)
        {
            try
            {
                var timer = new Timer(30_000);
                timer.Elapsed += (_, _) => Environment.Exit(0);
                timer.Start();
                AppInstance.GetCurrent().UnregisterKey();
                DatabaseService.AutoBackupToAppDataLocal();
            }
            catch { }
        }
        Application.Current.Exit();
    }



}
