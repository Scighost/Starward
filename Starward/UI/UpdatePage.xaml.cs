// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core.Metadata;
using Starward.Service;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class UpdatePage : Page
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
                ErrorMessage = "新旧版本不兼容，需要重新下载完整文件";
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

#if DEBUG||DEV
    public string ChannelText => "开发版";
#else
    public string ChannelText => AppConfig.EnablePreviewRelease ? "预览版" : "正式版";
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
                    ErrorMessage = "新旧版本不兼容，需要重新下载完整文件";
                }
            }
            try
            {
                var githubRelease = await _metadataClient.GetGithubReleaseAsync(NewVersion.Version);
                if (githubRelease != null)
                {
                    markdown.Text = githubRelease.Body;
                    GRid_Markdown.Visibility = Visibility.Visible;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("Cannot get github release: {error}", ex.Message);
            }
            newRelease = await _metadataClient.GetReleaseAsync(AppConfig.EnablePreviewRelease, RuntimeInformation.OSArchitecture);
            NewVersion ??= newRelease;
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


    private async void markdown_LinkClicked(object sender, CommunityToolkit.WinUI.UI.Controls.LinkClickedEventArgs e)
    {
        try
        {
            _logger.LogInformation("Open url: {url}", e.Link);
            if (Uri.TryCreate(e.Link, UriKind.RelativeOrAbsolute, out var uri))
            {
                await Launcher.LaunchUriAsync(uri);
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
            Button_IgnoreVersion.IsEnabled = false;

            if (newRelease is null)
            {
                newRelease = await _metadataClient.GetReleaseAsync(isPreview, RuntimeInformation.OSArchitecture);
                if (NewVersion.DisableAutoUpdate)
                {
                    Button_Update.IsEnabled = false;
                    ErrorMessage = "新旧版本不兼容，需要重新下载完整文件";
                    Button_RemindLatter.IsEnabled = true;
                    Button_IgnoreVersion.IsEnabled = true;
                    return;
                }
            }
            if (newRelease != null)
            {
                IsProgressBarVisible = true;
                ProgresBar_Update.IsIndeterminate = true;
                await _updateService.PrepareForUpdateAsync(newRelease);
                _updateService.Start();
                _timer.Start();
            }
        }
        catch (NotSupportedException)
        {
            IsProgressBarVisible = false;
            ErrorMessage = "安装目录不符合要求，无法自动更新";
            Button_Update.IsEnabled = false;
            Button_RemindLatter.IsEnabled = true;
            Button_IgnoreVersion.IsEnabled = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update now");
            Button_Update.IsEnabled = true;
            Button_RemindLatter.IsEnabled = true;
            Button_IgnoreVersion.IsEnabled = true;
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



    private void _timer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        const double mb = 1 << 20;
        try
        {
            if (_updateService.State is UpdateService.UpdateState.Pending or UpdateService.UpdateState.Moving)
            {
                IsProgressTextVisible = false;
                IsProgressBarVisible = true;
                ProgresBar_Update.IsIndeterminate = true;
            }
            if (_updateService.State is UpdateService.UpdateState.Downloading)
            {
                IsProgressTextVisible = true;
                ProgressBytesText = $"{_updateService.Progress_BytesDownloaded / mb:F2}/{_updateService.Progress_BytesToDownload / mb:F2} MB";
                ProgressCountText = $"{_updateService.Progress_FileCountDownloaded}/{_updateService.Progress_FileCountToDownload}";
                var progress = (double)_updateService.Progress_BytesDownloaded / _updateService.Progress_BytesToDownload;
                ProgressPercentText = $"{progress:P1}";
                ProgresBar_Update.IsIndeterminate = false;
                ProgresBar_Update.Value = progress * 100;
            }
            if (_updateService.State is UpdateService.UpdateState.Finish)
            {
                IsProgressTextVisible = false;
                ProgresBar_Update.IsIndeterminate = false;
                ProgresBar_Update.Value = 100;
                _timer.Stop();
                Restart();
            }
            if (_updateService.State is UpdateService.UpdateState.Stop)
            {
                IsProgressTextVisible = false;
                IsProgressBarVisible = false;
                ErrorMessage = _updateService.ErrorMessage;
                _timer.Stop();
                Button_Update.IsEnabled = true;
                Button_RemindLatter.IsEnabled = true;
                Button_IgnoreVersion.IsEnabled = true;
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
            Button_IgnoreVersion.IsEnabled = true;
        }
    }


}
