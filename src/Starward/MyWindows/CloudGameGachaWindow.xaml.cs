using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Starward.Core;
using Starward.Frameworks;
using Starward.Messages;
using System;
using System.Text.Json.Nodes;
using System.Web;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class CloudGameGachaWindow : WindowEx
{


    private const string SPAN_WEB_PREFIX_YS_CN = "https://webstatic.mihoyo.com/hk4e/event/e20190909gacha-v3/index.html";

    private const string SPAN_WEB_PREFIX_SR_CN = "https://webstatic.mihoyo.com/hkrpg/event/e20211215gacha-v2/index.html";



    public GameBiz GameBiz { get; set; }




    public CloudGameGachaWindow()
    {
        this.InitializeComponent();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        if (ShouldSystemUseDarkMode())
        {
            RootGrid.RequestedTheme = ElementTheme.Dark;
        }
        else
        {
            RootGrid.RequestedTheme = ElementTheme.Light;
        }
        AdaptTitleBarButtonColorToActuallTheme();
    }



    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // todo WinAppSDK 升级到 1.5 后，为云游戏的网页缓存设置单独的文件夹
            if (GameBiz.ToGame() == GameBiz.hk4e)
            {
                webview.Source = new Uri("https://ys.mihoyo.com/cloud/");
            }
            if (GameBiz.ToGame() == GameBiz.hkrpg)
            {
                webview.Source = new Uri("https://sr.mihoyo.com/cloud/");
            }
        }
        catch (Exception)
        {

        }

    }





    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await webview.EnsureCoreWebView2Async();
            string result = await webview.CoreWebView2.ExecuteScriptAsync("document.documentElement.outerHTML");
            string? html = JsonNode.Parse(result)?.ToString();
            if (!string.IsNullOrWhiteSpace(html))
            {
                string? url = GetMatchUrl(GameBiz, html);
                if (!string.IsNullOrWhiteSpace(url))
                {
                    url = HttpUtility.HtmlDecode(url);
                    WeakReferenceMessenger.Default.Send(new UpdateGachaLogMessage(url));
                    TextBlock_Info.Visibility = Visibility.Visible;
                    TextBlock_Error.Visibility = Visibility.Collapsed;
                    return;
                }
            }
            TextBlock_Info.Visibility = Visibility.Collapsed;
            TextBlock_Error.Visibility = Visibility.Visible;
            TextBlock_Error.Text = Lang.GachaLogPage_CannotFindURL;
        }
        catch (Exception ex)
        {
            TextBlock_Info.Visibility = Visibility.Collapsed;
            TextBlock_Error.Visibility = Visibility.Visible;
            TextBlock_Error.Text = ex.Message;
        }
    }




    private static string? GetMatchUrl(GameBiz gameBiz, string html)
    {
        string? prefix = gameBiz.ToGame().Value switch
        {
            GameBiz.hk4e => SPAN_WEB_PREFIX_YS_CN,
            GameBiz.hkrpg => SPAN_WEB_PREFIX_SR_CN,
            _ => null
        };
        if (prefix is not null)
        {
            ReadOnlySpan<char> span = html.AsSpan();
            int index = span.LastIndexOf(prefix);
            if (index >= 0)
            {
                var length = span[index..].IndexOfAny("\"");
                return new(span.Slice(index, length));
            }
        }
        return null;
    }




}
