using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Web.WebView2.Core;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Models;
using Starward.Services;
using System;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class GameNoticesPage : Page
{


    private readonly ILogger<GameNoticesPage> _logger = AppConfig.GetLogger<GameNoticesPage>();


    private readonly GameService _gameService = AppConfig.GetService<GameService>();


    private readonly LauncherService _launcherService = AppConfig.GetService<LauncherService>();


    public GameNoticesPage()
    {
        this.InitializeComponent();
    }



    private GameBiz gameBiz;


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameBiz biz)
        {
            gameBiz = biz;
        }
    }




    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(16);
        InitializeBgAsync();
        await InitializeWebAsync();
    }



    private async void InitializeBgAsync()
    {
        try
        {
            string? bg = await _launcherService.GetBackgroundImageAsync(gameBiz, disableCustom: true);
            if (Uri.TryCreate(bg, UriKind.RelativeOrAbsolute, out var uri))
            {
                Image_Bg.Source = new BitmapImage(uri);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize bg");
        }
    }




    private async Task InitializeWebAsync()
    {
        try
        {
            long uid = 0;
            try
            {
                var accounts = _gameService.GetGameAccounts(gameBiz);
                if (accounts.FirstOrDefault(x => x.IsLogin) is GameAccount acc)
                {
                    uid = acc.Uid;
                }
            }
            catch { }
            string lang = CultureInfo.CurrentUICulture.Name;
            string url = LauncherClient.GetGameNoticesUrl(gameBiz, uid, lang);
            try
            {
                await webview.EnsureCoreWebView2Async();
                webview.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ensure core webview2");
            }
            webview.CoreWebView2.DOMContentLoaded += async (_, _) => await InsertBgAsync();
            webview.CoreWebView2.WebMessageReceived += (s, e) =>
            {
                string message = e.TryGetWebMessageAsString();
                if (message is "close")
                {
                    MainPage.Current.NavigateTo(typeof(LauncherPage));
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
            var content = await _launcherService.GetLauncherContentAsync(gameBiz);
            string bg = content.BackgroundImage.Background;
            await webview.EnsureCoreWebView2Async();
            string script = $$"""
                function InsertBg() {
                    let root = document.getElementById("root");
                    if (root === null) {
                        window.setTimeout(InsertBg, 100);
                    } else {
                        root.style.backgroundImage = "url('{{bg}}')";
                        root.style.backgroundRepeat = "no-repeat";
                        root.style.backgroundPosition = "center";
                        root.style.backgroundSize = "cover";
                        let home = document.getElementsByClassName("home");
                        let mask = document.getElementsByClassName("home__mask");
                        if (mask.length > 0) {
                            mask[0].remove();
                        } else if (home.length > 0) {
                            home[0].style.background = "transparent";
                        } else {
                            window.setTimeout(InsertBg, 100);
                        }
                        let close = document.getElementsByClassName("home__close");
                        if (close.length > 0) {
                            close[0].addEventListener("click", (e) => { window.chrome.webview.postMessage("close") });
                        }
                        close = document.getElementsByClassName("innerann__close");
                        if (close.length > 0) {
                            close[0].addEventListener("click", (e) => { window.chrome.webview.postMessage("close") });
                        }
                    }
                }
                InsertBg();
                """;
            await webview.CoreWebView2.ExecuteScriptAsync(script);
            await Task.Delay(100);
            webview.Opacity = 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Insert bg");
        }
    }



}
