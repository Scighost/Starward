using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Frameworks;
using Starward.Helpers;
using Starward.Messages;
using Starward.Models;
using Starward.Services;
using Starward.Services.Launcher;
using System;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Timers;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class GameNoticesWindow : WindowEx
{




    private readonly ILogger<GameNoticesWindow> _logger = AppConfig.GetLogger<GameNoticesWindow>();


    private readonly LauncherBackgroundService _launcherBackgroundService = AppConfig.GetService<LauncherBackgroundService>();


    private readonly GameAccountService _gameAccountService = AppConfig.GetService<GameAccountService>();



    public GameBiz GameBiz;


    private Timer timer;



    public GameNoticesWindow()
    {
        this.InitializeComponent();
        SystemBackdrop = new TransparentBackdrop();
        InitializeWindow();
        Closed += (_, _) => WeakReferenceMessenger.Default.Send(new GameNoticesWindowClosedMessage());
    }




    private void InitializeWindow()
    {
        try
        {
            Title = "Starward";
            AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
            AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
            AppWindow.TitleBar.SetDragRectangles([new RectInt32(0, 0, 0, 0)]);
            AdaptTitleBarButtonColorToActuallTheme();
            if (AppWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.IsMaximizable = false;
                presenter.IsResizable = false;
                presenter.SetBorderAndTitleBar(false, false);
            }
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE, User32.GetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_STYLE) & ~(nint)User32.WindowStyles.WS_DLGFRAME);
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_HWNDPARENT, MainWindow.Current.WindowHandle);
            var pos = MainWindow.Current.AppWindow.Position;
            var size = MainWindow.Current.AppWindow.Size;
            int edge = (int)(8 * MainWindow.Current.UIScale);
            AppWindow.MoveAndResize(new RectInt32(pos.X + edge, pos.Y, size.Width - 2 * edge, size.Height - edge));
        }
        catch { }
    }




    protected override nint InputSiteSubclassProc(HWND hWnd, uint uMsg, nint wParam, nint lParam, nuint uIdSubclass, nint dwRefData)
    {
        if (uMsg == (uint)User32.WindowMessage.WM_KEYDOWN)
        {

            var key = (VirtualKey)wParam;
            if (key == VirtualKey.Escape)
            {
                Close();
                return 0;
            }
        }
        return base.InputSiteSubclassProc(hWnd, uMsg, wParam, lParam, uIdSubclass, dwRefData);
    }



    private async void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        timer = new Timer(10000);
        timer.AutoReset = false;
        timer.Elapsed += (_, _) => DispatcherQueue.TryEnqueue(Close);
        timer.Start();
        await InitializeWebAsync();
    }



    private async Task InitializeWebAsync()
    {
        try
        {
            long uid = 0;
            try
            {
                var accounts = _gameAccountService.GetGameAccounts(GameBiz);
                if (accounts.FirstOrDefault(x => x.IsLogin) is GameAccount acc)
                {
                    uid = acc.Uid;
                }
            }
            catch { }
            string lang = CultureInfo.CurrentUICulture.Name;
            string url = LauncherClient.GetGameNoticesUrl(GameBiz, uid, lang);
            try
            {
                await webview.EnsureCoreWebView2Async();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ensure core webview2");
            }
            webview.CoreWebView2.DOMContentLoaded += async (_, _) => await InsertBgAsync();
            webview.CoreWebView2.ProcessFailed += (_, _) => Close();
            webview.CoreWebView2.NavigationCompleted += (_, e) => { if (!e.IsSuccess) Close(); };
            webview.CoreWebView2.WebMessageReceived += async (s, e) =>
            {
                var node = JsonNode.Parse(e.WebMessageAsJson);
                string? action = node?["action"]?.ToString();
                string? param = node?["param"]?.ToString();
                if (action is "close")
                {
                    Close();
                }
                if (action is "url")
                {
                    if (Uri.TryCreate(param, UriKind.RelativeOrAbsolute, out var uri))
                    {
                        await Launcher.LaunchUriAsync(uri);
                    }
                }
            };
            webview.Source = new Uri(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize game notices");
        }
    }




    private async Task InsertBgAsync()
    {
        try
        {
            string? bg = null;
            try
            {
                bg = await _launcherBackgroundService.GetBackgroundImageUrlAsync(GameBiz);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Get background image ({gameBiz})", (object)GameBiz);
            }
            await webview.EnsureCoreWebView2Async();
            string script = $$"""
                document.onkeydown = (e) => {if(e.key === "Escape") chrome.webview.postMessage({ "action": "close" });}
                miHoYoGameJSSDK.closeWebview = () => chrome.webview.postMessage({ "action": "close" });
                miHoYoGameJSSDK.openInBrowser = (url) => chrome.webview.postMessage({ "action": "url", "param": url });
                miHoYoGameJSSDK.openInWebview = (url) => chrome.webview.postMessage({ "action": "url", "param": url });
                function InsertBg() {
                    let root = document.getElementById("root");
                    if (root === null) {
                        window.setTimeout(InsertBg, 100);
                    } else {
                        root.style.background = "transparent";
                        let home = document.getElementsByClassName("home");
                        let mask = document.getElementsByClassName("home__mask");
                        if (mask.length > 0) {
                            mask[0].remove();
                        } else if (home.length > 0) {
                            home[0].style.background = "transparent";
                        } else {
                            window.setTimeout(InsertBg, 100);
                        }
                    }
                }
                InsertBg();
                """;
            await webview.CoreWebView2.ExecuteScriptAsync(script);
            await Task.Delay(300);
            webview.Visibility = Visibility.Visible;
            timer.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Insert bg");
        }
    }





}
