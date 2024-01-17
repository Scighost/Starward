using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Pages.Welcome;
using Starward.Services;
using System;
using System.IO;
using Vanara.PInvoke;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WelcomeWindow : WindowEx
{


    public static new WelcomeWindow Current { get; private set; }


    private readonly DatabaseService _databaseService = AppConfig.GetService<DatabaseService>();


    public WelcomeWindow()
    {
        Current = this;
        this.InitializeComponent();
        InitializeWindow();
    }


    private void InitializeWindow()
    {
        Title = "Starward";
        //new SystemBackdropHelper(this, SystemBackdropProperty.AcrylicDefault with
        //{
        //    TintColorLight = 0xFFE7E7E7,
        //    TintColorDark = 0xFF303030
        //}).TrySetAcrylic(true);
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.HideIconAndSystemMenu;
        ChangeWindowSize(AppConfig.WindowSizeMode);
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 10000, (int)(48 * UIScale)));
        AppWindow.SetIcon(Path.Combine(AppContext.BaseDirectory, @"Assets\logo.ico"));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
        frame.Navigate(typeof(SelectLanguagePage));
    }



    private void WindowEx_Closed(object sender, WindowEventArgs args)
    {
        Current = null!;
    }



    public void NavigateTo(Type page, object parameter, NavigationTransitionInfo infoOverride)
    {
        frame.Navigate(page, parameter, infoOverride);
    }



    public string TextLanguage { get; set; }

    public int WindowSizeMode { get; set; }

    public string UserDataFolder { get; set; }


    public void ChangeWindowSize(int mode)
    {
        WindowSizeMode = mode == 0 ? 0 : 1;
        if (mode == 0)
        {
            WindowSizeMode = 0;
            CenterInScreen(1280, 768);
        }
        else
        {
            WindowSizeMode = 1;
            CenterInScreen(1064, 648);
        }
    }



    public void Reset()
    {
        TextLanguage = null!;
        WindowSizeMode = 0;
        UserDataFolder = null!;
    }



    public void ApplySetting()
    {
        _databaseService.SetDatabase(UserDataFolder);
        AppConfig.UserDataFolder = UserDataFolder;
        AppConfig.Language = TextLanguage;
        AppConfig.WindowSizeMode = WindowSizeMode;
        AppConfig.SaveConfiguration();
    }



    public void OpenMainWindow()
    {
        App.Current.SwitchMainWindow(new MainWindow());
    }




    public override nint WindowSubclassProc(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_SYSCOMMAND)
        {
            // SC_MAXIMIZE
            if (wParam == 0xF030)
            {
                return IntPtr.Zero;
            }
        }
        return base.WindowSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }




}
