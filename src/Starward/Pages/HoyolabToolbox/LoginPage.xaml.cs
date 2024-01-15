using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.Web.WebView2.Core;
using Starward.Core;
using Starward.Messages;
using Starward.Services;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class LoginPage : PageBase
{

    private const string URL_CN = "https://www.miyoushe.com/";
    private const string URL_OS = "https://www.hoyolab.com/home";


    private const string RefreshIcon = "\uE72C";
    private const string CancelIcon = "\uE711";


    private readonly ILogger<LoginPage> _logger = AppConfig.GetLogger<LoginPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();


    public LoginPage()
    {
        this.InitializeComponent();
    }





    private string GetGameBizUrl()
    {
        if (CurrentGameBiz.IsChinaServer())
        {
            return CurrentGameBiz.ToGame() switch
            {
                GameBiz.GenshinImpact => $"{URL_CN}ys",
                GameBiz.StarRail => $"{URL_CN}sr",
                GameBiz.Honkai3rd => $"{URL_CN}bh3",
                _ => URL_CN,
            };
        }
        else
        {
            return URL_OS;
        }
    }





    protected override async void OnLoaded()
    {
        await InitializeCoreWebView();
    }



    private async Task InitializeCoreWebView()
    {
        try
        {
            await webview.EnsureCoreWebView2Async();
            webview.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            var manager = webview.CoreWebView2.CookieManager;
            var url = GetGameBizUrl();
            var cookies = await manager.GetCookiesAsync(url);
            foreach (var item in cookies)
            {
                manager.DeleteCookie(item);
            }
            webview.CoreWebView2.Navigate(url);
            webview.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
            webview.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
            webview.CoreWebView2.SourceChanged += CoreWebView2_SourceChanged;
            webview.CoreWebView2.HistoryChanged += CoreWebView2_HistoryChanged;
            FlyoutBase.ShowAttachedFlyout(Button_Finish);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize webview");
        }
    }



    private void CoreWebView2_NavigationStarting(CoreWebView2 sender, CoreWebView2NavigationStartingEventArgs args)
    {
        try
        {
            FontIcon_RefreshOrCancel.Glyph = CancelIcon;
        }
        catch { }
    }

    private async void CoreWebView2_NavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
    {
        try
        {
            FontIcon_RefreshOrCancel.Glyph = RefreshIcon;
            if (args.IsSuccess)
            {
                await sender.ExecuteScriptAsync("""
                    if (window.location.host == 'www.miyoushe.com') {
                        var openLoginDialogIntervalId = setInterval(openLoginDialog, 100);
                        function openLoginDialog() {
                            var ele = document.getElementsByClassName('header__avatarwrp');
                            if (ele.length > 0) {
                                clearInterval(openLoginDialogIntervalId);
                                openLoginDialogIntervalId = null;
                                ele[0].click();
                            }
                        }
                    }
                    if (window.location.host == 'www.hoyolab.com') {
                        var openLoginDialogIntervalId = setInterval(openLoginDialog, 100);
                        function openLoginDialog() {
                            var ele = document.getElementsByClassName('header-avatar');
                            if (ele.length > 0) {
                                clearInterval(openLoginDialogIntervalId);
                                openLoginDialogIntervalId = null;
                                ele[0].click();
                            }
                        }
                    }
                    """);
            }
        }
        catch { }
    }

    private void CoreWebView2_SourceChanged(CoreWebView2 sender, CoreWebView2SourceChangedEventArgs args)
    {
        try
        {
            TextBox_Uri.Text = sender.Source;
        }
        catch { }
    }



    private void TextBox_Uri_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            if (e.Key == Windows.System.VirtualKey.Enter)
            {
                var url = TextBox_Uri.Text;
                if (!(url.StartsWith("https://") || url.StartsWith("http://")))
                {
                    url = $"https://{url}";
                }

                webview.CoreWebView2.Navigate(url);
                TextBox_Uri.Text = url;
            }
        }
        catch { }
    }

    private void CoreWebView2_HistoryChanged(CoreWebView2 sender, object args)
    {
        try
        {
            Button_GoBack.IsEnabled = sender.CanGoBack;
            Button_GoForward.IsEnabled = sender.CanGoForward;
        }
        catch { }
    }


    private void Button_GoBack_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (webview.CoreWebView2?.CanGoBack ?? false)
            {
                webview.CoreWebView2.GoBack();
            }
        }
        catch { }
    }


    private void Button_GoForward_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (webview.CoreWebView2?.CanGoForward ?? false)
            {
                webview.CoreWebView2.GoForward();
            }
        }
        catch { }
    }


    private void Button_RefreshOrCancel_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (FontIcon_RefreshOrCancel.Glyph is RefreshIcon)
            {
                webview.CoreWebView2?.Reload();
            }
            else
            {
                webview.CoreWebView2?.Stop();
            }
        }
        catch { }
    }



    private async void Button_Finish_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Button_Finish.IsEnabled = false;
            var manager = webview.CoreWebView2.CookieManager;
            var cookies = await manager.GetCookiesAsync(GetGameBizUrl());
            var cookieString = string.Join(";", cookies.Select(x => $"{x.Name}={x.Value}"));
            var user = await _gameRecordService.AddRecordUserAsync(cookieString);
            var roles = await _gameRecordService.AddGameRolesAsync(cookieString);
            WeakReferenceMessenger.Default.Send(new GameRecordRoleChangedMessage(roles.FirstOrDefault(x => x.GameBiz == CurrentGameBiz.ToString())));
            TextBlock_Tip.Text = string.Format(Lang.LoginPage_AlreadyAddedGameRoles, roles.Count, string.Join("\r\n", roles.Select(x => $"{x.Nickname}  {x.Uid}")));
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Log in (web mode)");
            TextBlock_Tip.Text = $"{Lang.Common_AccountError}\r\n{ex.Message}";
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Log in (web mode)");
            TextBlock_Tip.Text = ex.Message;
            TextBlock_Tip.Text = $"{Lang.Common_NetworkError}\r\n{ex.Message}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Log in (web mode)");
            TextBlock_Tip.Text = $"{ex.GetType().Name}\r\n{ex.Message}";
        }
        finally
        {
            FlyoutBase.ShowAttachedFlyout(Button_Finish);
            Button_Finish.IsEnabled = true;
        }
    }



}
