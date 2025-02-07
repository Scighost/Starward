using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.Windows.AppLifecycle;
using Starward.Core;
using Starward.Helpers;
using Starward.Services.InstallGame;
using Starward.Services.Launcher;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Numerics;
using System.Security.Principal;
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
public sealed partial class DownloadGamePage : PageBase
{

    private readonly ILogger<DownloadGamePage> _logger = AppConfig.GetLogger<DownloadGamePage>();

    //private readonly LauncherContentService _launcherContentService = AppConfig.GetService<LauncherContentService>();

    private readonly LauncherBackgroundService _launcherBackgroundService = AppConfig.GetService<LauncherBackgroundService>();

    private readonly InstallGameService _installGameService;

    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;



    public DownloadGamePage()
    {
        this.InitializeComponent();
        InstallGameWindow.Current.ChangeAccentColor(null, null);
        InstallGameWindow.Current.AppWindow.Closing += AppWindow_Closing;

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromMilliseconds(100);
        _timer.Tick += _timer_Tick;

        GameBiz gameBiz = AppConfig.Configuration.GetValue<string>("biz");
        gameFolder = AppConfig.Configuration.GetValue<string>("loc")!;
        voiceLanguage = AppConfig.Configuration.GetValue<AudioLanguage>("lang");

        _installGameService = gameBiz.ToGame().Value switch
        {
            GameBiz.bh3 => AppConfig.GetService<Honkai3rdInstallGameService>(),
            GameBiz.hk4e => AppConfig.GetService<GenshinInstallGameService>(),
            GameBiz.hkrpg => AppConfig.GetService<StarRailInstallGameService>(),
            GameBiz.nap => AppConfig.GetService<ZZZInstallGameService>(),
            _ => null!,
        };
    }



