using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using NuGet.Versioning;
using Starward.Features.RPC;
using Starward.Features.Setting;
using Starward.Frameworks;
using Starward.RPC.Update;
using Starward.RPC.Update.Github;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.System;


namespace Starward.Features.Update;

[INotifyPropertyChanged]
public sealed partial class UpdateWindow : WindowEx
{


    private readonly ILogger<UpdateWindow> _logger = AppService.GetLogger<UpdateWindow>();


    private readonly MetadataClient _metadataClient = AppService.GetService<MetadataClient>();


    private readonly UpdateService _updateService = AppService.GetService<UpdateService>();


    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;



    public UpdateWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += _timer_Tick;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        this.Closed += UpdateWindow_Closed;
    }



    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        Title = "Starward - Update";
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        SystemBackdrop = new DesktopAcrylicBackdrop();
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
    }



    private void CenterInScreen()
    {
        RectInt32 workArea = DisplayArea.GetFromWindowId(MainWindowId, DisplayAreaFallback.Nearest).WorkArea;
        if (NewVersion is null)
        {
            Grid_Update.Visibility = Visibility.Collapsed;
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
        else
        {
            Button_RemindLatter.Visibility = Visibility.Collapsed;
            int w = (int)(1000 * UIScale);
            int h = (int)(w / 4.0 * 3.0);
            if (w > workArea.Width || h > workArea.Height)
            {
                h = (int)(workArea.Height * 0.9);
                w = (int)(h / 4.0 * 3.0);
                if (w > workArea.Width)
                {
                    w = (int)(workArea.Width * 0.9);
                    h = (int)(w * 4.0 / 3.0);
                }
            }
            int x = workArea.X + (workArea.Width - w) / 2;
            int y = workArea.Y + (workArea.Height - h) / 2;
            AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
        }
    }



    public new void Activate()
    {
        CenterInScreen();
        base.Activate();
    }



    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        if (NewVersion?.DisableAutoUpdate ?? false)
        {
            IsUpdateNowEnabled = false;
            ErrorMessage = Lang.UpdatePage_YouNeedToManuallyDownloadTheNewVersionPackage;
        }
        if (UpdateService.UpdateFinished)
        {
            Finish(skipRestart: true);
        }
        _ = LoadUpdateContentAsync();
    }



    private void UpdateWindow_Closed(object sender, WindowEventArgs args)
    {
        _timer.Stop();
        _timer.Tick -= _timer_Tick;
        _updateService.StopUpdate();
        WeakReferenceMessenger.Default.UnregisterAll(this);
        this.Closed -= UpdateWindow_Closed;
    }




    public ReleaseVersion? NewVersion { get; set => SetProperty(ref field, value); }


#if DEV
    public string ChannelText => Lang.UpdatePage_DevChannel;
#else
    public string ChannelText => AppSetting.EnablePreviewRelease ? Lang.UpdatePage_PreviewChannel : Lang.UpdatePage_StableChannel;
