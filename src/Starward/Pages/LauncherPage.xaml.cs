// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.Win32;
using Starward.Controls;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Helpers;
using Starward.Messages;
using Starward.Models;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Timers;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class LauncherPage : PageBase
{

    private readonly ILogger<LauncherPage> _logger = AppConfig.GetLogger<LauncherPage>();

    private readonly GameService _gameService = AppConfig.GetService<GameService>();

    private readonly LauncherContentService _launcherContentService = AppConfig.GetService<LauncherContentService>();

    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();

    private readonly DatabaseService _databaseService = AppConfig.GetService<DatabaseService>();

    private readonly GameResourceService _gameResourceService = AppConfig.GetService<GameResourceService>();

    private readonly GameAccountService _gameAccountService = AppConfig.GetService<GameAccountService>();


    public LauncherPage()
    {
        this.InitializeComponent();

        TextBlockHelper.Inlines(
            TextBlock_StartArgumentDesc.Inlines,
            Lang.GameSettingPage_StartArgumentDesc,
            ("{Unity Standalone Player Command Line Arguments}", "https://docs.unity3d.com/Manual/PlayerCommandLineArguments.html"));
    }



    protected override async void OnLoaded()
    {
        try
        {
            RegisterMessageHandler();
            InitializeCurrentGameBiz();
            // banner
            InitializeBannerSize();
            InitializeBannerTimer();

            await Task.Delay(16);
            CheckGameVersion();
            UpdateGameState();
            InitializePlayTime();
            GetGameAccount();

            if (!AppConfig.LauncherPageFirstLoaded)
            {
                // 避免加载窗口和缓存图片同时进行可能导致的崩溃
                await Task.Delay(200);
                AppConfig.LauncherPageFirstLoaded = true;
            }
            await UpdateLauncherContentAsync();
            await UpdateGameNoticesAlertAsync();
        }
        catch { }
    }



    protected override void OnUnloaded()
    {
        _bannerTimer.Stop();
        GameProcess?.Dispose();
        processTimer?.Dispose();
        WeakReferenceMessenger.Default.UnregisterAll(this);
    }



    private void RegisterMessageHandler()
    {
        WeakReferenceMessenger.Default.Register<GameAccountSwitcherDisabledChanged>(this, (_, _) =>
        {
            GetGameAccount();
            _ = UpdateGameNoticesAlertAsync();
        });
        WeakReferenceMessenger.Default.Register<WindowStateChangedMessage>(this, (_, m) =>
        {
            if (m.IsHide)
            {
                _bannerTimer.Stop();
            }
            else
            {
                _bannerTimer.Start();
            }
        });
        WeakReferenceMessenger.Default.Register<GameNoticeRedHotDisabledChanged>(this, (_, _) => _ = UpdateGameNoticesAlertAsync());
        WeakReferenceMessenger.Default.Register<WindowSizeModeChangedMessage>(this, (_, _) => InitializeBannerSize());
        WeakReferenceMessenger.Default.Register<VideoPlayStateChangedMessage>(this, (_, _) => UpdateGameButtonStyle());
    }



    private void InitializeCurrentGameBiz()
    {
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
        try
        {
            StartGameArgument = AppConfig.GetStartArgument(CurrentGameBiz);
            EnableThirdPartyTool = AppConfig.GetEnableThirdPartyTool(CurrentGameBiz);
            ThirdPartyToolPath = AppConfig.GetThirdPartyToolPath(CurrentGameBiz);
            enableCustomBg = AppConfig.GetEnableCustomBg(CurrentGameBiz);
            OnPropertyChanged(nameof(EnableCustomBg));
            CustomBg = AppConfig.GetCustomBg(CurrentGameBiz);

            if (CurrentGameBiz is GameBiz.hk4e_cloud)
            {
                Button_UninstallGame.IsEnabled = false;
                Grid_BannerAndPost.HorizontalAlignment = HorizontalAlignment.Right;
            }
            if (CurrentGameBiz is GameBiz.nap_cn)
            {
                Button_RepairDropDown.IsEnabled = false;
                Button_UninstallGame.IsEnabled = false;
            }
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field 
        }
        catch { }
    }





    #region Banner & Post



    private Microsoft.UI.Dispatching.DispatcherQueueTimer _bannerTimer;


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
            _bannerTimer.Start();
        }
        else
        {
            _bannerTimer.Stop();
        }
    }


    private void InitializeBannerTimer()
    {
        if (_bannerTimer is null)
        {
            _bannerTimer = DispatcherQueue.CreateTimer();
            _bannerTimer.Interval = TimeSpan.FromSeconds(5);
            _bannerTimer.IsRepeating = true;
            _bannerTimer.Tick += _bannerTimer_Tick;
        }
    }


    private void InitializeBannerSize()
    {
        if (AppConfig.WindowSizeMode == 0)
        {
            Grid_BannerAndPost.Width = 432;
            RowDefinition_BannerAndPost.Height = new GridLength(200);
        }
        else
        {
            Grid_BannerAndPost.Width = 364;
            RowDefinition_BannerAndPost.Height = new GridLength(168);
        }
    }


    private async Task UpdateLauncherContentAsync()
    {
        try
        {
            var content = await _launcherContentService.GetLauncherContentAsync(CurrentGameBiz);
            BannerList = content.Banner;
            LauncherPostGroupList = content.Post.GroupBy(x => x.Type).OrderBy(x => x.Key).Select(x => new LauncherPostGroup(x.Key.ToLocalization(), x)).ToList();
            if (EnableBannerAndPost && BannerList.Count != 0 && LauncherPostGroupList.Count != 0)
            {
                Grid_BannerAndPost.Opacity = 1;
                Grid_BannerAndPost.IsHitTestVisible = true;
                _bannerTimer.Start();
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Cannot get game launcher content ({CurrentGameBiz}): {error}", CurrentGameBiz, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game launcher content ({CurrentGameBiz})", CurrentGameBiz);
        }
    }



    private void _bannerTimer_Tick(Microsoft.UI.Dispatching.DispatcherQueueTimer sender, object args)
    {
        try
        {
            if (EnableBannerAndPost && BannerList?.Count > 0)
            {
                FlipView_Banner.SelectedIndex = (FlipView_Banner.SelectedIndex + 1) % BannerList.Count;
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
                _logger.LogInformation("Open banner: {url}", banner.Image.Link);
                await Windows.System.Launcher.LaunchUriAsync(new Uri(banner.Image.Link));
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
        _bannerTimer.Stop();
        Border_PipsPager.Visibility = Visibility.Visible;
    }

    private void Grid_BannerContainer_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        _bannerTimer.Start();
        Border_PipsPager.Visibility = Visibility.Collapsed;
    }




    private async Task UpdateGameNoticesAlertAsync()
    {
        try
        {
            if (AppConfig.DisableGameNoticeRedHot || AppConfig.DisableGameAccountSwitcher || CurrentGameBiz.IsBilibiliServer())
            {
                Image_GameNoticesAlert.Visibility = Visibility.Collapsed;
                return;
            }
            long uid = 0;
            if (GameAccountList?.FirstOrDefault(x => x.IsLogin) is GameAccount account)
            {
                uid = account.Uid;
            }
            if (uid == 0)
            {
                Image_GameNoticesAlert.Visibility = Visibility.Collapsed;
                return;
            }
            if (await _launcherContentService.IsNoticesAlertAsync(CurrentGameBiz, uid))
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
        WeakReferenceMessenger.Default.Send(new MainPageNavigateMessage(typeof(GameNoticesPage)));
    }



    [RelayCommand]
    private async Task OpenGameNoticesInBrowser()
    {
        try
        {
            long uid = SelectGameAccount?.Uid ?? 0;
            string lang = CultureInfo.CurrentUICulture.Name;
            string url = LauncherClient.GetGameNoticesUrl(CurrentGameBiz, uid, lang);
            await Launcher.LaunchUriAsync(new Uri(url));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open game notices in browser");
        }
    }


    #endregion




    #region Start Game



    private void UpdateGameButtonStyle()
    {
        //var accentStyle = Application.Current.Resources["AccentButtonStyle"] as Style;
        //var defaultStyle = Application.Current.Resources["DefaultButtonStyle"] as Style;
        //if (AppConfig.IsPlayingVideo)
        //{
        //    Button_GameIsRunning.Style = defaultStyle;
        //    Button_StartGame.Style = defaultStyle;
        //    Button_DownloadGame.Style = defaultStyle;
        //    Button_UpdateGame.Style = defaultStyle;
        //    Button_RepairGame.Style = defaultStyle;
        //    Button_PreDownloadGame.Style = defaultStyle;
        //    AnimatedIcon_GameSetting.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
        //}
        //else
        //{
        if (!CanStartGame || IsGameRunning)
        {
            AnimatedIcon_GameSetting.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
        }
        else
        {
            AnimatedIcon_GameSetting.Foreground = Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"] as Brush;
        }
        //    Button_GameIsRunning.Style = accentStyle;
        //    Button_StartGame.Style = accentStyle;
        //    Button_DownloadGame.Style = accentStyle;
        //    Button_UpdateGame.Style = accentStyle;
        //    Button_RepairGame.Style = accentStyle;
        //    Button_PreDownloadGame.Style = accentStyle;
        //}
    }


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSwitchClientButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateGameButtonEnable))]
    private GameBiz configGameBiz;



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsDownloadGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsPreInstallButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsRepairGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsGameSupportCompleteRepair))]
    [NotifyPropertyChangedFor(nameof(IsSettingGameRepairButtonEnabled))]
    private Version? localGameVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsUpdateGameButtonEnable))]
    private Version? latestGameVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsPreInstallButtonEnable))]
    private Version? preInstallGameVersion;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsDownloadGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsRepairGameButtonEnable))]
    [NotifyPropertyChangedFor(nameof(IsSettingGameRepairButtonEnabled))]
    private bool isGameExeExists;


    public bool IsGameSupportCompleteRepair => CurrentGameBiz.ToGame() != GameBiz.None && CurrentGameBiz != GameBiz.hk4e_cloud && (CurrentGameBiz.ToGame() != GameBiz.Honkai3rd || (CurrentGameBiz.ToGame() == GameBiz.Honkai3rd && IsGameExeExists));


    public bool IsStartGameButtonEnable => LocalGameVersion != null && LocalGameVersion >= LatestGameVersion && IsGameExeExists && !IsGameRunning;


    public bool IsDownloadGameButtonEnable => (LocalGameVersion == null && !IsGameExeExists) || ((LocalGameVersion == null || !IsGameExeExists) && !IsGameSupportCompleteRepair);


    public bool IsUpdateGameButtonEnable => LocalGameVersion != null && LatestGameVersion > LocalGameVersion;


    public bool IsPreInstallButtonEnable => LocalGameVersion != null && PreInstallGameVersion != null && !IsSwitchClientButtonEnable;


    public bool IsRepairGameButtonEnable => IsGameSupportCompleteRepair && ((LocalGameVersion != null && !IsGameExeExists) || (LocalGameVersion == null && IsGameExeExists));


    public bool IsSwitchClientButtonEnable => ConfigGameBiz.ToGame() != GameBiz.None && ConfigGameBiz != CurrentGameBiz;


    [ObservableProperty]
    private bool isPreDownloadOK;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsStartGameButtonEnable))]
    private bool isGameRunning;
    partial void OnIsGameRunningChanged(bool value)
    {
        UpdateGameButtonStyle();
    }



    [ObservableProperty]
    private bool canStartGame = true;
    partial void OnCanStartGameChanged(bool value)
    {
        UpdateGameButtonStyle();
    }



    private Timer processTimer;


    [ObservableProperty]
    private Process? gameProcess;
    partial void OnGameProcessChanged(Process? oldValue, Process? newValue)
    {
        oldValue?.Dispose();
        processTimer?.Stop();
        if (processTimer is null)
        {
            processTimer = new(1000);
            processTimer.Elapsed += (_, _) => CheckGameExited();
        }
        if (newValue != null)
        {
            processTimer?.Start();
        }
        else
        {
            _logger.LogInformation("Game process exited");
            DispatcherQueue.TryEnqueue(GetGameAccount);
        }
    }




    private async void CheckGameVersion()
    {
        try
        {
            InstallPath = _gameResourceService.GetGameInstallPath(CurrentGameBiz);
            _logger.LogInformation("Game install path of {biz}: {path}", CurrentGameBiz, InstallPath);
            IsGameExeExists = _gameResourceService.IsGameExeExists(CurrentGameBiz);
            if (CurrentGameBiz == GameBiz.hk4e_cloud)
            {
                if (Directory.Exists(InstallPath))
                {
                    LocalGameVersion = new Version();
                    UpdateGameButtonStyle();
                }
                return;
            }
            (LocalGameVersion, ConfigGameBiz) = await _gameResourceService.GetLocalGameVersionAndBizAsync(CurrentGameBiz);
            _logger.LogInformation("Acutal version and gamebiz of {biz} is {version}, {configBiz}.", CurrentGameBiz, LocalGameVersion, ConfigGameBiz);
            UpdateGameButtonStyle();
            (LatestGameVersion, PreInstallGameVersion) = await _gameResourceService.GetGameResourceVersionAsync(CurrentGameBiz);
            if (IsPreInstallButtonEnable)
            {
                IsPreDownloadOK = await _gameResourceService.CheckPreDownloadIsOKAsync(CurrentGameBiz);
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
            IsGameRunning = false;
            if (IgnoreRunningGame)
            {
                GameProcess = null;
                return;
            }
            GameProcess = _gameService.GetGameProcess(CurrentGameBiz);
            if (GameProcess != null)
            {
                IsGameRunning = true;
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
                    DispatcherQueue.TryEnqueue(UpdateGameState);
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
            CanStartGame = false;
            if (!IgnoreRunningGame)
            {
                var p = _gameService.GetGameProcess(CurrentGameBiz);
                if (p != null)
                {
                    GameProcess = p;
                    return;
                }
            }
            var process1 = _gameService.StartGame(CurrentGameBiz, IgnoreRunningGame);
            if (process1 == null)
            {
                CanStartGame = true;
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new GameStartMessage(CurrentGameBiz));
                var action = AppConfig.AfterStartGameAction;
                if (action is AfterStartGameAction.Minimize)
                {
                    MainWindow.Current.Minimize();
                }
                else if (action is AfterStartGameAction.DoNothing)
                {
                    // do nothing
                }
                else
                {
                    MainWindow.Current.Hide();
                }
                _logger.LogInformation("Game started ({name}, {pid})", process1.ProcessName, process1.Id);
                if (AppConfig.IgnoreRunningGame)
                {
                    _ = _playTimeService.StartProcessToLogAsync(CurrentGameBiz);
                    CanStartGame = true;
                }
                else
                {
                    IsGameRunning = true;
                    var process2 = await _playTimeService.StartProcessToLogAsync(CurrentGameBiz);
                    GameProcess = process2 ?? process1;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start game");
            CanStartGame = true;
        }
    }




    #endregion




    #region Playtime



    [ObservableProperty]
    private TimeSpan playTimeTotal;


    [ObservableProperty]
    private TimeSpan playTimeMonth;


    [ObservableProperty]
    private TimeSpan playTimeWeek;


    [ObservableProperty]
    private TimeSpan playTimeDay;


    [ObservableProperty]
    private TimeSpan playTimeLast;


    [ObservableProperty]
    private string lastPlayTimeText;


    [ObservableProperty]
    private int startUpCount;


    private void InitializePlayTime()
    {
        try
        {
            PlayTimeTotal = _databaseService.GetValue<TimeSpan>($"playtime_total_{CurrentGameBiz}", out _);
            PlayTimeMonth = _databaseService.GetValue<TimeSpan>($"playtime_month_{CurrentGameBiz}", out _);
            PlayTimeWeek = _databaseService.GetValue<TimeSpan>($"playtime_week_{CurrentGameBiz}", out _);
            PlayTimeDay = _databaseService.GetValue<TimeSpan>($"playtime_day_{CurrentGameBiz}", out _);
            StartUpCount = _databaseService.GetValue<int>($"startup_count_{CurrentGameBiz}", out _);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(CurrentGameBiz);
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
            PlayTimeTotal = _playTimeService.GetPlayTimeTotal(CurrentGameBiz);
            PlayTimeMonth = _playTimeService.GetPlayCurrentMonth(CurrentGameBiz);
            PlayTimeWeek = _playTimeService.GetPlayCurrentWeek(CurrentGameBiz);
            PlayTimeDay = _playTimeService.GetPlayCurrentDay(CurrentGameBiz);
            StartUpCount = _playTimeService.GetStartUpCount(CurrentGameBiz);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(CurrentGameBiz);
            if (time > DateTimeOffset.MinValue)
            {
                LastPlayTimeText = time.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            _databaseService.SetValue($"playtime_total_{CurrentGameBiz}", PlayTimeTotal);
            _databaseService.SetValue($"playtime_month_{CurrentGameBiz}", PlayTimeMonth);
            _databaseService.SetValue($"playtime_week_{CurrentGameBiz}", PlayTimeWeek);
            _databaseService.SetValue($"playtime_day_{CurrentGameBiz}", PlayTimeDay);
            _databaseService.SetValue($"startup_count_{CurrentGameBiz}", StartUpCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update play time");
        }
    }



    #endregion




    #region Game Account



    private TextBox TextBox_AccountUid;


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


    private List<string> suggestionUids;


    private void GetGameAccount()
    {
        try
        {
            if (AppConfig.DisableGameAccountSwitcher || CurrentGameBiz.IsBilibiliServer() || CurrentGameBiz is GameBiz.nap_cn)
            {
                StackPanel_Account.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                StackPanel_Account.Visibility = Visibility.Visible;
            }
            GameAccountList = _gameAccountService.GetGameAccounts(CurrentGameBiz);
            SelectGameAccount = GameAccountList.FirstOrDefault(x => x.IsLogin);
            CanChangeGameAccount = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot get game account ({biz})", CurrentGameBiz);
        }
    }




    [RelayCommand(CanExecute = nameof(CanChangeGameAccount))]
    private void ChangeGameAccount()
    {
        try
        {
            if (SelectGameAccount is not null)
            {
                _gameAccountService.ChangeGameAccount(SelectGameAccount);
                foreach (var item in GameAccountList)
                {
                    item.IsLogin = false;
                }
                CanChangeGameAccount = false;
                SelectGameAccount.IsLogin = true;
                if (IsGameRunning)
                {
                    NotificationBehavior.Instance.Warning(Lang.LauncherPage_AccountSwitchingCannotTakeEffectWhileGameIsRunning);
                }
                _ = UpdateGameNoticesAlertAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cannot change game {biz} account to {name}", CurrentGameBiz, SelectGameAccount?.Name);
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
                        _gameAccountService.DeleteGameAccount(lacc);
                    }
                }
                SelectGameAccount.Time = DateTime.Now;
                _gameAccountService.SaveGameAccount(SelectGameAccount);
                FontIcon_SaveGameAccount.Glyph = "\uE8FB";
                _ = UpdateGameNoticesAlertAsync();
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
                _gameAccountService.DeleteGameAccount(SelectGameAccount);
                GetGameAccount();
                _ = UpdateGameNoticesAlertAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game account");
        }
    }



    private void AutoSuggestBox_Uid_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (TextBox_AccountUid is null)
            {
                var ele1 = VisualTreeHelper.GetChild(AutoSuggestBox_Uid, 0);
                var ele = VisualTreeHelper.GetChild(ele1, 0);
                if (ele is TextBox textBox)
                {
                    TextBox_AccountUid = textBox;
                    TextBox_AccountUid.InputScope = new InputScope { Names = { new InputScopeName { NameValue = InputScopeNameValue.Number } } };
                    TextBox_AccountUid.BeforeTextChanging += (s, e) =>
                    {
                        e.Cancel = !e.NewText.All(x => char.IsDigit(x));
                    };
                }
            }
        }
        catch { }
    }


    private void AutoSuggestBox_Uid_GotFocus(object sender, RoutedEventArgs e)
    {
        try
        {
            suggestionUids = _gameAccountService.GetSuggestionUids(CurrentGameBiz).Select(x => x.ToString()).ToList();
            UpdateSuggestionUids(AutoSuggestBox_Uid.Text);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get suggestion uids");
        }
    }


    private void AutoSuggestBox_Uid_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        try
        {
            if (args.Reason is AutoSuggestionBoxTextChangeReason.UserInput)
            {
                UpdateSuggestionUids(sender.Text);
            }
        }
        catch { }
    }


    private void UpdateSuggestionUids(string text)
    {
        try
        {
            if (suggestionUids != null && suggestionUids.Count > 0)
            {
                if (string.IsNullOrWhiteSpace(text))
                {
                    AutoSuggestBox_Uid.ItemsSource = suggestionUids;
                    AutoSuggestBox_Uid.IsSuggestionListOpen = true;
                }
                else
                {
                    var list = suggestionUids.Where(x => x != text && x.StartsWith(text)).ToList();
                    if (list.Count == 0)
                    {
                        AutoSuggestBox_Uid.IsSuggestionListOpen = false;
                    }
                    else
                    {
                        if (!(AutoSuggestBox_Uid.ItemsSource is List<string> source && source.SequenceEqual(list)))
                        {
                            AutoSuggestBox_Uid.ItemsSource = list;
                        }
                        AutoSuggestBox_Uid.IsSuggestionListOpen = true;
                    }
                }
            }
        }
        catch { }
    }



    #endregion




    #region Download Game




    private async Task<bool> CheckRedirectInstanceAsync()
    {
        var instance = App.FindInstanceForKey($"download_game_{CurrentGameBiz}");
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
            if (CurrentGameBiz is GameBiz.hk4e_cloud)
            {
                await Launcher.LaunchUriAsync(new Uri("https://mhyy.mihoyo.com/"));
                return;
            }

            if (CurrentGameBiz is GameBiz.nap_cn)
            {
                await LauncherZZZCBTLauncherAsync();
                return;
            }

            if (await CheckRedirectInstanceAsync())
            {
                return;
            }

            string? temp_install_path = null;

            if (Directory.Exists(InstallPath))
            {
                if (LocalGameVersion is null)
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
                    if (result is ContentDialogResult.Primary)
                    {
                        temp_install_path = InstallPath;
                    }
                    else if (result is ContentDialogResult.Secondary)
                    {
                        temp_install_path = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    temp_install_path = InstallPath;
                }
            }
            else
            {
                var folderDialog = new ContentDialog
                {
                    Title = Lang.LauncherPage_SelectInstallFolder,
                    // 请选择一个空文件夹用于安装游戏，或者定位已安装游戏的文件夹。
                    Content = string.Format(Lang.LauncherPage_SelectInstallFolderDesc, GameResourceService.GetGameExeName(CurrentGameBiz)),
                    PrimaryButtonText = Lang.Common_Select,
                    SecondaryButtonText = Lang.Common_Cancel,
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot,
                };
                if (await folderDialog.ShowAsync() is ContentDialogResult.Primary)
                {
                    temp_install_path = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
                }
            }

            if (!Directory.Exists(temp_install_path))
            {
                return;
            }

            if (Path.GetPathRoot(temp_install_path) == temp_install_path)
            {
                NotificationBehavior.Instance.Warning(Lang.LauncherPage_PleaseDoNotSelectTheRootDirectoryOfADrive);
                return;
            }

            InstallPath = temp_install_path;

            var downloadResource = await _gameResourceService.CheckDownloadGameResourceAsync(CurrentGameBiz, InstallPath);
            if (downloadResource is null)
            {
                CheckGameVersion();
                return;
            }
            var lang = await _gameResourceService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath);
            if (lang is VoiceLanguage.None)
            {
                lang = VoiceLanguage.All;
            }

            var content = new DownloadGameDialog
            {
                GameBiz = CurrentGameBiz,
                LanguageType = lang,
                GameResource = downloadResource,
                PreDownloadMode = IsPreInstallButtonEnable
            };
            var dialog = new ContentDialog
            {
                Title = IsUpdateGameButtonEnable ? Lang.LauncherPage_UpdateGame : (IsPreInstallButtonEnable ? Lang.LauncherPage_PreInstall : Lang.LauncherPage_InstallGame),
                Content = content,
                PrimaryButtonText = Lang.Common_Start,
                SecondaryButtonText = Lang.Common_Cancel,
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
                    Arguments = $"""{(content.EnableRepairMode ? "repair" : "download")} --biz {CurrentGameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                    Verb = "runas",
                });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start to download game");
        }
    }



    private async Task LauncherZZZCBTLauncherAsync()
    {
        string? launcherFolder = Registry.GetValue(GameRegistry.LauncherPath_nap_cbt3, GameRegistry.InstallPath, null) as string;
        string? launcher = Path.Join(launcherFolder, "launcher.exe");
        if (File.Exists(launcher))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = launcher,
                UseShellExecute = true,
                Verb = "runas",
            });
        }
        else
        {
            await Launcher.LaunchUriAsync(new Uri("https://zzz.mihoyo.com/"));
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
                    var lang = await _gameResourceService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath);
                    var exe = Process.GetCurrentProcess().MainModule?.FileName;
                    if (!File.Exists(exe))
                    {
                        exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exe,
                        UseShellExecute = true,
                        Arguments = $"""download --biz {CurrentGameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
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
            if (CurrentGameBiz is GameBiz.nap_cn)
            {
                await LauncherZZZCBTLauncherAsync();
                return;
            }
            var lang = await _gameResourceService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath);
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!File.Exists(exe))
            {
                exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
            }
            var control = new DownloadGameDialog
            {
                GameBiz = CurrentGameBiz,
                LanguageType = lang,
                RepairMode = true,
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
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Arguments = $"""repair --biz {CurrentGameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                Verb = "runas",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start repair game");
        }
    }



    [RelayCommand]
    private async Task ReinstallGameAsync()
    {
        try
        {
            var gameResource = await _gameResourceService.CheckDownloadGameResourceAsync(CurrentGameBiz, InstallPath!, true);
            var lang = await _gameResourceService.GetVoiceLanguageAsync(CurrentGameBiz, InstallPath);
            var exe = Process.GetCurrentProcess().MainModule?.FileName;
            if (!File.Exists(exe))
            {
                exe = Path.Combine(AppContext.BaseDirectory, "Starward.exe");
            }
            var control = new DownloadGameDialog
            {
                GameBiz = CurrentGameBiz,
                GameResource = gameResource!,
                LanguageType = lang,
                ReinstallMode = true,
            };
            var dialog = new ContentDialog
            {
                Title = Lang.LauncherPage_ReinstallGame,
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
            Process.Start(new ProcessStartInfo
            {
                FileName = exe,
                UseShellExecute = true,
                Arguments = $"""reinstall --biz {CurrentGameBiz} --loc "{InstallPath}" --lang {(int)lang} """,
                Verb = "runas",
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reinstall game");
        }
    }



    [RelayCommand]
    private void SwitchClient()
    {
        if (IsUpdateGameButtonEnable)
        {
            if (_gameResourceService.GetGameInstallPath(ConfigGameBiz) is null)
            {
                AppConfig.SetGameInstallPath(ConfigGameBiz, InstallPath);
            }
            WeakReferenceMessenger.Default.Send(new ChangeGameBizMessage(ConfigGameBiz));
        }
        else
        {
            MainWindow.Current.OverlayFrameNavigateTo(typeof(SwitchClientPage), CurrentGameBiz);
        }
    }



    [RelayCommand]
    private async Task UninstallGameAsync()
    {
        try
        {
            if (_gameService.GetGameProcess(CurrentGameBiz) != null)
            {
                NotificationBehavior.Instance.Warning(Lang.LauncherPage_GameIsRunning);
                return;
            }
            UninstallStep enableSteps = UninstallStep.CleanRegistry | UninstallStep.DeleteTempFiles;
            if (Directory.Exists(InstallPath))
            {
                enableSteps |= UninstallStep.DeleteGameAssets;
            }
            if (LocalGameVersion != null)
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
                string argu = $"""uninstall --biz {CurrentGameBiz} --loc "{InstallPath}" --steps {(int)steps}""";
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
                    CheckGameVersion();
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



    [RelayCommand]
    private void OpenGameResourcePage()
    {
        MainWindow.Current.OverlayFrameNavigateTo(typeof(GameResourcePage), CurrentGameBiz);
    }


    #endregion




    #region Game Setting



    public bool IsSettingGameRepairButtonEnabled => CurrentGameBiz.ToGame() != GameBiz.ZZZ && CurrentGameBiz.ToGame() != GameBiz.None && CurrentGameBiz != GameBiz.hk4e_cloud && LocalGameVersion != null;


    [ObservableProperty]
    private string? installPath;
    partial void OnInstallPathChanged(string? value)
    {
        AppConfig.SetGameInstallPath(CurrentGameBiz, value);
    }


    [ObservableProperty]
    private bool enableThirdPartyTool;
    partial void OnEnableThirdPartyToolChanged(bool value)
    {
        AppConfig.SetEnableThirdPartyTool(CurrentGameBiz, value);
    }


    [ObservableProperty]
    private string? thirdPartyToolPath;
    partial void OnThirdPartyToolPathChanged(string? value)
    {
        AppConfig.SetThirdPartyToolPath(CurrentGameBiz, value);
    }



    [ObservableProperty]
    private string? startGameArgument;
    partial void OnStartGameArgumentChanged(string? value)
    {
        AppConfig.SetStartArgument(CurrentGameBiz, value);
    }



    [ObservableProperty]
    private bool ignoreRunningGame = AppConfig.IgnoreRunningGame;
    partial void OnIgnoreRunningGameChanged(bool value)
    {
        AppConfig.IgnoreRunningGame = value;
        UpdateGameState();
    }


    [RelayCommand]
    private void OpenGameSetting()
    {
        SplitView_Content.IsPaneOpen = true;
    }


    [RelayCommand]
    private async Task ChangeGameInstallPathAsync()
    {
        try
        {
            var folder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.WindowHandle);
            if (!Directory.Exists(folder))
            {
                return;
            }
            _logger.LogInformation("Change game install path ({biz}): {path}", CurrentGameBiz, folder);
            InstallPath = folder;
            CheckGameVersion();
            UpdateGameState();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change game install path ({biz})", CurrentGameBiz);
        }
    }



    [RelayCommand]
    private async Task ChangeThirdPartyPathAsync()
    {
        try
        {
            var file = await FileDialogHelper.PickSingleFileAsync(MainWindow.Current.WindowHandle);
            if (File.Exists(file))
            {
                ThirdPartyToolPath = file;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change third party tool path ({biz})", CurrentGameBiz);
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
        CheckGameVersion();
    }


    [RelayCommand]
    private void DeleteThirdPartyToolPath()
    {
        ThirdPartyToolPath = null;
    }





    private void TextBlock_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        if (sender.FontSize == 14 && sender.IsTextTrimmed)
        {
            sender.FontSize = 12;
        }
    }



    #endregion




    #region Background Setting



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(VideoBgVolumeButtonIcon))]
    private int videoBgVolume = AppConfig.VideoBgVolume;
    partial void OnVideoBgVolumeChanged(int value)
    {
        if (MainWindow.Current?.mainPage is not null)
        {
            MainWindow.Current.mainPage.VideoBgVolume = value;
        }
    }


    [ObservableProperty]
    private bool useOneBg = AppConfig.UseOneBg;
    partial void OnUseOneBgChanged(bool value)
    {
        AppConfig.UseOneBg = value;
        AppConfig.SetCustomBg(CurrentGameBiz, CustomBg);
        AppConfig.SetEnableCustomBg(CurrentGameBiz, EnableCustomBg);

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
        AppConfig.SetEnableCustomBg(CurrentGameBiz, value);
        WeakReferenceMessenger.Default.Send(new UpdateBackgroundImageMessage(true));
        UpdateGameButtonStyle();
    }


    [ObservableProperty]
    private string? customBg;


    [RelayCommand]
    private async Task ChangeCustomBgAsync()
    {
        var file = await _launcherContentService.ChangeCustomBgAsync();
        if (file is not null)
        {
            CustomBg = file;
            AppConfig.SetCustomBg(CurrentGameBiz, file);
            WeakReferenceMessenger.Default.Send(new UpdateBackgroundImageMessage(true));
        }
    }


    [RelayCommand]
    private async Task OpenCustomBgAsync()
    {
        await _launcherContentService.OpenCustomBgAsync(CustomBg);
    }


    [RelayCommand]
    private void DeleteCustomBg()
    {
        AppConfig.SetCustomBg(CurrentGameBiz, null);
        CustomBg = null;
        WeakReferenceMessenger.Default.Send(new UpdateBackgroundImageMessage(true));
    }




    #endregion



}
