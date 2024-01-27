// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using System;
using System.Globalization;
using System.IO;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward;

/// <summary>
/// Provides application-specific behavior to supplement the default Application class.
/// </summary>
public partial class App : Application
{

    public static new App Current => (App)Application.Current;


    /// <summary>
    /// Initializes the singleton application object.  This is the first line of authored code
    /// executed, and as such is the logical equivalent of main() or WinMain().
    /// </summary>
    public App()
    {
        this.InitializeComponent();
        RequestedTheme = ApplicationTheme.Dark;
        UnhandledException += App_UnhandledException;
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
            if (args[1].ToLower() is "download" or "repair" or "reinstall")
            {
                m_window = new InstallGameWindow();
                m_window.Activate();
                return;
            }
            if (Uri.TryCreate(args[1], UriKind.Absolute, out Uri? uri))
            {
                if (uri.Host is "test")
                {
                    m_window = new TestUrlProtocolWindow();
                    m_window.Activate();
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
        if (AppConfig.UserDataFolder is null)
        {
            m_window = new WelcomeWindow();
            m_window.Activate();
        }
        else
        {
            if (AppConfig.Configuration.GetValue<bool>("hide"))
            {
                m_SystemTrayWindow = new SystemTrayWindow();
            }
            else
            {
                m_window = new MainWindow();
                m_window.Activate();
            }
        }
    }



    private AppInstance instance;

    private WindowEx m_window;

    private SystemTrayWindow m_SystemTrayWindow;


    public void SwitchMainWindow(WindowEx window)
    {
        var old = m_window;
        m_window = window;
        m_window.Activate();
        old?.Close();
    }


    public void EnsureMainWindow()
    {
        m_window ??= new MainWindow();
        m_window.Activate();
        m_window.Show();
    }


    public void CloseMainWindow()
    {
        m_window?.Close();
        m_window = null!;
    }


    public void InitializeSystemTray()
    {
        m_SystemTrayWindow ??= new SystemTrayWindow();
    }


    public void CloseSystemTray()
    {
        m_SystemTrayWindow?.Close();
        m_SystemTrayWindow = null!;
    }



    private void AppInstance_Activated(object? sender, AppActivationArguments e)
    {
        if (m_window is null)
        {
            m_SystemTrayWindow?.DispatcherQueue.TryEnqueue(() =>
            {
                if (AppConfig.UserDataFolder is null)
                {
                    m_window = new WelcomeWindow();
                }
                else
                {
                    m_window = new MainWindow();
                }
                m_window.Activate();
            });
        }
        else
        {
            m_window.DispatcherQueue.TryEnqueue(() =>
            {
                m_window.Show();
            });
        }
    }




    private void InitializeLanguage()
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(AppConfig.UserDataFolder))
            {
                var lang = AppConfig.Language;
                if (!string.IsNullOrWhiteSpace(lang))
                {
                    CultureInfo.CurrentUICulture = new CultureInfo(lang);
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



    #region Accent Color



    private static Color ColorMix(Color input, Color blend, double percent)
    {
        return Color.FromArgb(255,
                              (byte)(input.R * percent + blend.R * (1 - percent)),
                              (byte)(input.G * percent + blend.G * (1 - percent)),
                              (byte)(input.B * percent + blend.B * (1 - percent)));
    }



    public static void ChangeAccentColor(ElementTheme theme, Color? backColor = null, Color? foreColor = null)
    {
        try
        {
            var colors = new Color[14];
            if (backColor != null && foreColor != null)
            {
                colors[0] = backColor.Value;
                for (int i = 1; i < 4; i++)
                {
                    double percent = 1 - 0.2 * i;
                    colors[i] = ColorMix(backColor.Value, Colors.White, percent);
                }
                for (int i = 4; i < 7; i++)
                {
                    double percent = 1 - 0.2 * (i - 3);
                    colors[i] = ColorMix(backColor.Value, Colors.Black, percent);
                }

                colors[7] = foreColor.Value;
                for (int i = 8; i < 11; i++)
                {
                    double percent = 1 - 0.2 * (i - 7);
                    colors[i] = ColorMix(foreColor.Value, Colors.White, percent);
                }
                for (int i = 11; i < 14; i++)
                {
                    double percent = 1 - 0.2 * (i - 10);
                    colors[i] = ColorMix(foreColor.Value, Colors.Black, percent);
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
            if (theme is ElementTheme.Dark)
            {
                Application.Current.Resources["SystemAccentColor"] = colors[0];
                Application.Current.Resources["SystemAccentColorLight1"] = colors[1];
                Application.Current.Resources["SystemAccentColorLight2"] = colors[2];
                Application.Current.Resources["SystemAccentColorLight3"] = colors[3];
                Application.Current.Resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(colors[10]);
                Application.Current.Resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(colors[10]);
                Application.Current.Resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(colors[9]);
            }
            if (theme is ElementTheme.Light)
            {
                Application.Current.Resources["SystemAccentColor"] = colors[0];
                Application.Current.Resources["SystemAccentColorDark1"] = colors[4];
                Application.Current.Resources["SystemAccentColorDark2"] = colors[5];
                Application.Current.Resources["SystemAccentColorDark3"] = colors[6];
                Application.Current.Resources["AccentTextFillColorPrimaryBrush"] = new SolidColorBrush(colors[12]);
                Application.Current.Resources["AccentTextFillColorSecondaryBrush"] = new SolidColorBrush(colors[13]);
                Application.Current.Resources["AccentTextFillColorTertiaryBrush"] = new SolidColorBrush(colors[11]);
            }
        }
        catch { }
    }


    #endregion



}
