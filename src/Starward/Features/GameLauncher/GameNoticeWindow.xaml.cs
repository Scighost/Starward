using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Starward.Core;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Globalization;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using System.Timers;
using Vanara.PInvoke;
using Windows.Graphics;
using Windows.System;


namespace Starward.Features.GameLauncher;

public sealed partial class GameNoticeWindow : WindowEx
{


    private Timer timer;


    private readonly ILogger<GameNoticeWindow> _logger = AppService.GetLogger<GameNoticeWindow>();


    private readonly GameNoticeService _gameNoticeService = AppService.GetService<GameNoticeService>();


    public GameBiz CurrentGameBiz { get; set; }


    public long CurrentUid { get; set; }


    public nint ParentWindowHandle { get; set; }



    public GameNoticeWindow()
    {
        this.InitializeComponent();
        SystemBackdrop = new TransparentBackdrop();
        InitializeWindow();
        Closed += (_, _) => WeakReferenceMessenger.Default.Send(new GameNoticeWindowClosedMessage());
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
        }
        catch { }
    }





    public new void Activate()
    {
        try
        {
            AppWindow? appWindow = AppWindow.GetFromWindowId(new WindowId((ulong)ParentWindowHandle));
            if (appWindow is null)
            {
                Close();
                return;
            }
            var pos = appWindow.Position;
            var size = appWindow.Size;
            int edge = (int)(8 * UIScale);
            AppWindow.MoveAndResize(new RectInt32(pos.X + edge, pos.Y, size.Width - 2 * edge, size.Height - edge));
            User32.SetWindowLong(WindowHandle, User32.WindowLongFlags.GWL_HWNDPARENT, ParentWindowHandle);
        }
        catch { }
        base.Activate();
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
        if (ParentWindowHandle is 0)
        {
            Close();
            return;
        }
        timer = new Timer(5000);
        timer.AutoReset = false;
        timer.Elapsed += (_, _) => DispatcherQueue.TryEnqueue(Close);
        timer.Start();
        await InitializeWebViewAsync();
    }



    private async Task InitializeWebViewAsync()
    {
        try
        {
            string lang = CultureInfo.CurrentUICulture.Name;
            string url = _gameNoticeService.GetGameNoticeUrl(CurrentGameBiz);
            try
            {
                await webview.EnsureCoreWebView2Async();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Initialize WebView2 failed.");
                Close();
                return;
            }
            webview.CoreWebView2.ProcessFailed += (_, _) => Close();
            webview.CoreWebView2.NavigationCompleted += (_, e) => { if (!e.IsSuccess) Close(); };
            webview.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webview.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
            webview.Source = new Uri(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize game notices");
            Close();
        }
    }



    private async void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
    {
        try
        {
            await webview.EnsureCoreWebView2Async();
            string script = $$"""
                document.onkeydown = (e) => {if(e.key === "Escape") chrome.webview.postMessage({ "action": "close" });}
                miHoYoGameJSSDK.closeWebview = () => chrome.webview.postMessage({ "action": "close" });
                miHoYoGameJSSDK.openInBrowser = (url) => chrome.webview.postMessage({ "action": "url", "param": url });
                miHoYoGameJSSDK.openInWebview = (url) => chrome.webview.postMessage({ "action": "url", "param": url });
                
                function RemoveBg() {
                    let root = document.getElementById("root");
                    if (root === null) {
                        window.setTimeout(RemoveBg, 100);
                    } else {
                        root.style.background = "transparent";
                        let home = document.getElementsByClassName("home");
                        let mask = document.getElementsByClassName("home__mask");
                        if (mask.length > 0) {
                            mask[0].remove();
                        } else if (home.length > 0) {
                            home[0].style.background = "transparent";
                        } else {
                            window.setTimeout(RemoveBg, 100);
                        }
                    }
                }

                function modifyAllFontFaces() {
                    for (let sheet of document.styleSheets) {
                        try {
                            for (let i = sheet.cssRules.length - 1; i >= 0; i--) {
                                const rule = sheet.cssRules[i];
                                if (rule instanceof CSSFontFaceRule) {
                                    if (rule.style.fontFamily.includes("bh3")) {
                                        let src = rule.style.getPropertyValue("src").replace("http://127.0.0.1:1221/font", "https://webstatic.mihoyo.com/bh3/upload/announcement/font");
                                        rule.style.setProperty("src", src);
                                    } else if (rule.style.fontFamily.includes("ys")) {
                                        let src = rule.style.getPropertyValue("src").replace("http://127.0.0.1:1221/font", "https://sdk.mihoyo.com/hk4e/fonts");
                                        rule.style.setProperty("src", src);
                                    } else if (rule.style.fontFamily.includes("rpg")) {
                                        let src = rule.style.getPropertyValue("src").replace("http://127.0.0.1:1221/font", "https://sdk.mihoyo.com/hkrpg/fonts");
                                        rule.style.setProperty("src", src);
                                    } else if (rule.style.fontFamily.includes("nap")) {
                                        sheet.deleteRule(i);
                                    }
                                }
                            }
                        } catch (e) { }
                    }
                }

                RemoveBg();
                modifyAllFontFaces();
                """;
            await webview.CoreWebView2.ExecuteScriptAsync(script);
            await Task.Delay(300);
            webview.Visibility = Visibility.Visible;
            timer.Stop();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Add js sdk");
            Close();
        }
    }



    private async void CoreWebView2_WebMessageReceived(CoreWebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var node = JsonNode.Parse(args.WebMessageAsJson);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "web message received");
            Close();
        }
    }




}
