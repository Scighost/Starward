using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
        await InitializeAsync();
    }




    private async Task InitializeAsync()
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
            webview.Source = new Uri(url);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize game notices");
        }
    }




}