#endif



    private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && NewVersion != null)
            {
                var url = fe.Tag switch
                {
                    "release" => NewVersion.ReleasePage,
                    "install" => NewVersion.Install,
                    "portable" => NewVersion.Portable,
                    _ => null,
                };
                _logger.LogInformation("Open url: {url}", url);
                if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri))
                {
                    await Launcher.LaunchUriAsync(uri);
                }
            }
        }
        catch { }
    }





    #region Update



    public bool IsUpdateNowEnabled { get; set => SetProperty(ref field, value); } = true;

    public bool IsUpdateRemindLatterEnabled { get; set => SetProperty(ref field, value); } = true;

    public bool IsProgressTextVisible { get; set => SetProperty(ref field, value); }

    public bool IsProgressBarVisible { get; set => SetProperty(ref field, value); }

    public string ProgressBytesText { get; set => SetProperty(ref field, value); }

    public string ProgressCountText { get; set => SetProperty(ref field, value); }

    public string ProgressPercentText { get; set => SetProperty(ref field, value); }

    public string ProgressSpeedText { get; set => SetProperty(ref field, value); }

    public string? ErrorMessage { get; set => SetProperty(ref field, value); }



    public bool AutoRestartWhenUpdateFinished
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppSetting.AutoRestartWhenUpdateFinished = value;
            }
        }
    } = AppSetting.AutoRestartWhenUpdateFinished;



    public bool ShowUpdateContentAfterUpdateRestart
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppSetting.ShowUpdateContentAfterUpdateRestart = value;
            }
        }
    } = AppSetting.ShowUpdateContentAfterUpdateRestart;




    [RelayCommand]
    private async Task UpdateNowAsync()
    {
        try
        {
            ErrorMessage = null;
            IsUpdateNowEnabled = false;
            IsUpdateRemindLatterEnabled = false;

            if (NewVersion != null)
            {
                _timer.Start();
                await _updateService.StartUpdateAsync(NewVersion);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update now");
            Button_UpdateNow.IsEnabled = true;
            Button_RemindLatter.IsEnabled = true;
        }
    }




    private void UpdateProgressState()
    {
        if (_updateService.State is UpdateState.Pending)
        {
            IsProgressTextVisible = true;
            IsProgressBarVisible = true;
            ProgressBar_Update.IsIndeterminate = true;
            UpdateProgressValue();
        }
        else if (_updateService.State is UpdateState.Downloading)
        {
            IsUpdateNowEnabled = false;
            IsUpdateRemindLatterEnabled = false;
            IsProgressBarVisible = true;
            IsProgressTextVisible = true;
            ProgressBar_Update.IsIndeterminate = false;
            UpdateProgressValue();
        }
        else if (_updateService.State is UpdateState.Finish)
        {
            IsProgressTextVisible = false;
            ProgressBar_Update.IsIndeterminate = false;
            ProgressBar_Update.Value = 100;
        }
        else if (_updateService.State is UpdateState.Stop)
        {
            IsUpdateNowEnabled = true;
            IsUpdateRemindLatterEnabled = true;
            IsProgressTextVisible = false;
            IsProgressBarVisible = false;
            ErrorMessage = null;
        }
        else if (_updateService.State is UpdateState.Error)
        {
            IsUpdateNowEnabled = true;
            IsUpdateRemindLatterEnabled = true;
            IsProgressTextVisible = false;
            IsProgressBarVisible = false;
            ErrorMessage = _updateService.ErrorMessage;
        }
        else if (_updateService.State is UpdateState.NotSupport)
        {
            IsUpdateNowEnabled = false;
            IsUpdateRemindLatterEnabled = true;
            IsProgressTextVisible = false;
            IsProgressBarVisible = false;
            ErrorMessage = _updateService.ErrorMessage;
        }
    }



    private void UpdateProgressValue()
    {
        if (_updateService.Progress_TotalBytes == 0 || _updateService.Progress_DownloadBytes == 0)
        {
            ProgressBytesText = "";
            ProgressCountText = "";
            return;
        }
        const double mb = 1 << 20;
        ProgressBytesText = $"{_updateService.Progress_DownloadBytes / mb:F2}/{_updateService.Progress_TotalBytes / mb:F2} MB";
        ProgressCountText = $"{_updateService.Progress_DownloadFileCount}/{_updateService.Progress_TotalFileCount}";
        var progress = (double)_updateService.Progress_DownloadBytes / _updateService.Progress_TotalBytes;
        ProgressPercentText = $"{progress:P1}";
        ProgressBar_Update.Value = progress * 100;
    }




    private void _timer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {

        try
        {
            UpdateProgressState();
            if (_updateService.State is UpdateState.Finish)
            {
                _timer.Stop();
                Finish();
            }
            else if (_updateService.State is UpdateState.Stop or UpdateState.Error or UpdateState.NotSupport)
            {
                _timer.Stop();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update progress");
        }
    }



    private void Finish(bool skipRestart = false)
    {
        AppSetting.IgnoreVersion = null;
        Button_UpdateNow.Visibility = Visibility.Collapsed;
        Button_Restart.Visibility = Visibility.Visible;
        AppService.GetService<RpcService>().KeepRunningOnExited(false, noLongerChange: true);
        if (AutoRestartWhenUpdateFinished && !skipRestart)
        {
            Restart();
        }
    }



    [RelayCommand]
    private void Restart()
    {
        try
        {
            string? launcher = AppSetting.StarwardLauncherExecutePath;
            if (File.Exists(launcher))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = launcher,
                    WorkingDirectory = Path.GetDirectoryName(launcher),
                });
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Restart");
            ErrorMessage = ex.Message;
        }
    }



    [RelayCommand]
    private void RemindMeLatter()
    {
        this.Close();
    }



    [RelayCommand]
    private void IgnoreThisVersion()
    {
        if (NewVersion is null)
        {
            AppSetting.LastAppVersion = AppSetting.AppVersion;
        }
        else
        {
            AppSetting.IgnoreVersion = NewVersion.Version;
        }
        this.Close();
    }




    private void Button_UpdateRemindLatter_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        Button_UpdateRemindLatter.Opacity = 1;
    }


    private void Button_UpdateRemindLatter_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        Button_UpdateRemindLatter.Opacity = 0;
    }




    #endregion





    #region Update Content WebView





    private async Task LoadUpdateContentAsync()
    {
        try
        {
            StackPanel_Loading.Visibility = Visibility.Visible;
            StackPanel_Error.Visibility = Visibility.Collapsed;

            await webview.EnsureCoreWebView2Async();
            webview.CoreWebView2.Profile.PreferredColorScheme = ShouldSystemUseDarkMode() ? CoreWebView2PreferredColorScheme.Dark : CoreWebView2PreferredColorScheme.Light;
            webview.CoreWebView2.DOMContentLoaded -= CoreWebView2_DOMContentLoaded;
            webview.CoreWebView2.DOMContentLoaded += CoreWebView2_DOMContentLoaded;
            webview.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
            webview.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;

            string markdown = await GetReleaseContentMarkdownAsync();
            string html = await RenderMarkdownAsync(markdown);
            webview.NavigateToString(html);
            AppSetting.LastAppVersion = AppSetting.AppVersion;
        }
        catch (COMException ex)
        {
            _logger.LogError(ex, "Load recent update content");
            TextBlock_Error.Text = Lang.Common_WebView2ComponentInitializationFailed;
            StackPanel_Loading.Visibility = Visibility.Collapsed;
            StackPanel_Error.Visibility = Visibility.Visible;
        }
        catch (Exception ex) when (ex is HttpRequestException or SocketException or IOException)
        {
            _logger.LogError(ex, "Load recent update content");
            string tag = NewVersion?.Version ?? AppSetting.AppVersion;
            webview.Source = new Uri($"https://github.com/Scighost/Starward/releases/tag/{tag}");
            webview.Visibility = Visibility.Visible;
            StackPanel_Loading.Visibility = Visibility.Collapsed;
            StackPanel_Error.Visibility = Visibility.Collapsed;
            AppSetting.LastAppVersion = AppSetting.AppVersion;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load recent update content");
            TextBlock_Error.Text = Lang.DownloadGamePage_UnknownError;
            StackPanel_Loading.Visibility = Visibility.Collapsed;
            StackPanel_Error.Visibility = Visibility.Visible;
        }
    }



    private async Task<string> GetReleaseContentMarkdownAsync()
    {
        bool showPrerelease = false;
        NuGetVersion? startVersion, endVersion;
        if (NewVersion is null)
        {
            _ = NuGetVersion.TryParse(AppSetting.LastAppVersion, out startVersion);
            _ = NuGetVersion.TryParse(AppSetting.AppVersion, out endVersion);
        }
        else
        {
            _ = NuGetVersion.TryParse(AppSetting.AppVersion, out startVersion);
            _ = NuGetVersion.TryParse(NewVersion.Version, out endVersion);
        }
        startVersion ??= new NuGetVersion(0, 0, 0);
        endVersion ??= new NuGetVersion(int.MaxValue, int.MaxValue, int.MaxValue);
        if (endVersion.IsPrerelease)
        {
            showPrerelease = true;
            if (startVersion.IsPrerelease)
            {
                startVersion = new NuGetVersion(startVersion.Major, startVersion.Minor, startVersion.Patch - 1);
            }
        }

        var releases = await _metadataClient.GetGithubReleaseAsync(1, 20);
        var markdown = new StringBuilder();
        int count = 0;
        foreach (var release in releases)
        {
            if (NuGetVersion.TryParse(release.TagName, out var version))
            {
                if (version > startVersion && version <= endVersion)
                {
                    // 只显示最新的几个连续的预览版，最新稳定版之前的预览版不显示
                    if (!version.IsPrerelease)
                    {
                        showPrerelease = false;
                    }
                    if (!(showPrerelease ^ version.IsPrerelease))
                    {
                        AppendReleaseToStringBuilder(release, markdown);
                        count++;
                    }
                }
            }
            else
            {
                AppendReleaseToStringBuilder(release, markdown);
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
                var r = await _metadataClient.GetGithubReleaseAsync(NewVersion?.Version ?? AppSetting.AppVersion);
                if (r is not null)
                {
                    AppendReleaseToStringBuilder(r, markdown);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
                if (releases.FirstOrDefault() is GithubRelease r)
                {
                    AppendReleaseToStringBuilder(r, markdown);
                }
            }
        }
        return markdown.ToString();
    }



    private static void AppendReleaseToStringBuilder(GithubRelease release, StringBuilder stringBuilder)
    {
        stringBuilder.AppendLine($"# {release.Name}");
        stringBuilder.AppendLine();
        stringBuilder.AppendLine(release.Body);
        stringBuilder.AppendLine("<br>");
        stringBuilder.AppendLine();
    }



    private async Task<string> RenderMarkdownAsync(string markdown)
    {
        string html = await _metadataClient.RenderGithubMarkdownAsync(markdown);
        var cssFile = Path.Combine(AppContext.BaseDirectory, @"Assets\CSS\github-markdown.css");
        string? css = null;
        if (File.Exists(cssFile))
        {
            css = await File.ReadAllTextAsync(cssFile);
            css = $"<style>{css}</style>";
        }
        else
        {
            css = """<link href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.8.1/github-markdown.min.css" type="text/css" rel="stylesheet" />""";
        }
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
              <base target="_blank">
              {{css}}
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



    [RelayCommand]
    private async Task RetryAsync()
    {
        await LoadUpdateContentAsync();
    }




    #endregion





    #region Converter



    public static string ByteLengthToString(long byteLength)
    {
        double length = byteLength;
        return length switch
        {
            >= (1 << 30) => $"{length / (1 << 30):F2} GB",
            >= (1 << 20) => $"{length / (1 << 20):F2} MB",
            _ => $"{length / (1 << 10):F2} KB",
        };
    }



    public static Visibility StringToVisibility(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? Visibility.Collapsed : Visibility.Visible;
    }


    #endregion



}
