using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using Starward.Core;
using Starward.Helpers;
using Starward.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.System;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class DownloadGamePage : Page
{

    private readonly ILogger<DownloadGamePage> _logger = AppConfig.GetLogger<DownloadGamePage>();

    private readonly GameService _gameService = AppConfig.GetService<GameService>();

    private readonly LauncherService _launcherService = AppConfig.GetService<LauncherService>();

    private readonly DownloadGameService _downloadGameService = AppConfig.GetService<DownloadGameService>();


    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();


    public DownloadGamePage()
    {
        this.InitializeComponent();
        MainWindow.Current.ChangeAccentColor(null, null);
        MainWindow.Current.AppWindow.Closing += AppWindow_Closing;
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += (_, _) => UpdateTicks();

        InitializeGameBiz();
    }

    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        if (_downloadGameService.State is DownloadGameService.DownloadGameState.None or DownloadGameService.DownloadGameState.Error || isFinish)
        {
            MainWindow.Current.Close();
            return;
        }
        var dialog = new ContentDialog
        {
            Title = "关闭程序",
            Content = "下次启动后可恢复下载；\r\n解压过程中关闭程序会造成游戏文件损坏。",
            PrimaryButtonText = "关闭",
            SecondaryButtonText = "取消",
            DefaultButton = ContentDialogButton.Secondary,
            XamlRoot = this.XamlRoot,
        };
        if (await dialog.ShowAsync() is ContentDialogResult.Primary)
        {
            MainWindow.Current.Close();
        }
    }

    private void InitializeGameBiz()
    {
        var config = new ConfigurationBuilder().AddCommandLine(Environment.GetCommandLineArgs()).Build();
        var str_biz = config["biz"];
        var str_loc = config["loc"];
        var str_lang = config["lang"];
        Enum.TryParse(str_biz, out GameBiz biz);
        int.TryParse(str_lang, out int lang);
        gameBiz = biz;
        gameFolder = str_loc!;
        voiceLanguage = (VoiceLanguage)lang;

    }




    private GameBiz gameBiz;


    private string gameFolder;


    private VoiceLanguage voiceLanguage;



    private CancellationTokenSource tokenSource;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            await Task.Delay(16);
            await CheckInstanceAsync();
            _ = GetBgAsync();
            IsContentVisible = true;
            _ = PrepareForDownloadAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when loaded");
        }
    }




    private async Task CheckInstanceAsync()
    {
        var instance = AppInstance.FindOrRegisterForKey($"download_game_{gameBiz}");
        if (!instance.IsCurrent)
        {
            var dialog = new ContentDialog
            {
                Title = "重复启动",
                Content = "程序即将退出",
                PrimaryButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            await instance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
            Environment.Exit(0);
        }
        if (gameBiz.ToGame() is GameBiz.None || gameBiz is GameBiz.hk4e_cloud || !Directory.Exists(gameFolder))
        {
            instance.UnregisterKey();
            var dialog = new ContentDialog
            {
                Title = "参数错误",
                Content = "程序即将退出",
                PrimaryButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            Environment.Exit(0);
        }
        using WindowsIdentity identity = WindowsIdentity.GetCurrent();
        WindowsPrincipal principal = new WindowsPrincipal(identity);
        if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
        {
            var dialog = new ContentDialog
            {
                Title = "权限不足",
                Content = "请使用管理员模式启动",
                PrimaryButtonText = "确定",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            Environment.Exit(0);
        }
    }



    [ObservableProperty]
    private ImageSource backgroundImage/* = new BitmapImage(new Uri("ms-appx:///Assets/Image/StartUpBG2.png"))*/;



    [ObservableProperty]
    private bool isContentVisible;



    private async Task GetBgAsync()
    {
        try
        {
            var file = _launcherService.GetCachedBackgroundImage(gameBiz, true);
            if (!File.Exists(file))
            {
                file = await _launcherService.GetBackgroundImageAsync(gameBiz, true);
            }
            using var fs = File.OpenRead(file);
            var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
            int decodeWidth = (int)decoder.PixelWidth;
            int decodeHeight = (int)decoder.PixelHeight;
            WriteableBitmap bitmap = new WriteableBitmap(decodeWidth, decodeHeight);
            fs.Position = 0;
            await bitmap.SetSourceAsync(fs.AsRandomAccessStream());
            (Color? back, Color? fore) = AccentColorHelper.GetAccentColor(bitmap.PixelBuffer, decodeWidth, decodeHeight);
            MainWindow.Current.ChangeAccentColor(back, fore);
            BackgroundImage = bitmap;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "get bg");
        }
    }




    private bool decompress;

    private bool isFinish;


    [ObservableProperty]
    private bool isActionButtonEnable;


    [ObservableProperty]
    private string? actionButtonIcon = StartIcon;


    private const string StartIcon = "\xE102";
    private const string PauseIcon = "\xE103";
    private const string NextIcon = "\xE101";
    private const string StopIcon = "\xE15B";
    private const string ErrorIcon = "\xEA39";
    private const string FinishIcon = "\xE930";


    [ObservableProperty]
    private string? actionButtonText = "开始";


    [ObservableProperty]
    private bool isProgressStateVisible = true;

    [ObservableProperty]
    private string? stateText;

    [ObservableProperty]
    private string? progressBytesText;

    [ObservableProperty]
    private string? progressText;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private string? speedText;

    [ObservableProperty]
    private string? remainTimeText;

    private long lastProgressBytes;

    private long lastMilliseconds;


    private void UpdateTicks()
    {
        try
        {
            var state = _downloadGameService.State;
            if (state is DownloadGameService.DownloadGameState.None)
            {
                return;
            }
            if (state is DownloadGameService.DownloadGameState.Preparing)
            {
                StateText = "准备中";
            }
            if (state is DownloadGameService.DownloadGameState.Prepared)
            {
                _ = DownloadAsync();
            }
            if (state is DownloadGameService.DownloadGameState.Downloading)
            {
                StateText = "下载中";
            }
            if (state is DownloadGameService.DownloadGameState.Downloaded)
            {
                _ = VerifyAsync();
            }
            if (state is DownloadGameService.DownloadGameState.Verifying)
            {
                StateText = "校验中";
            }
            if (state is DownloadGameService.DownloadGameState.Verified)
            {
                if (decompress)
                {
                    _ = DecompressAsync();
                }
                else
                {
                    FinishTask();
                    return;
                }
            }
            if (state is DownloadGameService.DownloadGameState.Decompressing)
            {
                StateText = "解压中";
            }
            if (state is DownloadGameService.DownloadGameState.Decompressed)
            {
                FinishTask();
                return;
            }
            if (state is DownloadGameService.DownloadGameState.Error)
            {
                ShowErrorMessage();
                return;
            }
            if (state is DownloadGameService.DownloadGameState.Finish)
            {
                FinishTask();
                return;
            }
            UpdateDownloadProgress();
        }
        catch { }
    }



    private async void ShowErrorMessage()
    {
        try
        {
            _timer.Stop();
            StateText = "出错了";
            SpeedText = null;
            RemainTimeText = null;
            IsActionButtonEnable = true;
            ActionButtonIcon = StartIcon;
            ActionButtonText = "重试";

            var dialog = new ContentDialog()
            {
                PrimaryButtonText = "打开日志",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            if (_downloadGameService.ErrorType is nameof(HttpRequestException))
            {
                dialog.Title = "网络错误";
                dialog.Content = _downloadGameService.ErrorMessage;
            }
            else
            {
                dialog.Title = "未知错误";
                dialog.Content = _downloadGameService.ErrorMessage;
            }
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                await Launcher.LaunchUriAsync(new Uri(AppConfig.LogFile));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Show error message");
        }
    }



    private void UpdateDownloadProgress()
    {
        const double GB = 1 << 30;
        long thisProgressBytes = _downloadGameService.ProgressBytes;
        long thisMilliseconds = _stopwatch.ElapsedMilliseconds;
        ProgressBytesText = $"{thisProgressBytes / GB:F2}/{_downloadGameService.TotalBytes / GB:F2} GB";
        var progress = (double)thisProgressBytes / _downloadGameService.TotalBytes;
        progress = double.IsNormal(progress) ? progress : 0;
        ProgressValue = progress * 100;
        ProgressText = progress.ToString("P1");
        if (thisMilliseconds - lastMilliseconds > 1000)
        {
            double speed = (double)(thisProgressBytes - lastProgressBytes) / (thisMilliseconds - lastMilliseconds) * 1000;
            SpeedText = $"{speed / (1 << 20):F2} MB/s";
            var remainTime = TimeSpan.FromSeconds((_downloadGameService.TotalBytes - thisProgressBytes) / speed);
            RemainTimeText = $"{remainTime.Days * 24 + remainTime.Hours}h {remainTime.Minutes}m {remainTime.Seconds}s";
            lastProgressBytes = thisProgressBytes;
            lastMilliseconds = thisMilliseconds;
        }
    }



    private void FinishTask()
    {
        _timer.Stop();
        isFinish = true;
        IsActionButtonEnable = true;
        ActionButtonIcon = FinishIcon;
        ActionButtonText = "完成";
        IsProgressStateVisible = false;
        ProgressValue = 100;
    }




    private async Task PrepareForDownloadAsync()
    {
        _timer.Start();
        IsActionButtonEnable = false;
        lastMilliseconds = _stopwatch.ElapsedMilliseconds;
        lastProgressBytes = _downloadGameService.ProgressBytes;
        decompress = await _downloadGameService.PrepareForDownloadAsync(gameBiz, gameFolder, voiceLanguage);
    }


    private async Task DownloadAsync()
    {
        _timer.Start();
        IsActionButtonEnable = true;
        ActionButtonIcon = PauseIcon;
        ActionButtonText = "暂停";

        lastMilliseconds = _stopwatch.ElapsedMilliseconds;
        lastProgressBytes = _downloadGameService.ProgressBytes;
        tokenSource = new CancellationTokenSource();
        await _downloadGameService.DownloadAsync(tokenSource.Token);
    }


    private async Task VerifyAsync()
    {
        try
        {
            _timer.Start();
            IsActionButtonEnable = true;
            ActionButtonIcon = NextIcon;
            ActionButtonText = "跳过";

            lastMilliseconds = _stopwatch.ElapsedMilliseconds;
            lastProgressBytes = 0;
            tokenSource = new CancellationTokenSource();
            var list = await _downloadGameService.VerifyPackageAsync(tokenSource.Token);
            if (list.Any())
            {
                var dialog = new ContentDialog
                {
                    Title = "校验失败",
                    Content = $"""
                    以下文件校验失败：
                    {string.Join("\r\n", list)}
                    """,
                    PrimaryButtonText = "重新下载",
                    SecondaryButtonText = "忽略",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    foreach (var name in list)
                    {
                        var files = Directory.GetFiles(gameFolder, $"{name}.*");
                        foreach (var file in files)
                        {
                            File.Delete(file);
                        }
                    }
                    _ = DownloadAsync();
                }
            }
        }
        catch { }
    }



    private async Task DecompressAsync()
    {
        _timer.Start();
        IsActionButtonEnable = false;
        ActionButtonIcon = ErrorIcon;
        ActionButtonText = "不可中断";

        lastMilliseconds = _stopwatch.ElapsedMilliseconds;
        lastProgressBytes = 0;
        await _downloadGameService.DecompressAsync();
    }




    [RelayCommand]
    private async Task ClickActionButtonAsync()
    {
        try
        {
            if (isFinish)
            {
                return;
            }
            var state = _downloadGameService.State;
            if (state is DownloadGameService.DownloadGameState.Verifying)
            {
                var dialog = new ContentDialog
                {
                    Title = "跳过校验",
                    Content = "解压未校验的文件可能会导致游戏文件损坏",
                    PrimaryButtonText = "跳过",
                    SecondaryButtonText = "取消",
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    tokenSource?.Cancel();
                }
            }
            if (state is DownloadGameService.DownloadGameState.Downloading)
            {
                tokenSource?.Cancel();
                _timer.Stop();
                ActionButtonIcon = StartIcon;
                ActionButtonText = "继续";
                StateText = "下载已暂停";
                SpeedText = null;
                RemainTimeText = null;
            }
            if (state is DownloadGameService.DownloadGameState.Prepared)
            {
                _ = DownloadAsync();
            }
            if (state is DownloadGameService.DownloadGameState.Error)
            {
                _ = PrepareForDownloadAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Click action button");
        }
    }





}
