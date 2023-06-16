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
            // 关闭软件
            Title = Lang.DownloadGamePage_CloseSoftware,
            // 下次启动后可恢复下载；\r\n解压过程中关闭程序会造成游戏文件损坏。
            Content = Lang.DownloadGamePage_CloseSoftwareContent,
            // 关闭
            PrimaryButtonText = Lang.DownloadGamePage_Close,
            // 取消
            SecondaryButtonText = Lang.Common_Cancel,
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
        var args = Environment.GetCommandLineArgs();
        var config = new ConfigurationBuilder().AddCommandLine(args).Build();
        var str_biz = config["biz"];
        var str_loc = config["loc"];
        var str_lang = config["lang"];
        Enum.TryParse(str_biz, out GameBiz biz);
        int.TryParse(str_lang, out int lang);
        gameBiz = biz;
        gameFolder = str_loc!;
        voiceLanguage = (VoiceLanguage)lang;
        if (args[1].ToLower() is "repair")
        {
            repairMode = true;
        }
    }


    private bool repairMode;


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
                // 重复启动
                Title = Lang.DownloadGamePage_RepeatStart,
                // 程序即将退出
                Content = Lang.DownloadGamePage_SoftwareWillBeExitedSoon,
                // 确定
                PrimaryButtonText = Lang.Common_Confirm,
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            await instance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
            Environment.Exit(0);
        }
        if (gameBiz.ToGame() is GameBiz.None || gameBiz is GameBiz.hk4e_cloud || (repairMode && gameBiz.ToGame() != GameBiz.GenshinImpact) || !Directory.Exists(gameFolder))
        {
            instance.UnregisterKey();
            var dialog = new ContentDialog
            {
                // 参数错误
                Title = Lang.DownloadGamePage_ParameterError,
                // 程序即将退出
                Content = Lang.DownloadGamePage_SoftwareWillBeExitedSoon,
                // 确定
                PrimaryButtonText = Lang.Common_Confirm,
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
                // 权限不足
                Title = Lang.DownloadGamePage_NoPermission,
                // 请使用管理员身份启动
                Content = Lang.DownloadGamePage_PleaseStartAsAdministrator,
                // 确定
                PrimaryButtonText = Lang.Common_Confirm,
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


    private const string StartIcon = "\uE768";
    private const string PauseIcon = "\uE769";
    private const string NextIcon = "\uE893";
    private const string StopIcon = "\uE71A";
    private const string ErrorIcon = "\uEA39";
    private const string FinishIcon = "\uE930";


    [ObservableProperty]
    private string? actionButtonText = Lang.DownloadGamePage_Start; // 开始


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
                // 准备中
                StateText = Lang.DownloadGamePage_Preparing;
                return;
            }
            if (state is DownloadGameService.DownloadGameState.Prepared)
            {
                if (repairMode)
                {
                    _ = VerifyAsync();
                }
                else
                {
                    _ = DownloadAsync();
                }
            }
            if (state is DownloadGameService.DownloadGameState.Downloading)
            {
                // 下载中
                StateText = Lang.DownloadGamePage_Downloading;
            }
            if (state is DownloadGameService.DownloadGameState.Downloaded)
            {
                if (repairMode)
                {
                    FinishTask();
                    return;
                }
                else
                {
                    _ = VerifyAsync();
                }
            }
            if (state is DownloadGameService.DownloadGameState.Verifying)
            {
                // 校验中
                StateText = Lang.DownloadGamePage_Verifying;
            }
            if (state is DownloadGameService.DownloadGameState.Verified)
            {
                if (repairMode)
                {
                    _ = DownloadAsync();
                }
                else
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
            }
            if (state is DownloadGameService.DownloadGameState.Decompressing)
            {
                // 解压中
                StateText = Lang.DownloadGamePage_Decompressing;
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
            // 出错了
            StateText = Lang.DownloadGamePage_SomethingError;
            SpeedText = null;
            RemainTimeText = null;
            IsActionButtonEnable = true;
            ActionButtonIcon = StartIcon;
            // 重试
            ActionButtonText = Lang.DownloadGamePage_Retry;

            var dialog = new ContentDialog()
            {
                // 打开日志
                PrimaryButtonText = Lang.DownloadGamePage_OpenLog,
                // 取消
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            if (_downloadGameService.ErrorType is nameof(HttpRequestException))
            {
                // 网络错误
                dialog.Title = Lang.DownloadGamePage_NetworkError;
                dialog.Content = _downloadGameService.ErrorMessage;
            }
            else
            {
                // 未知错误
                dialog.Title = Lang.DownloadGamePage_UnknownError;
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
        double progress = 0;
        if (repairMode && _downloadGameService.State is DownloadGameService.DownloadGameState.Verifying)
        {
            progress = (double)_downloadGameService.ProgressCount / _downloadGameService.TotalCount;
            ProgressBytesText = $"{_downloadGameService.ProgressCount}/{_downloadGameService.TotalCount}";
            RemainTimeText = null;
        }
        else
        {
            progress = (double)thisProgressBytes / _downloadGameService.TotalBytes;
            ProgressBytesText = $"{thisProgressBytes / GB:F2}/{_downloadGameService.TotalBytes / GB:F2} GB";
        }
        progress = double.IsNormal(progress) ? progress : 0;
        ProgressValue = progress * 100;
        ProgressText = progress.ToString("P1");


        if (thisMilliseconds - lastMilliseconds > 1000)
        {
            double speed = (double)(thisProgressBytes - lastProgressBytes) / (thisMilliseconds - lastMilliseconds) * 1000;
            if (speed > 0)
            {
                SpeedText = $"{speed / (1 << 20):F2} MB/s";
                if (repairMode && _downloadGameService.State is DownloadGameService.DownloadGameState.Verifying)
                {
                    RemainTimeText = null;
                }
                else
                {
                    var remainTime = TimeSpan.FromSeconds((_downloadGameService.TotalBytes - thisProgressBytes) / speed);
                    RemainTimeText = $"{remainTime.Days * 24 + remainTime.Hours}h {remainTime.Minutes}m {remainTime.Seconds}s";
                }
            }
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
        // 已完成
        ActionButtonText = Lang.DownloadGamePage_Finished;
        IsProgressStateVisible = false;
        ProgressValue = 100;
    }




    private async Task PrepareForDownloadAsync()
    {
        _timer.Start();
        IsActionButtonEnable = false;
        lastMilliseconds = _stopwatch.ElapsedMilliseconds;
        if (repairMode)
        {
            await _downloadGameService.PrepareForRepairAsync(gameBiz, gameFolder, voiceLanguage);
        }
        else
        {
            lastProgressBytes = _downloadGameService.ProgressBytes;
            decompress = await _downloadGameService.PrepareForDownloadAsync(gameBiz, gameFolder, voiceLanguage);
        }
    }


    private async Task DownloadAsync()
    {
        _timer.Start();
        IsActionButtonEnable = true;
        ActionButtonIcon = PauseIcon;
        // 暂停
        ActionButtonText = Lang.DownloadGamePage_Pause;

        lastMilliseconds = _stopwatch.ElapsedMilliseconds;
        lastProgressBytes = _downloadGameService.ProgressBytes;
        tokenSource = new CancellationTokenSource();
        if (repairMode)
        {
            await _downloadGameService.DownloadSeparateFilesAsync(tokenSource.Token);
        }
        else
        {
            await _downloadGameService.DownloadAsync(tokenSource.Token);
        }
    }


    private async Task VerifyAsync()
    {
        try
        {
            _timer.Start();

            if (repairMode)
            {
                IsActionButtonEnable = false;
                ActionButtonIcon = PauseIcon;
                // 校验中
                ActionButtonText = Lang.DownloadGamePage_Verifying;
                tokenSource = new CancellationTokenSource();
                await _downloadGameService.VerifySeparateFilesAsync(tokenSource.Token);
            }
            else
            {
                IsActionButtonEnable = true;
                ActionButtonIcon = NextIcon;
                // 跳过
                ActionButtonText = Lang.DownloadGamePage_Skip;

                lastMilliseconds = _stopwatch.ElapsedMilliseconds;
                lastProgressBytes = 0;
                tokenSource = new CancellationTokenSource();
                var list = await _downloadGameService.VerifyPackageAsync(tokenSource.Token);
                if (list.Any())
                {
                    var dialog = new ContentDialog
                    {
                        // 校验失败
                        Title = Lang.DownloadGamePage_VerifyFailed,
                        // 以下文件校验失败
                        Content = $"""
                        Lang.DownloadGamePage_TheFollowingFileVerifyFailed
                        {string.Join("\r\n", list)}
                        """,
                        // 重新下载
                        PrimaryButtonText = Lang.DownloadGamePage_Redownload,
                        // 忽略
                        SecondaryButtonText = Lang.DownloadGamePage_Ignore,
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
        }
        catch { }
    }



    private async Task DecompressAsync()
    {
        _timer.Start();
        IsActionButtonEnable = false;
        ActionButtonIcon = ErrorIcon;
        // 终止
        ActionButtonText = Lang.DownloadGamePage_Stop;

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
                if (repairMode)
                {
                    return;
                }
                var dialog = new ContentDialog
                {
                    // 跳过校验
                    Title = Lang.DownloadGamePage_SkipVerification,
                    // 解压未校验的文件可能会导致游戏文件损坏
                    Content = Lang.DownloadGamePage_SkipVerificationContent,
                    // 跳过
                    PrimaryButtonText = Lang.DownloadGamePage_Skip,
                    // 取消
                    SecondaryButtonText = Lang.Common_Cancel,
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
                // 继续
                ActionButtonText = Lang.DownloadGamePage_Pause;
                // 下载已暂停
                StateText = Lang.DownloadGamePage_DownloadPaused;
                SpeedText = null;
                RemainTimeText = null;
            }
            if (state is DownloadGameService.DownloadGameState.Prepared)
            {
                _ = DownloadAsync();
            }
            if (state is DownloadGameService.DownloadGameState.Verified && repairMode)
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
