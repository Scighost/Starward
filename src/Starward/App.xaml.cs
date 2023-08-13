// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Starward.Helpers;
using Starward.Services;
using System;
using System.Globalization;
using System.IO;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
        UnhandledException += App_UnhandledException;
        InitializeConsoleOutput();
        InitializeLanguage();
    }

    private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
        var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward", "crash");
        Directory.CreateDirectory(folder);
        var file = Path.Combine(folder, $"crash_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
        File.WriteAllText(file, e.Exception.ToString());
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs _)
    {
        instance = AppInstance.GetCurrent();
        instance.Activated += AppInstance_Activated;
        var args = Environment.GetCommandLineArgs();
        if (args.Length > 1)
        {
            if (args[1].ToLower() is "download" or "repair")
            {
                m_window = new MainWindow("download");
                m_window.Activate();
                return;
            }
        }
        var sync = SystemTrayService.GetSyncMutex();
        sync.WaitOne();
        if (SystemTrayService.IsSignalMutexExisting())
        {
            var instances = AppInstance.GetInstances();
            var arg = instance.GetActivatedEventArgs();
            foreach (var item in instances)
            {
                if (item.Key.StartsWith("main"))
                {
                    await item.RedirectActivationToAsync(arg);
                }
            }
            sync.ReleaseMutex();
            this.Exit();
        }
        else
        {
            sync.ReleaseMutex();
            AppInstance.FindOrRegisterForKey($"main_{Environment.ProcessId}");
            m_window = new MainWindow();
            m_window.Activate();
        }
    }



    private AppInstance instance;

    private Window m_window;


    private void AppInstance_Activated(object? sender, AppActivationArguments e)
    {
        User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWNORMAL);
        User32.SetForegroundWindow(MainWindow.Current.HWND);
    }



    private void InitializeConsoleOutput()
    {
        try
        {
            if (AppConfig.EnableConsole)
            {
                ConsoleHelper.Alloc();
                ConsoleHelper.Show();
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"Welcome to Starward v{AppConfig.AppVersion}");
                Console.WriteLine(DateTimeOffset.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                Console.WriteLine(Environment.CommandLine);
                Console.WriteLine();
                Console.ResetColor();
            }
        }
        catch { }
    }



    private void InitializeLanguage()
    {
        try
        {
            var lang = AppConfig.Language;
            Console.WriteLine($"Language is {lang}");
            if (!string.IsNullOrWhiteSpace(lang))
            {
                CultureInfo.CurrentUICulture = new CultureInfo(lang);
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




}
