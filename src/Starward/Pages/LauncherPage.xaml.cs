// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Starward.Controls;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Helpers;
using Starward.Models;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Vanara.PInvoke;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class LauncherPage : Page
{

    private readonly ILogger<LauncherPage> _logger = AppConfig.GetLogger<LauncherPage>();

    private readonly GameService _gameService = AppConfig.GetService<GameService>();

    private readonly LauncherService _launcherService = AppConfig.GetService<LauncherService>();

    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();

    private readonly DatabaseService _databaseService = AppConfig.GetService<DatabaseService>();

    private readonly DownloadGameService _downloadGameService = AppConfig.GetService<DownloadGameService>();

    private readonly SystemTrayService _systemTrayService = AppConfig.GetService<SystemTrayService>();

    private readonly Microsoft.UI.Dispatching.DispatcherQueueTimer _timer;

    private GameBiz gameBiz;



    public LauncherPage()
    {
        this.InitializeComponent();

        if (AppConfig.WindowSizeMode > 0)
        {
            Grid_BannerAndPost.Width = 364;
            RowDefinition_BannerAndPost.Height = new GridLength(168);
        }

        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(5);
        _timer.IsRepeating = true;
        _timer.Tick += _timer_Tick;
    }


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
        try
        {
            UpdateStartGameButtonStyle();
            InitializeCommandTrigger();
            await Task.Delay(16);
            InitializeGameBiz();
            UpdateGameVersion();
            InitializePlayTime();
            UpdateGameState();
            GetGameAccount();
            await GetLauncherContentAsync();
            await GetGameNoticesAlertAsync();
        }
        catch { }
    }


    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _timer.Stop();
        GameProcess?.Dispose();
    }



    private void InitializeGameBiz()
    {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        try
        {
            StartGameArgument = AppConfig.GetStartArgument(gameBiz);
            EnableThirdPartyTool = AppConfig.GetEnableThirdPartyTool(gameBiz);
            ThirdPartyToolPath = AppConfig.GetThirdPartyToolPath(gameBiz);
            enableCustomBg = AppConfig.GetEnableCustomBg(gameBiz);
            OnPropertyChanged(nameof(EnableCustomBg));
            CustomBg = AppConfig.GetCustomBg(gameBiz);

            if (gameBiz is GameBiz.hk4e_cloud)
            {
                Button_UninstallGame.IsEnabled = false;
                Grid_BannerAndPost.HorizontalAlignment = HorizontalAlignment.Right;
            }
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field 
        }
        catch { }
    }






    #region Anncounce & Post



    [ObservableProperty]
    private List<LauncherBanner> bannerList;


    [ObservableProperty]
    private List<LauncherPostGroup> launcherPostGroupList;


    [ObservableProperty]
    private bool enableBannerAndPost = AppConfig.EnableBannerAndPost;
    partial void OnEnableBannerAndPostChanged(bool value)
    {
        AppConfig.EnableBannerAndPost = value;
        Grid_BannerAndPost.Opacity = value ? 1 : 0;
        Grid_BannerAndPost.IsHitTestVisible = value;
        if (value)
        {
            _timer.Start();
        }
        else
        {
            _timer.Stop();
        }
    }



    private async Task GetLauncherContentAsync()
    {
        try
        {
            var content = await _launcherService.GetLauncherContentAsync(gameBiz);
            BannerList = content.Banner;
            LauncherPostGroupList = content.Post.GroupBy(x => x.Type).OrderBy(x => x.Key).Select(x => new LauncherPostGroup(x.Key.ToLocalization(), x)).ToList();
            if (EnableBannerAndPost && BannerList.Any() && LauncherPostGroupList.Any())
            {
                Grid_BannerAndPost.Opacity = 1;
                Grid_BannerAndPost.IsHitTestVisible = true;
                _timer.Start();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Cannot get game launcher content ({gamebiz}): {error}", gameBiz, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game launcher content ({gamebiz})", gameBiz);
        }
    }



    private void _timer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (BannerList?.Any() ?? false)
            {
                PipsPager_Banner.SelectedPageIndex = (PipsPager_Banner.SelectedPageIndex + 1) % PipsPager_Banner.NumberOfPages;
            }
        }
        catch { }
    }


    private async void Image_Banner_Tapped(object sender, TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is LauncherBanner banner)
            {
                _logger.LogInformation("Open banner {title}: {url}", banner.Name, banner.Url);
                await Windows.System.Launcher.LaunchUriAsync(new Uri(banner.Url));
            }
        }
        catch { }
    }


    private void FlipView_Banner_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var grid = VisualTreeHelper.GetChild(FlipView_Banner, 0);
            if (grid != null)
            {
                var count = VisualTreeHelper.GetChildrenCount(grid);
                if (count > 0)
                {
                    for (int i = 0; i < count; i++)
                    {
                        var child = VisualTreeHelper.GetChild(grid, i);
                        if (child is Button button)
                        {
                            button.IsHitTestVisible = false;
                            button.Opacity = 0;
                        }
                    }
                }
            }
        }
        catch { }
    }

    private void Grid_BannerContainer_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _timer.Stop();
        Border_PipsPager.Visibility = Visibility.Visible;
    }

    private void Grid_BannerContainer_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _timer.Start();
        Border_PipsPager.Visibility = Visibility.Collapsed;
    }




    private async Task GetGameNoticesAlertAsync()
    {
        try
        {
            long uid = 0;
            if (GameAccountList?.FirstOrDefault(x => x.IsLogin) is GameAccount account)
            {
                uid = account.Uid;
            }
            if (await _launcherService.IsNoticesAlertAsync(gameBiz, uid))
            {
                Image_GameNoticesAlert.Visibility = Visibility.Visible;
            }
            else
            {
                Image_GameNoticesAlert.Visibility = Visibility.Collapsed;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Game notices alert");
            Image_GameNoticesAlert.Visibility = Visibility.Collapsed;
        }
    }




    [RelayCommand]
    private void NavigateToGameNoticesPage()
    {
        MainPage.Current.NavigateTo(typeof(GameNoticesPage));
    }


    #endregion




    #region Start Game




    private Timer processTimer;


    [ObservableProperty]
    private bool canStartGame = true;
    partial void OnCanStartGameChanged(bool value)
    {
        UpdateStartGameButtonStyle();
    }



    private void UpdateStartGameButtonStyle()
    {
        if (MainPage.Current.IsPlayingVideo)
        {
            Button_StartGame.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;
            AnimatedIcon_GameSetting.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
        }
        else
        {
            if (CanStartGame || IsDownloadGameEnable || IsUpdateGameEnable)
            {
                AnimatedIcon_GameSetting.Foreground = Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] as Brush;
            }
            else
            {
                AnimatedIcon_GameSetting.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
            }
            Button_StartGame.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
        }
    }


    private void InitializeCommandTrigger()
    {
        StartGameCommand.CanExecuteChanged += (_, _) => UpdateStartGameButtonStyle();
        DownloadGameCommand.CanExecuteChanged += (_, _) => UpdateStartGameButtonStyle();
    }


    [ObservableProperty]
    private string startGameButtonText = Lang.LauncherPage_StartGame;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameEnable))]
    [NotifyPropertyChangedFor(nameof(IsDownloadGameEnable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateGameEnable))]
    [NotifyPropertyChangedFor(nameof(IsPreDownloadEnable))]
    [NotifyPropertyChangedFor(nameof(IsRepairGameEnable))]
    private Version? localVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameEnable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateGameEnable))]
    private Version? currentVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreDownloadEnable))]
    private Version? preVersion;



    public bool IsStartGameEnable => LocalVersion != null && LocalVersion >= CurrentVersion;


    public bool IsDownloadGameEnable => LocalVersion == null;


    public bool IsUpdateGameEnable => LocalVersion != null && CurrentVersion > LocalVersion;


    public bool IsPreDownloadEnable => LocalVersion != null && PreVersion != null;


    public bool IsRepairGameEnable => (gameBiz is GameBiz.hk4e_cn or GameBiz.hk4e_global || gameBiz.ToGame() is GameBiz.Honkai3rd) && LocalVersion != null;


    [ObservableProperty]
    private bool isPreDownloadOK;


    [ObservableProperty]
    private string? installPath;
    partial void OnInstallPathChanged(string? value)
    {
        AppConfig.SetGameInstallPath(gameBiz, value);
        _logger.LogInformation("Game install path {biz}: {path}", gameBiz, value);
    }


    [ObservableProperty]
    private bool enableThirdPartyTool;
    partial void OnEnableThirdPartyToolChanged(bool value)
    {
        AppConfig.SetEnableThirdPartyTool(gameBiz, value);
    }


    [ObservableProperty]
    private string? thirdPartyToolPath;
    partial void OnThirdPartyToolPathChanged(string? value)
    {
        AppConfig.SetThirdPartyToolPath(gameBiz, value);
    }


    [ObservableProperty]
    private Process? gameProcess;
    partial void OnGameProcessChanged(Process? oldValue, Process? newValue)
    {
        oldValue?.Dispose();
        processTimer?.Stop();
        if (newValue != null)
        {
            try
            {
                CanStartGame = false;
                StartGameButtonText = Lang.LauncherPage_GameIsRunning;
                newValue.EnableRaisingEvents = true;
                newValue.Exited += (_, _) => CheckGameExited();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
            {
                // Access is denied
                processTimer?.Start();
            }
        }
        else
        {
            _logger.LogInformation("Game process exited");
            DispatcherQueue.TryEnqueue(GetGameAccount);
        }
    }




    [ObservableProperty]
    private string? startGameArgument;
    partial void OnStartGameArgumentChanged(string? value)
    {
        AppConfig.SetStartArgument(gameBiz, value);
    }


    [ObservableProperty]
    private int targetFPS;
    partial void OnTargetFPSChanged(int value)
    {
        try
        {
            value = Math.Clamp(value, 60, 320);
            _gameService.SetStarRailFPS(gameBiz, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set StarRail FPS");
        }
    }


    [ObservableProperty]
    private bool ignoreRunningGame = AppConfig.IgnoreRunningGame;
    partial void OnIgnoreRunningGameChanged(bool value)
    {
        AppConfig.IgnoreRunningGame = value;
        UpdateGameState();
    }




    private async void UpdateGameVersion()
    {
        try
        {
            InstallPath = _gameService.GetGameInstallPath(gameBiz);
            if (gameBiz == GameBiz.hk4e_cloud)
            {
                if (Directory.Exists(InstallPath))
                {
                    LocalVersion = new Version();
                    UpdateStartGameButtonStyle();
                }
                return;
            }
            LocalVersion = await _downloadGameService.GetLocalGameVersionAsync(gameBiz);
            UpdateStartGameButtonStyle();
            (CurrentVersion, PreVersion) = await _downloadGameService.GetGameVersionAsync(gameBiz);
            if (IsPreDownloadEnable)
            {
                IsPreDownloadOK = await _downloadGameService.CheckPreDownloadIsOKAsync(gameBiz, InstallPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check game version");
        }
    }



    private void UpdateGameState()
    {
        try
        {
            CanStartGame = true;
            StartGameButtonText = Lang.LauncherPage_StartGame;
            if (IgnoreRunningGame)
            {
                return;
            }
            if (processTimer is null)
            {
                processTimer = new(1000);
                processTimer.Elapsed += (_, _) => CheckGameExited();
            }
            GameProcess = _gameService.GetGameProcess(gameBiz);
            if (GameProcess != null)
            {
                _logger.LogInformation("Game is running ({name}, {pid})", GameProcess.ProcessName, GameProcess.Id);
            }
        }
        catch { }
    }



    private void CheckGameExited()
    {
        try
        {
            if (GameProcess != null)
            {
                if (GameProcess.HasExited)
                {
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        CanStartGame = true;
                        StartGameButtonText = Lang.LauncherPage_StartGame;
                    });
                    GameProcess.Dispose();
                    GameProcess = null;
                }
            }
        }
        catch { }
    }



    [RelayCommand]
    private async Task StartGameAsync()
    {
        try
        {
            if (!IgnoreRunningGame)
            {
                var p = _gameService.GetGameProcess(gameBiz);
                if (p != null)
                {
                    GameProcess = p;
                    return;
                }
            }
            var process1 = _gameService.StartGame(gameBiz, IgnoreRunningGame);
            if (process1 != null)
            {
                MainPage.Current.PauseVideo();
                if (AppConfig.EnableSystemTrayIcon && _systemTrayService.IsCreated)
                {
                    User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_HIDE);
                }
                else
                {
                    User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWMINIMIZED);
                }
                _logger.LogInformation("Game started ({name}, {pid})", process1.ProcessName, process1.Id);
                if (AppConfig.IgnoreRunningGame)
                {
                    _ = _playTimeService.StartProcessToLogAsync(gameBiz);
                }
                else
                {
                    var process2 = await _playTimeService.StartProcessToLogAsync(gameBiz);
                    GameProcess = process2 ?? process1;
                }
            }
        }
        catch (FileNotFoundException ex)
        {
            NotificationBehavior.Instance.Warning($"File \"{ex.FileName}\" not found.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start game");
        }
    }



    [RelayCommand]
    private async Task ChangeGameInstallPathAsync()
    {
        try
        {
            var folder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.HWND);
            if (Directory.Exists(folder))
            {
                InstallPath = folder;
            }
            else
            {
                InstallPath = null;
            }
            UpdateGameVersion();
            UpdateGameState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change game install path ({biz})", gameBiz);
        }
    }



    [RelayCommand]
    private async Task ChangeThirdPartyPathAsync()
    {
        try
        {
            var file = await FileDialogHelper.PickSingleFileAsync(MainWindow.Current.HWND);
            if (File.Exists(file))
            {
                ThirdPartyToolPath = file;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change third party tool path ({biz})", gameBiz);
        }
    }


    [RelayCommand]
    private async Task OpenGameInstallFolderAsync()
    {
        try
        {
            if (Directory.Exists(InstallPath))
            {
                await Launcher.LaunchFolderPathAsync(InstallPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open game install folder {folder}", InstallPath);
        }
    }



    [RelayCommand]
    private async Task OpenThirdPartyToolFolderAsync()
    {
        try
        {
            if (File.Exists(ThirdPartyToolPath))
            {
                var folder = Path.GetDirectoryName(ThirdPartyToolPath);
                var file = await StorageFile.GetFileFromPathAsync(ThirdPartyToolPath);
                var option = new FolderLauncherOptions();
                option.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderPathAsync(folder, option);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open third party tool folder {folder}", ThirdPartyToolPath);
        }
    }


    [RelayCommand]
    private void DeleteGameInstallPath()
    {
        InstallPath = null;
        UpdateGameVersion();
    }


    [RelayCommand]
    private void DeleteThirdPartyToolPath()
    {
        ThirdPartyToolPath = null;
    }



    [ObservableProperty]
    private TimeSpan playTimeTotal;


    [ObservableProperty]
    private TimeSpan playTimeMonth;


    [ObservableProperty]
    private TimeSpan playTimeWeek;


    [ObservableProperty]
    private TimeSpan playTimeLast;


    [ObservableProperty]
    private string lastPlayTimeText;


    private void InitializePlayTime()
    {
        try
        {
            PlayTimeTotal = _databaseService.GetValue<TimeSpan>($"playtime_total_{gameBiz}", out _);
            PlayTimeMonth = _databaseService.GetValue<TimeSpan>($"playtime_month_{gameBiz}", out _);
            PlayTimeWeek = _databaseService.GetValue<TimeSpan>($"playtime_week_{gameBiz}", out _);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(gameBiz);
            if (time > DateTimeOffset.MinValue)
            {
                LastPlayTimeText = time.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize play time");
        }
    }



    [RelayCommand]
    private void UpdatePlayTime()
    {
        try
        {
            PlayTimeTotal = _playTimeService.GetPlayTimeTotal(gameBiz);
            PlayTimeMonth = _playTimeService.GetPlayCurrentMonth(gameBiz);
            PlayTimeWeek = _playTimeService.GetPlayCurrentWeek(gameBiz);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(gameBiz);
            if (time > DateTimeOffset.MinValue)
            {
                LastPlayTimeText = time.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            _databaseService.SetValue($"playtime_total_{gameBiz}", PlayTimeTotal);
            _databaseService.SetValue($"playtime_month_{gameBiz}", PlayTimeMonth);
            _databaseService.SetValue($"playtime_week_{gameBiz}", PlayTimeWeek);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update play time");
        }
    }


    [RelayCommand]
    private void OpenGameSetting()
    {
        SplitView_Content.IsPaneOpen = true;
    }



    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize == 14 && sender.IsTextTrimmed)
        {
            sender.FontSize = 12;
        }
    }


    #endregion




    #region Game Account



    [ObservableProperty]
    private List<GameAccount> gameAccountList;


    [ObservableProperty]
    private GameAccount? selectGameAccount;
    partial void OnSelectGameAccountChanged(GameAccount? value)
    {
        CanChangeGameAccount = value is not null;
    }


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ChangeGameAccountCommand))]
    private bool canChangeGameAccount;



    private void GetGameAccount()
    {
        try
        {
            GameAccountList = _gameService.GetGameAccounts(gameBiz).ToList();
            SelectGameAccount = GameAccountList.FirstOrDefault(x => x.IsLogin);
            CanChangeGameAccount = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot get game account ({biz})", gameBiz);
        }
    }




    [RelayCommand(CanExecute = nameof(CanChangeGameAccount))]
    private void ChangeGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                _gameService.ChangeGameAccount(SelectGameAccount);
                foreach (var item in GameAccountList)
                {
                    item.IsLogin = false;
                }
                CanChangeGameAccount = false;
                SelectGameAccount.IsLogin = true;
                _ = GetGameNoticesAlertAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot change game {biz} account to {name}", gameBiz, SelectGameAccount?.Name);
        }
    }


    [RelayCommand]
    private async Task SaveGameAccountAsync()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                var acc = SelectGameAccount;
                if (GameAccountList.FirstOrDefault(x => x.SHA256 != acc.SHA256 && x.Uid == acc.Uid) is GameAccount lacc)
                {
                    var dialog = new ContentDialog
                    {
                        Title = Lang.Common_Attention,
                        Content = string.Format(Lang.LauncherPage_AccountSaveNew, acc.Uid),
                        PrimaryButtonText = Lang.LauncherPage_Replace,
                        SecondaryButtonText = Lang.LauncherPage_SaveNew,
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot,
                    };
                    if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                    {
                        GameAccountList.Remove(lacc);
                        _gameService.DeleteGameAccount(lacc);
                    }
                }
                SelectGameAccount.Time = DateTime.Now;
                _gameService.SaveGameAccount(SelectGameAccount);
                FontIcon_SaveGameAccount.Glyph = "\uE8FB";
                _ = GetGameNoticesAlertAsync();
                await Task.Delay(3000);
                FontIcon_SaveGameAccount.Glyph = "\uE74E";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Save game account");
        }
    }


    [RelayCommand]
    private void DeleteGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                _gameService.DeleteGameAccount(SelectGameAccount);
                GetGameAccount();
                _ = GetGameNoticesAlertAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game account");
        }
    }









    #endregion




    #region Background



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoBgVolumeButtonIcon))]
    private int videoBgVolume = AppConfig.VideoBgVolume;
    partial void OnVideoBgVolumeChanged(int value)
    {
        if (MainPage.Current is not null)
        {
            MainPage.Current.VideoBgVolume = value;
        }
    }


    [ObservableProperty]
    private bool useOneBg = AppConfig.UseOneBg;
    partial void OnUseOneBgChanged(bool value)
    {
        AppConfig.UseOneBg = value;
        AppConfig.SetCustomBg(gameBiz, CustomBg);
        AppConfig.SetEnableCustomBg(gameBiz, EnableCustomBg);

    }


    public string VideoBgVolumeButtonIcon => VideoBgVolume switch
    {
        > 66 => "\uE995",
        > 33 => "\uE994",
        > 1 => "\uE993",
        _ => "\uE992",
    };


    private int notMuteVolume = 100;

    [RelayCommand]
    private void Mute()
    {
        if (VideoBgVolume > 0)
        {
            notMuteVolume = VideoBgVolume;
            VideoBgVolume = 0;
        }
        else
        {
            VideoBgVolume = notMuteVolume;
        }
    }


    [ObservableProperty]
    private bool enableCustomBg;
    partial void OnEnableCustomBgChanged(bool value)
    {
        AppConfig.SetEnableCustomBg(gameBiz, value);
        _ = MainPage.Current.UpdateBackgroundImageAsync(true);
        UpdateStartGameButtonStyle();
    }


    [ObservableProperty]
    private string? customBg;


    [ObservableProperty]
    private bool enableDynamicAccentColor = AppConfig.EnableDynamicAccentColor;
    partial void OnEnableDynamicAccentColorChanged(bool value)
    {
        AppConfig.EnableDynamicAccentColor = value;
        _ = MainPage.Current.UpdateBackgroundImageAsync();
    }


    [RelayCommand]
    private async Task ChangeCustomBgAsync()
    {
        var file = await _launcherService.ChangeCustomBgAsync();
        if (file is not null)
        {
            CustomBg = file;
            AppConfig.SetCustomBg(gameBiz, file);
            _ = MainPage.Current.UpdateBackgroundImageAsync(true);
        }
    }


    [RelayCommand]
    private async Task OpenCustomBgAsync()
    {
        await _launcherService.OpenCustomBgAsync(CustomBg);
    }


    [RelayCommand]
    private void DeleteCustomBg()
    {
        AppConfig.SetCustomBg(gameBiz, null);
        CustomBg = null;
        _ = MainPage.Current.UpdateBackgroundImageAsync(true);
    }




    #endregion





    #region Download Game




    private async Task<bool> CheckRedirectInstanceAsync()
    {
        var instance = App.FindInstanceForKey($"download_game_{gameBiz}");
        if (instance != null)
        {
            await instance.RedirectActivationToAsync(instance.GetActivatedEventArgs());
            return true;
        }
        else
        {
            return false;
        }
    }




    [RelayCommand]
    private async Task DownloadGameAsync()
    {
        try
        {
            if (gameBiz is GameBiz.hk4e_cloud)
            {
                await Launcher.LaunchUriAsync(new Uri("https://mhyy.mihoyo.com/"));
                return;
            }

            if (await CheckRedirectInstanceAsync())
            {
                return;
            }


            if (Directory.Exists(InstallPath))
            {
                if (LocalVersion is null)
                {
                    var folderDialog = new ContentDialog
                    {
                        Title = Lang.LauncherPage_SelectInstallFolder,
                        // 以下文件夹中有尚未完成的下载任务
                        Content = $"""
                        {Lang.LauncherPage_TheFollowingFolderContainsUnfinishedDownloadTasks}

                        {InstallPath}
                        """,
                        PrimaryButtonText = Lang.Common_Continue,
                        SecondaryButtonText = Lang.LauncherPage_Reselect,
                        CloseButtonText = Lang.Common_Cancel,
                        DefaultButton = ContentDialogButton.Primary,
                        XamlRoot = this.XamlRoot,
                    };
                    var result = await folderDialog.ShowAsync();
                    if (result is ContentDialogResult.Secondary)
                    {
                        var folder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.HWND);
                        if (Directory.Exists(folder))
                        {
                            InstallPath = folder;
                        }
                    }
                    if (result is ContentDialogResult.None)
                    {
                        return;
                    }
                }
            }
            else
            {
                var folderDialog = new ContentDialog
                {
                    Title = Lang.LauncherPage_SelectInstallFolder,
                    // 请选择一个空文件夹用于安装游戏，或者定位已安装游戏的文件夹。
                    Content = Lang.LauncherPage_SelectInstallFolderDesc,
                    PrimaryButtonText = Lang.Common_Select,
                    SecondaryButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot,
                };
                if (await folderDialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    var folder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.HWND);
                    if (Directory.Exists(folder))
                    {
                        InstallPath = folder;
                    }
                }
            }

            if (!Directory.Exists(InstallPath))
            {
                return;
            }

            var downloadResource = await _downloadGameService.CheckDownloadGameResourceAsync(gameBiz, InstallPath);
            if (downloadResource is null)
            {
                UpdateGameVersion();
                return;
            }
            var lang = await _downloadGameService.GetVoiceLanguageAsync(gameBiz, InstallPath);
            if (lang is VoiceLanguage.None)
            {
                lang = VoiceLanguage.All;
            }

            var content = new DownloadGameDialog { GameBiz = gameBiz, LanguageType = lang, GameResource = downloadResource, IsPreDownload = IsPreDownloadEnable };
            var dialog = new ContentDialog
            {
                Title = IsUpdateGameEnable ? Lang.LauncherPage_UpdateGame : (IsPreDownloadEnable ? Lang.LauncherPage_PreInstall : Lang.LauncherPage_InstallGame),
                Content = content,
                PrimaryButtonText = Lang.Common_Start,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };

            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                lang = content.LanguageType;
                string argu = $"""{(content.EnableRepairMode ? "repair" : "download")} --biz {gameBiz} --loc "{InstallPath}" --lang {(int)lang} """;
                ProcessHelper.StartAsAdmin(null, argu);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start to download game");
        }
    }






    [RelayCommand]
    private async Task PreDownloadGameAsync()
    {
        try
        {
            if (await CheckRedirectInstanceAsync())
            {
                return;
            }
            if (IsPreDownloadOK)
            {
                var dialog = new ContentDialog
                {
                    Title = Lang.LauncherPage_PreInstall,
                    // 预下载已完成，是否校验文件？
                    Content = Lang.LauncherPage_WouldYouLikeToVerifyTheFiles,
                    PrimaryButtonText = Lang.LauncherPage_StartVerification,
                    SecondaryButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    var lang = await _downloadGameService.GetVoiceLanguageAsync(gameBiz, InstallPath);
                    string argu = $"""download --biz {gameBiz} --loc "{InstallPath}" --lang {(int)lang} """;
                    ProcessHelper.StartAsAdmin(null, argu);
                }
            }
            else
            {
                await DownloadGameAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pre download game");
        }
    }




    [RelayCommand]
    private async Task RepairGameAsync()
    {
        try
        {
            var lang = await _downloadGameService.GetVoiceLanguageAsync(gameBiz, InstallPath);
            if (gameBiz.ToGame() is GameBiz.GenshinImpact)
            {
                var control = new DownloadGameDialog
                {
                    GameBiz = gameBiz,
                    LanguageType = lang,
                    ShowAllInfo = false,
                    RepairGame = true,
                };
                var dialog = new ContentDialog
                {
                    Title = Lang.LauncherPage_RepairGame,
                    Content = control,
                    PrimaryButtonText = Lang.Common_Confirm,
                    SecondaryButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is not ContentDialogResult.Primary)
                {
                    return;
                }
                lang = control.LanguageType;
            }
            string argu = $"""repair --biz {gameBiz} --loc "{InstallPath}" --lang {(int)lang} """;
            ProcessHelper.StartAsAdmin(null, argu);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start repair game");
        }
    }




    [RelayCommand]
    private async Task UninstallGameAsync()
    {
        try
        {
            if (_gameService.GetGameProcess(gameBiz) != null)
            {
                NotificationBehavior.Instance.Warning(Lang.LauncherPage_GameIsRunning);
                return;
            }
            UninstallStep enableSteps = UninstallStep.CleanRegistry | UninstallStep.DeleteTempFiles;
            if (Directory.Exists(InstallPath))
            {
                enableSteps |= UninstallStep.DeleteGameAssets;
            }
            if (LocalVersion != null)
            {
                enableSteps |= UninstallStep.BackupScreenshot;
            }
            var control = new UninstallGameDialog
            {
                Steps = enableSteps,
                EnableSteps = enableSteps,
            };
            var dialog = new ContentDialog
            {
                Title = Lang.LauncherPage_UninstallGame,
                Content = control,
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Secondary,
                XamlRoot = this.XamlRoot,
            };
            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                UninstallStep steps = control.Steps;
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!File.Exists(exe))
                {
                    exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                }
                string argu = $"""uninstall --biz {gameBiz} --loc "{InstallPath}" --steps {(int)steps}""";
                _logger.LogInformation("Start to uninstall game with argu: {argu}", argu);
                var p = Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Arguments = argu,
                    Verb = "runas",
                });
                if (p != null)
                {
                    await p.WaitForExitAsync();
                    if (p.ExitCode != 0)
                    {
                        _logger.LogError("Uninstallation error, exit code {code}", p.ExitCode);
                        NotificationBehavior.Instance.Warning(Lang.LauncherPage_UninstallationError, Lang.LauncherPage_PleaseCheckTheRelatedLogs);
                    }
                    else
                    {
                        _logger.LogInformation("Uninstall finished.");
                        NotificationBehavior.Instance.Success(Lang.LauncherPage_UninstallationCompleted);
                    }
                    UpdateGameVersion();
                    GetGameAccount();
                }
                else
                {
                    _logger.LogWarning("Uninstall process not started.");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Uninstall game");
        }
    }


    #endregion




}
