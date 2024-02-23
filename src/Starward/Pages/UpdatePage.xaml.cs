// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core.Metadata;
using Starward.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class UpdatePage : PageBase
{

    private readonly ILogger<UpdatePage> _logger = AppConfig.GetLogger<UpdatePage>();

    private readonly MetadataClient _metadataClient = AppConfig.GetService<MetadataClient>();

    private readonly UpdateService _updateService = AppConfig.GetService<UpdateService>();

    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;

    public UpdatePage()
    {
        this.InitializeComponent();
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += _timer_Tick;
    }



    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is ReleaseVersion version)
        {
            NewVersion = version;
            if (NewVersion.DisableAutoUpdate)
            {
                Button_Update.IsEnabled = false;
                // 你需要手动下载新版安装包
                ErrorMessage = Lang.UpdatePage_YouNeedToManuallyDownloadTheNewVersionPackage;
            }
        }
    }



    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await GetReleaseAsync();
    }

    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        _updateService.Stop();
    }


    private bool isPreview = AppConfig.EnablePreviewRelease;

#if DEV
    public string ChannelText => Lang.UpdatePage_DevChannel;
#else
    public string ChannelText => AppConfig.EnablePreviewRelease ? Lang.UpdatePage_PreviewChannel : Lang.UpdatePage_StableChannel;
