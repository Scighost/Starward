using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using NuGet.Versioning;
using Starward.Core.Metadata;
using Starward.Core.Metadata.Github;
using Starward.Frameworks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class UpdateContentWindow : WindowEx
{


    private readonly ILogger<UpdateContentWindow> _logger = AppConfig.GetLogger<UpdateContentWindow>();


    private readonly MetadataClient _metadataClient = AppConfig.GetService<MetadataClient>();



    public UpdateContentWindow()
    {
        this.InitializeComponent();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        Title = "Starward - Recently Updated Content";
        if (ShouldSystemUseDarkMode())
        {
            RootGrid.RequestedTheme = ElementTheme.Dark;
        }
        else
        {
            RootGrid.RequestedTheme = ElementTheme.Light;
        }
        SystemBackdrop = new DesktopAcrylicBackdrop();
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
        CenterInScreen();
    }




    private void CenterInScreen()
    {
        RectInt32 workArea = DisplayArea.GetFromWindowId(MainWindowId, DisplayAreaFallback.Nearest).WorkArea;
        int h = (int)(workArea.Height * 0.95);
        int w = (int)(h / 4.0 * 3.0);
        if (w > workArea.Width)
        {
            w = (int)(workArea.Width * 0.95);
            h = (int)(w * 4.0 / 3.0);
        }
        int x = workArea.X + (workArea.Width - w) / 2;
        int y = workArea.Y + (workArea.Height - h) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }



    private async void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        NuGetVersion.TryParse(AppConfig.LastAppVersion, out lastVersion!);
        lastVersion ??= new NuGetVersion(0, 0, 0);
        NuGetVersion.TryParse(AppConfig.AppVersion, out thisVersion!);
        thisVersion ??= new NuGetVersion(999, 999, 999);
        await LoadPageAsync();
    }



    private NuGetVersion lastVersion;

    private NuGetVersion thisVersion;




    private async Task LoadPageAsync()
    {
        try
        {
            StackPanel_Loading.Visibility = Visibility.Visible;
            StackPanel_Error.Visibility = Visibility.Collapsed;

            await webview.EnsureCoreWebView2Async();

            var releases = await _metadataClient.GetGithubReleaseAsync(1, 20);
            var markdown = new StringBuilder();
            int count = 0;
            bool preRelease = true;
            foreach (var release in releases)
            {
                if (NuGetVersion.TryParse(release.TagName, out var version))
                {
                    if (thisVersion.IsPrerelease && version.IsPrerelease && preRelease)
                    {
                        markdown.AppendLine($"# {release.Name}");
                        markdown.AppendLine();
                        markdown.AppendLine(release.Body);
                        markdown.AppendLine("<br>");
                        markdown.AppendLine();
                        count++;
                    }
                    else
                    {
                        if (!version.IsPrerelease)
                        {
                            preRelease = false;
                        }
                        if ((version > lastVersion || thisVersion < lastVersion) && version <= thisVersion)
                        {
                            if (!version.IsPrerelease)
                            {
                                markdown.AppendLine($"# {release.Name}");
                                markdown.AppendLine();
                                markdown.AppendLine(release.Body);
                                markdown.AppendLine("<br>");
                                markdown.AppendLine();
                                count++;
                            }
                        }
                    }
                }
                else
                {
                    markdown.AppendLine($"# {release.Name}");
                    markdown.AppendLine();
                    markdown.AppendLine(release.Body);
                    markdown.AppendLine("<br>");
                    markdown.AppendLine();
                    count++;
                }
                if (count >= 10)
                {
                    break;
                }
            }
            if (markdown.Length == 0)
            {
                try
                {
                    var r = await _metadataClient.GetGithubReleaseAsync(AppConfig.AppVersion!);
                    if (r is not null)
                    {
                        markdown.AppendLine($"# {r.Name}");
                        markdown.AppendLine();
                        markdown.AppendLine(r.Body);
                        markdown.AppendLine("<br>");
                        markdown.AppendLine();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex);
                    if (releases.FirstOrDefault() is GithubRelease r)
                    {
                        markdown.AppendLine($"# {r.Name}");
                        markdown.AppendLine();
                        markdown.AppendLine(r.Body);
                        markdown.AppendLine("<br>");
                        markdown.AppendLine();
                    }
                }
            }

            string html = await _metadataClient.RenderGithubMarkdownAsync(markdown.ToString());
            var cssFile = Path.Combine(AppContext.BaseDirectory, @"Assets\CSS\github-markdown.css");
            string css = "";
            if (File.Exists(cssFile))
            {
                css = await File.ReadAllTextAsync(cssFile);
            }

            html = $$"""
                    <!DOCTYPE html>
                    <html>
                    <head>
                    <base target="_blank">
                    {{(string.IsNullOrWhiteSpace(css) ? """<link href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.5.1/github-markdown.min.css" type="text/css" rel="stylesheet" />""" : "")}}
                    
                    <style>
                    {{css}}
                    </style>

                    <style>
                    @media (prefers-color-scheme: light) {
                      ::-webkit-scrollbar {
                        width: 6px
                      }

                      ::-webkit-scrollbar-thumb {
                        background-color: #b8b8b8;
                        border-radius: 1000px 0px 0px 1000px
                      }

                      ::-webkit-scrollbar-thumb:hover {
                        background-color: #8b8b8b
                      }
                    }

                    @media (prefers-color-scheme: dark) {
                      ::-webkit-scrollbar {
                        width: 6px
                      }

                      ::-webkit-scrollbar-thumb {
                        background-color: #646464;
                        border-radius: 1000px 0px 0px 1000px
                      }

                      ::-webkit-scrollbar-thumb:hover {
                        background-color: #8b8b8b
                      }
                    }
                    </style>

                    </head>
                    <body style="margin: 12px 24px 12px 24px; overflow-x: hidden;">
                    <article class="markdown-body" style="background: transparent;">
                    {{html}}
                    </article>
                    </body>
                    </html>
                    """;

            if (ShouldSystemUseDarkMode())
            {
                webview.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Dark;
            }
            else
            {
                webview.CoreWebView2.Profile.PreferredColorScheme = CoreWebView2PreferredColorScheme.Light;
            }

            webview.CoreWebView2.DOMContentLoaded -= CoreWebView2_DOMContentLoaded;
            webview.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webview.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
            webview.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
            webview.NavigateToString(html);
            AppConfig.LastAppVersion = AppConfig.AppVersion;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Load recent update content");
            TextBlock_Error.Text = Lang.Common_NetworkError;
            StackPanel_Loading.Visibility = Visibility.Collapsed;
            StackPanel_Error.Visibility = Visibility.Visible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load recent update content");
            TextBlock_Error.Text = Lang.DownloadGamePage_UnknownError;
            StackPanel_Loading.Visibility = Visibility.Collapsed;
            StackPanel_Error.Visibility = Visibility.Visible;
        }
    }


    private void CoreWebView2_DOMContentLoaded(CoreWebView2 sender, CoreWebView2DOMContentLoadedEventArgs args)
    {
        webview.Focus(FocusState.Programmatic);
        webview.Visibility = Visibility.Visible;
        StackPanel_Loading.Visibility = Visibility.Collapsed;
        StackPanel_Error.Visibility = Visibility.Collapsed;
    }


    private void CoreWebView2_NewWindowRequested(CoreWebView2 sender, CoreWebView2NewWindowRequestedEventArgs args)
    {
        try
        {
            _ = Launcher.LaunchUriAsync(new Uri(args.Uri));
            args.Handled = true;
        }
        catch { }
    }



    private async void Button_Retry_Click(object sender, RoutedEventArgs e)
    {
        await LoadPageAsync();
    }


    private void Button_RemindLatter_Click(Microsoft.UI.Xaml.Controls.SplitButton sender, Microsoft.UI.Xaml.Controls.SplitButtonClickEventArgs args)
    {
        Close();
    }


    private void Button_Ignore_Click(object sender, RoutedEventArgs e)
    {
        AppConfig.LastAppVersion = AppConfig.AppVersion;
        Close();
    }


}
