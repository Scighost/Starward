// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.AppLifecycle;
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
            await Task.Delay(16);
            InitializeGameBiz();
            UpdateGameVersion();
            InitializePlayTime();
            UpdateGameState();
            GetGameAccount();
            await GetLauncherContentAsync();
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

            if (gameBiz is GameBiz.hkrpg_cn or GameBiz.hkrpg_global)
            {
                NumberBox_FPS_1.IsEnabled = true;
                targetFPS = _gameService.GetStarRailFPS(gameBiz);
                OnPropertyChanged(nameof(TargetFPS));
            }

            if (gameBiz is GameBiz.hk4e_cloud)
            {
                Grid_BannerAndPost.HorizontalAlignment = HorizontalAlignment.Right;
            }

            if (gameBiz.ToGame() is GameBiz.Honkai3rd)
            {
                Border_Playtime.Visibility = Visibility.Collapsed;
                StackPanel_Account.Visibility = Visibility.Collapsed;
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
            LauncherPostGroupList = content.Post.GroupBy(x => x.Type).OrderBy(x => x.Key).Select(x => new LauncherPostGroup(PostTypeToString(x.Key), x)).ToList();
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


    private string PostTypeToString(PostType type)
    {
        return type switch
        {
            PostType.POST_TYPE_ACTIVITY => "活动",
            PostType.POST_TYPE_ANNOUNCE => "公告",
            PostType.POST_TYPE_INFO => "资讯",
            _ => "",
        };
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



    private void FlipView_Banner_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        _timer.Stop();
    }

    private void FlipView_Banner_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _timer.Start();
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
            Button_GameSetting.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;
        }
        else
        {
            Button_StartGame.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
            if (CanStartGame)
            {
                Button_GameSetting.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
            }
            else
            {
                Button_GameSetting.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;
            }
        }
    }


    [ObservableProperty]
    private string startGameButtonText = "开始游戏";


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


    public bool IsRepairGameEnable => (gameBiz is GameBiz.hk4e_cn or GameBiz.hk4e_global) && LocalVersion != null;


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
                StartGameButtonText = "游戏正在运行";
                newValue.EnableRaisingEvents = true;
                newValue.Exited += (_, _) => CheckGameExited();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 5)
            {
                // Access is denied
                processTimer?.Start();
            }
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
                }
                return;
            }
            LocalVersion = await _downloadGameService.GetLocalGameVersionAsync(gameBiz);
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
            StartGameButtonText = "开始游戏";
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
                        StartGameButtonText = "开始游戏";
                    });
                    GameProcess.Dispose();
                    GameProcess = null;
                }
            }
        }
        catch { }
    }



    [RelayCommand]
    private void StartGame()
    {
        try
        {
            if (IgnoreRunningGame)
            {
                var process = _gameService.StartGame(gameBiz, IgnoreRunningGame);
                if (process != null)
                {
                    MainPage.Current.PauseVideo();
                    User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWMINIMIZED);
                    _logger.LogInformation("Game started ({name}, {pid})", process.ProcessName, process.Id);
                    _playTimeService.StartProcessToLog(gameBiz, process);
                }
            }
            else
            {
                if (GameProcess?.HasExited ?? true)
                {
                    GameProcess = _gameService.StartGame(gameBiz, IgnoreRunningGame);
                    if (GameProcess != null)
                    {
                        MainPage.Current.PauseVideo();
                        User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWMINIMIZED);
                        _logger.LogInformation("Game started ({name}, {pid})", GameProcess.ProcessName, GameProcess.Id);
                        _playTimeService.StartProcessToLog(gameBiz, GameProcess);
                    }
                }
            }
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
        gameSettingOpenAction.Execute(this, null!);
        Flyout_GameSetting.Hide();
    }



    [RelayCommand]
    private void CloseGameSetting()
    {
        StackPanel_GameSetting.Visibility = Visibility.Visible;
        Flyout_GameSetting.ShowAt(Button_GameSetting);
        gameSettingCloseAction.Execute(this, null!);
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
                SelectGameAccount.Time = DateTime.Now;
                _gameService.SaveGameAccount(SelectGameAccount);
                FontIcon_SaveGameAccount.Glyph = "\uE8FB";
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
    private bool pauseVideoWhenChangeToOtherPage = AppConfig.PauseVideoWhenChangeToOtherPage;
    partial void OnPauseVideoWhenChangeToOtherPageChanged(bool value)
    {
        AppConfig.PauseVideoWhenChangeToOtherPage = value;
    }


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
        var instance = AppInstance.FindOrRegisterForKey($"download_game_{gameBiz}");
        if (!instance.IsCurrent)
        {
            await instance.RedirectActivationToAsync(instance.GetActivatedEventArgs());
            return true;
        }
        else
        {
            instance.UnregisterKey();
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
                        Title = "选择安装文件夹",
                        Content = $"以下文件夹中有尚未完成的下载任务\r\n{InstallPath}",
                        PrimaryButtonText = "继续",
                        SecondaryButtonText = "重新选择",
                        CloseButtonText = "取消",
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
                    Title = "选择安装文件夹",
                    Content = "请选择一个空文件夹用于安装游戏，或者选择已安装游戏的文件夹。",
                    PrimaryButtonText = "选择",
                    SecondaryButtonText = "取消",
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
                var versionDialog = new ContentDialog
                {
                    Title = "已是最新版本",
                    Content = "如果不是最新版本请修改游戏安装目录中的 config.ini 文件",
                    PrimaryButtonText = "确定",
                    XamlRoot = this.XamlRoot,
                };
                UpdateGameVersion();
                await versionDialog.ShowAsync();
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
                Title = IsUpdateGameEnable ? "更新游戏" : (IsPreDownloadEnable ? "预下载" : "安装游戏"),
                Content = content,
                PrimaryButtonText = "开始",
                SecondaryButtonText = "取消",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };

            if (await dialog.ShowAsync() is ContentDialogResult.Primary)
            {
                lang = content.LanguageType;
                var exe = Process.GetCurrentProcess().MainModule?.FileName;
                if (!File.Exists(exe))
                {
                    exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                }
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    UseShellExecute = true,
                    Arguments = $"""{(content.EnableRepairMode ? "repair" : "download")} --biz {gameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                    Verb = "runas",
                });
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
                    Title = "预下载",
                    Content = "预下载已完成，是否校验文件？",
                    PrimaryButtonText = "开始校验",
                    SecondaryButtonText = "取消",
                    DefaultButton = ContentDialogButton.Secondary,
                    XamlRoot = this.XamlRoot,
                };
                if (await dialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    var lang = await _downloadGameService.GetVoiceLanguageAsync(gameBiz, InstallPath);
                    var exe = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!File.Exists(exe))
                    {
                        exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exe,
                        UseShellExecute = true,
                        Arguments = $"""download --biz {gameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                        Verb = "runas",
                    });
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
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!File.Exists(exe))
            {
                exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Arguments = $"""repair --biz {gameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                Verb = "runas",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start repair game");
        }
    }



    #endregion




}