    private async void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        args.Cancel = true;
        if (_installGameService.State is InstallGameState.None or InstallGameState.Error || isFinish)
        {
            Exit();
            return;
        }
        var dialog = new ContentDialog
        {
            // 关闭软件
            Title = Lang.DownloadGamePage_CloseSoftware,
            // 下次启动后可恢复下载；\r\n解压过程中关闭程序会造成游戏文件损坏。
            Content = Lang.DownloadGamePage_CloseSoftwareContent,
            // 关闭
            PrimaryButtonText = Lang.Common_Exit,
            // 取消
            CloseButtonText = Lang.Common_Cancel,
            DefaultButton = ContentDialogButton.Close,
            XamlRoot = this.XamlRoot,
        };
        var result = await dialog.ShowAsync();
        if (result is ContentDialogResult.Primary)
        {
            Exit();
        }
    }


    private void Exit()
    {
        InstallGameWindow.Current.Close();
    }



    private bool repairMode;

    private bool reinstallMode;


    private GameBiz gameBiz;


    private string gameFolder;


    private AudioLanguage voiceLanguage;



    protected override async void OnLoaded()
    {
        try
        {
            await Task.Delay(16);
            await CheckAvailableAsync();
            _ = GetBgAsync();
            IsContentVisible = true;
            StartInstallGame();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error when loaded");
        }
    }


    protected override void OnUnloaded()
    {
        _installGameService.StateChanged -= _installGameService_StateChanged;
        _installGameService.Cancel();
    }



    private async Task CheckAvailableAsync()
    {
        var instance = AppInstance.FindOrRegisterForKey($"download_game_{gameBiz}");
        if (!instance.IsCurrent)
        {
            var dialog = new ContentDialog
            {
                // 重复启动
                Title = Lang.DownloadGamePage_RepeatStart,
                // 下载任务正在运行
                Content = string.Format(Lang.DownloadGamePage_TheDownloadTaskOfGameIsAlreadyRunning, gameBiz.ToGameName()),
                PrimaryButtonText = Lang.Common_Exit,
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            await instance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
            Environment.Exit(0);
        }
        if (!gameBiz.IsKnown() || gameBiz == GameBiz.clgm_cn || _installGameService is null)
        {
            instance.UnregisterKey();
            var dialog = new ContentDialog
            {
                // 游戏区服错误
                Title = Lang.DownloadGamePage_ParameterError,
                Content = $"({gameBiz}) is not supported.",
                PrimaryButtonText = Lang.Common_Exit,
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            Environment.Exit(0);
        }
        if (!Directory.Exists(gameFolder))
        {
            instance.UnregisterKey();
            var dialog = new ContentDialog
            {
                // 文件夹不存在
                Title = Lang.DownloadGamePage_ParameterError,
                Content = $"{Lang.DownloadGamePage_TheFolderDoesNotExist}\r\n{gameFolder}",
                PrimaryButtonText = Lang.Common_Exit,
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
                PrimaryButtonText = Lang.Common_Exit,
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
            Environment.Exit(0);
        }

        var args = Environment.GetCommandLineArgs();
        if (args[1].ToLower() is "repair")
        {
            repairMode = true;
        }
        if (args[1].ToLower() is "reinstall")
        {
            reinstallMode = true;
        }
        _installGameService.Initialize(gameBiz, gameFolder, voiceLanguage, repairMode, reinstallMode);
        _installGameService.StateChanged += _installGameService_StateChanged;
    }



    [ObservableProperty]
    private bool isContentVisible;




    #region Background


    [ObservableProperty]
    private ImageSource backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Image/StartUpBG2.png"));

    private async Task GetBgAsync()
    {
        try
        {
            var file = _launcherBackgroundService.GetCachedBackgroundImage(gameBiz, true);
            if (!File.Exists(file))
            {
                file = await _launcherBackgroundService.GetBackgroundImageAsync(gameBiz, true);
            }
            if (file != null)
            {
                using var fs = File.OpenRead(file);
                var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
                int decodeWidth = (int)ActualWidth;
                int decodeHeight = (int)ActualHeight;
                WriteableBitmap bitmap = new WriteableBitmap(decodeWidth, decodeHeight);
                fs.Position = 0;
                await bitmap.SetSourceAsync(fs.AsRandomAccessStream());
                (Color? back, Color? fore) = AccentColorHelper.GetAccentColor(bitmap.PixelBuffer, decodeWidth, decodeHeight);
                InstallGameWindow.Current.ChangeAccentColor(back, fore);
                BackgroundImage = bitmap;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "get bg");
        }
    }


    #endregion




    #region Progress bar animation


    AmbientLight ambientLight;
    PointLight pointLight;
    Vector3KeyFrameAnimation pointLightAnimation;


    private async void ProgressBar_Download_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(1000);
        var visual = ElementCompositionPreview.GetElementVisual(ProgressBar_Download);
        var com = visual.Compositor;
        ambientLight = com.CreateAmbientLight();
        ambientLight.Color = Colors.White;
        ambientLight.Intensity = 1f;
        ambientLight.Targets.Add(visual);

        float width = (float)ProgressBar_Download.ActualWidth;
        float height = (float)ProgressBar_Download.ActualHeight;

        pointLight = com.CreatePointLight();
        pointLight.Color = Colors.White;
        pointLight.CoordinateSpace = visual;
        pointLight.Intensity = 0.6f;
        pointLight.Targets.Add(visual);
        pointLight.MaxAttenuationCutoff = height * 4;

        pointLightAnimation = com.CreateVector3KeyFrameAnimation();
        pointLightAnimation.Duration = TimeSpan.FromSeconds(2);
        pointLightAnimation.InsertKeyFrame(0.00f, new Vector3(-height * 4, height / 2, height * 2));
        pointLightAnimation.InsertKeyFrame(0.25f, new Vector3(-height * 4, height / 2, height * 2));
        pointLightAnimation.InsertKeyFrame(0.75f, new Vector3(width + height * 4, height / 2, height * 2), com.CreateLinearEasingFunction());
        pointLightAnimation.InsertKeyFrame(1.00f, new Vector3(width + height * 4, height / 2, height * 2), com.CreateLinearEasingFunction());
        pointLightAnimation.IterationBehavior = AnimationIterationBehavior.Forever;
        StartProgressAnimation();
    }


    private void StartProgressAnimation()
    {
        pointLight?.StartAnimation(nameof(pointLight.Offset), pointLightAnimation);
    }


    private void StopProgressAnimation()
    {
        if (pointLight != null)
        {
            pointLight.Offset = new Vector3(-1000, 0, 0);
            pointLight.StopAnimation(nameof(pointLight.Offset));
        }
    }


    #endregion




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
    private string actionType;

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

    private long lastTimeTicks;



    private void _timer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        var arg = new StateEventArgs
        {
            State = _installGameService.State,
            TotalBytes = _installGameService.TotalBytes,
            ProgressBytes = _installGameService.ProgressBytes,
            TotalCount = _installGameService.TotalCount,
            ProgressCount = _installGameService.ProgressCount,
        };
        OnStateOrProgressChanged(arg);
    }


    private void _installGameService_StateChanged(object? sender, StateEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => OnStateOrProgressChanged(e));
    }



    private void OnStateOrProgressChanged(StateEventArgs e)
    {
        try
        {
            switch (e.State)
            {
                case InstallGameState.None:
                    return;
                case InstallGameState.Prepare:
                    StateText = Lang.DownloadGamePage_Preparing;
                    break;
                case InstallGameState.Download:
                    StateText = Lang.DownloadGamePage_Downloading;
                    IsActionButtonEnable = true;
                    ActionButtonIcon = PauseIcon;
                    ActionButtonText = Lang.DownloadGamePage_Pause;
                    break;
                case InstallGameState.Verify:
                    StateText = Lang.DownloadGamePage_Verifying;
                    if (_installGameService.IsRepairMode)
                    {
                        IsActionButtonEnable = false;
                        ActionButtonIcon = PauseIcon;
                        ActionButtonText = Lang.DownloadGamePage_Verifying;
                    }
                    else
                    {
                        IsActionButtonEnable = true;
                        ActionButtonIcon = NextIcon;
                        ActionButtonText = Lang.DownloadGamePage_Skip;
                    }
                    break;
                case InstallGameState.Decompress:
                    StateText = Lang.DownloadGamePage_Decompressing;
                    IsActionButtonEnable = false;
                    ActionButtonIcon = ErrorIcon;
                    ActionButtonText = Lang.DownloadGamePage_Stop;
                    break;
                case InstallGameState.Merge:
                    StateText = Lang.DownloadGamePage_Merging;
                    break;
                case InstallGameState.Finish:
                    _timer.Stop();
                    FinishTask();
                    return;
                case InstallGameState.Error:
                    _timer.Stop();
                    ShowErrorMessage(e.Exception!);
                    break;
                default:
                    break;
            }
            if (e.StateChanged)
            {
                lastProgressBytes = e.ProgressBytes;
                lastTimeTicks = Stopwatch.GetTimestamp();
            }
            UpdateDownloadProgress(e);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "On state or progress changed");
        }
    }



    private async void ShowErrorMessage(Exception exception)
    {
        try
        {
            if (exception is TaskCanceledException)
            {
                _logger.LogInformation("Task canceled");
                return;
            }

            // 出错了
            StateText = Lang.DownloadGamePage_SomethingError;
            SpeedText = null;
            RemainTimeText = null;
            IsActionButtonEnable = true;
            ActionButtonIcon = StartIcon;
            // 重试
            ActionButtonText = Lang.DownloadGamePage_Retry;
            StopProgressAnimation();

            if (exception is CheckSumFailedException ex1)
            {
                await ShowVerifyFailedDialogAsync(ex1);
                return;
            }

            var dialog = new ContentDialog()
            {
                // 打开日志
                PrimaryButtonText = Lang.DownloadGamePage_OpenLog,
                // 取消
                CloseButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot,
            };
            if (exception is HttpRequestException ex2)
            {
                // 网络错误
                dialog.Title = Lang.DownloadGamePage_NetworkError;
                dialog.Content = ex2.Message;
            }
            else
            {
                // 未知错误
                dialog.Title = Lang.DownloadGamePage_UnknownError;
                dialog.Content = exception.Message;
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



    private void UpdateDownloadProgress(StateEventArgs args)
    {
        const double GB = 1 << 30;
        long thisProgressBytes = args.ProgressBytes;
        long thisTimeTicks = Stopwatch.GetTimestamp();
        double progress = 0;
        if (_installGameService.IsRepairMode && args.State is InstallGameState.Verify)
        {
            progress = (double)args.ProgressCount / args.TotalCount;
            ProgressBytesText = $"{args.ProgressCount}/{args.TotalCount}";
            RemainTimeText = null;
        }
        else if (args.State is InstallGameState.Merge)
        {
            progress = (double)args.ProgressCount / args.TotalCount;
            ProgressBytesText = $"{args.ProgressCount}/{args.TotalCount}";
            SpeedText = null;
            RemainTimeText = null;
        }
        else
        {
            progress = (double)thisProgressBytes / args.TotalBytes;
            ProgressBytesText = $"{thisProgressBytes / GB:F2}/{args.TotalBytes / GB:F2} GB";
        }
        progress = double.IsNormal(progress) ? progress : 0;
        ProgressValue = progress * 100;
        ProgressText = progress.ToString("P1");


        if (thisTimeTicks - lastTimeTicks >= Stopwatch.Frequency)
        {
            double speed = (thisProgressBytes - lastProgressBytes) / Stopwatch.GetElapsedTime(lastTimeTicks, thisTimeTicks).TotalSeconds;
            if (speed >= 0)
            {
                SpeedText = $"{speed / (1 << 20):F2} MB/s";
                if (_installGameService.IsRepairMode && args.State is InstallGameState.Verify)
                {
                    RemainTimeText = null;
                }
                else if (args.State is InstallGameState.Merge)
                {
                    SpeedText = null;
                    RemainTimeText = null;
                }
                else
                {
                    if (speed == 0)
                    {
                        RemainTimeText = "-";
                    }
                    else
                    {
                        var remainTime = TimeSpan.FromSeconds((args.TotalBytes - thisProgressBytes) / speed);
                        RemainTimeText = $"{remainTime.Days * 24 + remainTime.Hours}h {remainTime.Minutes}m {remainTime.Seconds}s";
                    }
                }
            }
            lastProgressBytes = thisProgressBytes;
            lastTimeTicks = thisTimeTicks;
        }
    }



    private void FinishTask()
    {
        isFinish = true;
        IsActionButtonEnable = true;
        ActionButtonIcon = FinishIcon;
        ActionButtonText = Lang.DownloadGamePage_Finished;
        IsProgressStateVisible = false;
        ProgressValue = 100;
        StopProgressAnimation();
    }




    private void StartInstallGame(bool skipVerify = false)
    {
        StartProgressAnimation();
        IsActionButtonEnable = false;
        _ = _installGameService.StartAsync(skipVerify);
        _timer.Start();
    }




    [RelayCommand]
    private async Task ClickActionButtonAsync()
    {
        try
        {
            if (_installGameService.State is InstallGameState.Finish)
            {
                Exit();
                return;
            }
            var state = _installGameService.State;
            if (state is InstallGameState.Verify)
            {
                if (_installGameService.IsRepairMode)
                {
                    return;
                }
                var dialog = new ContentDialog
                {
                    // 跳过校验
                    Title = Lang.DownloadGamePage_SkipVerification,
                    // 跳过校验可能会导致游戏文件损坏
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
                    _installGameService.Cancel();
                    StartInstallGame(true);
                }
            }
            else if (state is InstallGameState.Download)
            {
                _timer.Start();
                _installGameService.Cancel();
                ActionButtonIcon = StartIcon;
                // 继续
                ActionButtonText = Lang.Common_Continue;
                // 下载已暂停
                StateText = Lang.DownloadGamePage_DownloadPaused;
                SpeedText = null;
                RemainTimeText = null;
                StopProgressAnimation();
            }
            else
            {
                StartInstallGame();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Click action button");
        }
    }



    private async Task ShowVerifyFailedDialogAsync(CheckSumFailedException exception)
    {
        try
        {
            var dialog = new ContentDialog
            {
                Title = Lang.DownloadGamePage_VerifyFailed,
                PrimaryButtonText = Lang.DownloadGamePage_Redownload,
                SecondaryButtonText = Lang.DownloadGamePage_SkipVerification,
                CloseButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = XamlRoot,
            };
            var content = new StackPanel();
            content.Children.Add(new TextBlock
            {
                Text = string.Format(Lang.DownloadGamePage_0FilesVerifyFailed, exception.Files.Count),
                TextWrapping = TextWrapping.Wrap,
            });
            var button = new Button
            {
                Content = Lang.DownloadGamePage_OpenLog,
                Margin = new Thickness(0, 16, 0, 0),
            };
            button.Click += async (s, e) =>
            {
                await Launcher.LaunchUriAsync(new Uri(AppConfig.LogFile));
            };
            content.Children.Add(button);
            dialog.Content = content;
            var result = await dialog.ShowAsync();
            if (result is ContentDialogResult.Primary)
            {
                _installGameService.Cancel();
                await _installGameService.DeleteDownloadedFilesAsync(exception.Files);
                StartInstallGame();
            }
            else if (result is ContentDialogResult.Secondary)
            {
                _installGameService.Cancel();
                StartInstallGame(true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Show verify failed dialog");
        }
    }




}
