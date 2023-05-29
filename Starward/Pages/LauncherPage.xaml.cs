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
            await Task.Delay(16);
            InitializeGameBiz();
            InitializePlayTime();
            UpdateGameState();
            GetGameAccount();
            await GetLauncherContentAsync();
            _timer.Start();
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
        try
        {
            StartGameArgument = AppConfig.GetStartArgument(gameBiz);
            EnableThirdPartyTool = AppConfig.GetEnableThirdPartyTool(gameBiz);
            ThirdPartyToolPath = AppConfig.GetThirdPartyToolPath(gameBiz);
            if (gameBiz is GameBiz.hkrpg_cn or GameBiz.hkrpg_global)
            {
                NumberBox_FPS_1.IsEnabled = true;
                NumberBox_FPS_2.IsEnabled = true;
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
                targetFPS = _gameService.GetStarRailFPS(gameBiz);
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field 
                OnPropertyChanged(nameof(TargetFPS));
            }
            if (gameBiz is GameBiz.hk4e_cloud)
            {
                Grid_BannerAndPost.HorizontalAlignment = HorizontalAlignment.Right;
            }
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
        Grid_BannerAndPost.Opacity = value ? 1 : 0;
        Grid_BannerAndPost.IsHitTestVisible = value;
        AppConfig.EnableBannerAndPost = value;
    }



    private async Task GetLauncherContentAsync()
    {
        try
        {
            var content = await _launcherService.GetLauncherContentAsync(gameBiz);
            BannerList = content.Banner;
            LauncherPostGroupList = content.Post.GroupBy(x => x.Type).OrderBy(x => x.Key).Select(x => new LauncherPostGroup(PostTypeToString(x.Key), x)).ToList();
            if (EnableBannerAndPost)
            {
                Grid_BannerAndPost.Opacity = 1;
                Grid_BannerAndPost.IsHitTestVisible = true;
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
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    private bool canStartGame = true;
    partial void OnCanStartGameChanged(bool value)
    {
        if (value)
        {
            Button_GameSetting.Style = Application.Current.Resources["AccentButtonStyle"] as Style;
        }
        else
        {
            Button_GameSetting.Style = Application.Current.Resources["DefaultButtonStyle"] as Style;
        }
    }


    [ObservableProperty]
    private string startGameButtonText = "开始游戏";


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



    private void UpdateGameState()
    {
        try
        {
            CanStartGame = true;
            var install = _gameService.GetGameInstallPath(gameBiz);
            var exeName = _gameService.GetGameExeName(gameBiz);
            var exePath = Path.Join(install, exeName);
            if (!File.Exists(exePath))
            {
                _logger.LogWarning("Game uninstalled ({biz})", gameBiz);
                StartGameButtonText = "未安装游戏";
                CanStartGame = false;
                AppConfig.SetGameInstallPath(gameBiz, null);
                return;
            }
            InstallPath = install;
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



    [RelayCommand(CanExecute = nameof(CanStartGame))]
    private void StartGame()
    {
        try
        {
            if (IgnoreRunningGame)
            {
                var process = _gameService.StartGame(gameBiz, IgnoreRunningGame);
                if (process != null)
                {
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
                FontIcon_SaveGameAccount.Glyph = "\uE10B";
                await Task.Delay(3000);
                FontIcon_SaveGameAccount.Glyph = "\uE105";
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

   

}