#endif



    [ObservableProperty]
    private ReleaseVersion newVersion;


    private ReleaseVersion newRelease;



    private async Task GetReleaseAsync()
    {
        try
        {
            if (NewVersion is null)
            {
                NewVersion = await _metadataClient.GetVersionAsync(AppConfig.EnablePreviewRelease, RuntimeInformation.OSArchitecture);
                if (NewVersion.DisableAutoUpdate)
                {
                    Button_Update.IsEnabled = false;
                    ErrorMessage = Lang.UpdatePage_YouNeedToManuallyDownloadTheNewVersionPackage;
                }
            }
            await ShowGithubReleaseAsync(NewVersion.Version);

            newRelease = await _metadataClient.GetReleaseAsync(AppConfig.EnablePreviewRelease, RuntimeInformation.OSArchitecture);
            NewVersion ??= newRelease;

            _timer.Start();
            await _updateService.PrepareForUpdateAsync(newRelease);
            UpdateProgressState();
            _timer.Stop();
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Cannot get latest release: {error}", ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Get release");
        }
    }



    private async Task ShowGithubReleaseAsync(string tag)
    {
        try
        {
            var release = await _metadataClient.GetGithubReleaseAsync(tag);
            if (release != null)
            {
                string markdown = $"""
                    # {release.Name}

                    > Update at {release.PublishedAt.LocalDateTime:yyyy-MM-dd HH:mm:ss}

                    {release.Body}

                    """;
                string html = await _metadataClient.RenderGithubMarkdownAsync(markdown);
                var cssFile = Path.Combine(AppContext.BaseDirectory, @"Assets\CSS\github-markdown-dark.css");
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
                    <meta name="color-scheme" content="dark">
                    {{(string.IsNullOrWhiteSpace(css) ? """<link href="https://cdnjs.cloudflare.com/ajax/libs/github-markdown-css/5.2.0/github-markdown-dark.min.css" type="text/css" rel="stylesheet" />""" : "")}}
                    <style>
                    body::-webkit-scrollbar {display: none;}
                    {{css}}
                    </style>
                    </head>
                    <body style="background-color: #404040; margin: 24px;">
                    <article class="markdown-body" style="background-color: #404040;">
                    {{html}}
                    </article>
                    </body>
                    </html>
                    """;
                await webview.EnsureCoreWebView2Async();
                webview.NavigateToString(html);
                Border_Markdown.Visibility = Visibility.Visible;
                webview.CoreWebView2.Settings.AreDevToolsEnabled = false;
                webview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                webview.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                webview.CoreWebView2.NewWindowRequested += async (s, e) =>
                {
                    try
                    {
                        e.Handled = true;
                        await Launcher.LaunchUriAsync(new Uri(e.Uri));
                    }
                    catch { }
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning("Cannot get github release: {error}", ex.Message);
            if (!tag.Contains("dev"))
            {
                webview.Source = new Uri($"https://github.com/Scighost/Starward/releases/tag/{tag}");
                Border_Markdown.Visibility = Visibility.Visible;
                await webview.EnsureCoreWebView2Async();
                webview.CoreWebView2.Profile.PreferredColorScheme = Microsoft.Web.WebView2.Core.CoreWebView2PreferredColorScheme.Dark;
                webview.CoreWebView2.NewWindowRequested += async (s, e) =>
                {
                    try
                    {
                        e.Handled = true;
                        await Launcher.LaunchUriAsync(new Uri(e.Uri));
                    }
                    catch { }
                };
            }
        }
    }



    private async void HyperlinkButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe)
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




    [RelayCommand]
    private void RemindMeLatter()
    {
        MainWindow.Current.CloseOverlayPage();
    }



    [RelayCommand]
    private void IgnoreThisVersion()
    {
        AppConfig.IgnoreVersion = NewVersion?.Version;
        MainWindow.Current.CloseOverlayPage();
    }




    [RelayCommand]
    private async Task UpdateNowAsync()
    {
        try
        {
            ErrorMessage = null;
            Button_Update.IsEnabled = false;
            Button_RemindLatter.IsEnabled = false;

            if (newRelease is null)
            {
                newRelease = await _metadataClient.GetReleaseAsync(isPreview, RuntimeInformation.OSArchitecture);
                if (NewVersion.DisableAutoUpdate)
                {
                    IsProgressBarVisible = false;
                    Button_Update.IsEnabled = false;
                    ErrorMessage = Lang.UpdatePage_YouNeedToManuallyDownloadTheNewVersionPackage;
                    Button_RemindLatter.IsEnabled = true;
                    return;
                }
            }
            if (newRelease != null)
            {
                _timer.Start();
                while (_updateService.State is UpdateService.UpdateState.Preparing)
                {
                    await Task.Delay(100);
                }
                if (_updateService.State is not UpdateService.UpdateState.Pending)
                {
                    await _updateService.PrepareForUpdateAsync(newRelease);
                }
                if (_updateService.State is UpdateService.UpdateState.Pending)
                {
                    _updateService.Start();
                }
                _timer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update now");
            Button_Update.IsEnabled = true;
            Button_RemindLatter.IsEnabled = true;
        }
    }



    [ObservableProperty]
    private bool isProgressTextVisible;

    [ObservableProperty]
    private bool isProgressBarVisible;

    [ObservableProperty]
    private string progressBytesText;

    [ObservableProperty]
    private string progressCountText;

    [ObservableProperty]
    private string progressPercentText;

    [ObservableProperty]
    private string progressSpeedText;

    [ObservableProperty]
    private string? errorMessage;



    private void UpdateProgressState()
    {
        if (_updateService.State is UpdateService.UpdateState.Preparing)
        {
            IsProgressTextVisible = false;
            IsProgressBarVisible = true;
            ProgressBar_Update.IsIndeterminate = true;
        }
        if (_updateService.State is UpdateService.UpdateState.Pending)
        {
            IsProgressTextVisible = true;
            IsProgressBarVisible = true;
            ProgressBar_Update.IsIndeterminate = false;
            UpdateProgressValue();
        }
        if (_updateService.State is UpdateService.UpdateState.Downloading)
        {
            Button_Update.IsEnabled = false;
            Button_RemindLatter.IsEnabled = false;
            IsProgressBarVisible = true;
            IsProgressTextVisible = true;
            ProgressBar_Update.IsIndeterminate = false;
            UpdateProgressValue();
        }
        if (_updateService.State is UpdateService.UpdateState.Moving)
        {
            Button_Update.IsEnabled = false;
            Button_RemindLatter.IsEnabled = false;
            IsProgressBarVisible = true;
            IsProgressTextVisible = true;
            ProgressBar_Update.IsIndeterminate = true;
            UpdateProgressValue();
        }
        if (_updateService.State is UpdateService.UpdateState.Finish)
        {
            IsProgressTextVisible = false;
            ProgressBar_Update.IsIndeterminate = false;
            ProgressBar_Update.Value = 100;
        }
        if (_updateService.State is UpdateService.UpdateState.Stop)
        {
            IsProgressTextVisible = false;
            IsProgressBarVisible = false;
            ErrorMessage = null;
            Button_Update.IsEnabled = true;
            Button_RemindLatter.IsEnabled = true;
        }
        if (_updateService.State is UpdateService.UpdateState.Error)
        {
            IsProgressTextVisible = false;
            IsProgressBarVisible = false;
            ErrorMessage = _updateService.ErrorMessage;
            Button_Update.IsEnabled = true;
            Button_RemindLatter.IsEnabled = true;
        }
        if (_updateService.State is UpdateService.UpdateState.NotSupport)
        {
            IsProgressTextVisible = false;
            IsProgressBarVisible = false;
            ErrorMessage = _updateService.ErrorMessage;
            Button_Update.IsEnabled = false;
            Button_RemindLatter.IsEnabled = true;
        }
    }


    private void UpdateProgressValue()
    {
        const double mb = 1 << 20;
        ProgressBytesText = $"{_updateService.Progress_BytesDownloaded / mb:F2}/{_updateService.Progress_BytesToDownload / mb:F2} MB";
        ProgressCountText = $"{_updateService.Progress_FileCountDownloaded}/{_updateService.Progress_FileCountToDownload}";
        var progress = (double)_updateService.Progress_BytesDownloaded / _updateService.Progress_BytesToDownload;
        ProgressPercentText = $"{progress:P1}";
        ProgressBar_Update.Value = progress * 100;
    }




    private void _timer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {

        try
        {
            UpdateProgressState();
            if (_updateService.State is UpdateService.UpdateState.Finish)
            {
                _timer.Stop();
                Restart();
            }
            if (_updateService.State is UpdateService.UpdateState.Stop or UpdateService.UpdateState.Error or UpdateService.UpdateState.NotSupport)
            {
                _timer.Stop();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update progress");
        }
    }




    private void Restart()
    {
        try
        {
            var baseDir = new DirectoryInfo(AppContext.BaseDirectory).Parent?.FullName;
            var exe = Path.Join(baseDir, "Starward.exe");
            if (File.Exists(exe))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = baseDir,
                });
                AppConfig.IgnoreVersion = null;
                Environment.Exit(0);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Restart");
            ErrorMessage = ex.Message;
        }
        finally
        {
            Button_Update.IsEnabled = true;
            Button_RemindLatter.IsEnabled = true;
        }
    }



    private void Button_RemindLatter_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        Button_RemindLatter.Opacity = 1;
    }

    private void Button_RemindLatter_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        Button_RemindLatter.Opacity = 0;
    }


}
