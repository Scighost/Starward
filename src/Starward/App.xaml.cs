using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Configuration;
using Microsoft.UI;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Windows.AppLifecycle;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.IO;
using Windows.UI;


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
            if (args[1].ToLower() is "download" or "repair" or "reinstall")
            {
                //m_MainWindow = new InstallGameWindow();
                //m_MainWindow.Activate();
                return;
            }
            if (Uri.TryCreate(args[1], UriKind.Absolute, out Uri? uri))
            {
                if (uri.Host is "test")
                {
                    //m_MainWindow = new TestUrlProtocolWindow();
                    //m_MainWindow.Activate();
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
        if (AppConfig.Configuration.GetValue<bool>("hide"))
        {
            m_SystemTrayWindow = new Features.ViewHost.SystemTrayWindow();
        }
        else
        {
            m_MainWindow = new Features.ViewHost.MainWindow();
            m_MainWindow.Activate();
        }
    }



    private AppInstance instance;

    private Features.ViewHost.MainWindow m_MainWindow;

    private Features.ViewHost.SystemTrayWindow m_SystemTrayWindow;


    public void SwitchMainWindow(WindowEx window)
    {
        // to delete
    }


    public void EnsureMainWindow()
    {
        m_MainWindow ??= new Features.ViewHost.MainWindow();
        m_MainWindow.Activate();
        m_MainWindow.Show();
    }


    public void EnsureSystemTray()
    {
        m_SystemTrayWindow ??= new Features.ViewHost.SystemTrayWindow();
    }



    public void CloseMainWindow()
    {
        // to delete
    }



    public void CloseSystemTray()
    {
        // to delete
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
        WindowManager.CloseAll();
        Application.Current.Exit();
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
