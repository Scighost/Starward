using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using NuGet.Versioning;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.Gacha;
using Starward.Features.GameLauncher;
using Starward.Features.GamepadControl;
using Starward.Features.GameRecord;
using Starward.Features.GameSelector;
using Starward.Features.Passport;
using Starward.Features.Screenshot;
using Starward.Features.SelfQuery;
using Starward.Features.Setting;
using Starward.Features.Update;
using Starward.GameInstall;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Windows.Foundation;


namespace Starward.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class MainView : UserControl
{

    public static MainView Current { get; private set; }


    private readonly ILogger<MainView> _logger = AppConfig.GetLogger<MainView>();

    private readonly PassportService _passportService = AppConfig.GetService<PassportService>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();

    private readonly WindowsDeviceFpService _windowsDeviceFpService = AppConfig.GetService<WindowsDeviceFpService>();


    public GameId? CurrentGameId { get; private set => SetProperty(ref field, value); }

    public LauncherConfigModel? CurrentLauncherConfig { get; private set => SetProperty(ref field, value); }

    private GameFeatureConfig CurrentGameFeatureConfig { get; set; }



    public MainView()
    {
        Current = this;
        this.InitializeComponent();
        InitializeMainView();
    }



    private void InitializeMainView()
    {
        this.Loaded += MainView_Loaded;
        WeakReferenceMessenger.Default.Register<MainViewNavigateMessage>(this, OnMainViewNavigateMessageReceived);
        WeakReferenceMessenger.Default.Register<BH3GlobalGameServerChangedMessage>(this, OnBH3GlobalGameServerChanged);
        WeakReferenceMessenger.Default.Register<MainWindowStateChangedMessage>(this, (_, _) => _ = CheckUpdateOrShowRecentUpdateContentAsync());
        WeakReferenceMessenger.Default.Register<EnabledLauncherConfigChangedMessage>(this, OnEnabledLauncherConfigChanged);
        GameId? gameId = GameSwitcher.CurrentGameIcon?.GameId;
        if (gameId?.GameBiz == GameBiz.bh3_global)
        {
            string? id = AppConfig.LastGameIdOfBH3Global;
            if (!string.IsNullOrWhiteSpace(id))
            {
                gameId = new GameId(id, GameBiz.bh3_global);
            }
        }
        CurrentGameId = gameId;
        CurrentLauncherConfig = LauncherConfigModel.FromId(GameSwitcher.CurrentGameIcon?.LauncherId);
        UpdateNavigationView();
        InitializePassportUser();
        if (CurrentGameId is null)
        {
            ShowLauncherConfigView();
        }
    }




    private async void MainView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckSystemProxy();
        HotkeyManager.InitializeHotkey(this.XamlRoot.GetWindowHandle());
        _ = CheckUpdateOrShowRecentUpdateContentAsync();
        LogUploadService.Start();
        if (AppConfig.EnableGamepadController)
        {
            await Task.Delay(1000);
            var queue = Content.DispatcherQueue;
            _ = Task.Run(() => GamepadController.Initialize(queue));
        }
    }


    private async void CheckSystemProxy()
    {
        try
        {
            await Task.Delay(1500);
            Uri? proxy = HttpClient.DefaultProxy.GetProxy(new Uri("https://starward.scighost.com"));
            if (proxy is not null)
            {
                InAppToast.MainWindow?.Information(Lang.MainView_CheckSystemProxy_SystemProxyIsEnabled, proxy.ToString(), 5000);
            }
        }
        catch { }
    }


    private void GameSwitcher_CurrentGameChanged(object? sender, PinnedGameIcon icon)
    {
        GameId gameId = icon.GameId;
        if (icon.GameId.GameBiz == GameBiz.bh3_global)
        {
            string? id = AppConfig.LastGameIdOfBH3Global;
            if (!string.IsNullOrWhiteSpace(id))
            {
                gameId = new GameId(id, GameBiz.bh3_global);
            }
        }
        CurrentGameId = gameId;
        var newConfig = LauncherConfigModel.FromId(icon.LauncherId);
        if (CurrentLauncherConfig != newConfig)
        {
            CurrentLauncherConfig = newConfig;
            InitializePassportUser();
        }
        UpdateNavigationView();
        HideLauncherConfigView();
        UpdateDragableArea();
    }


    private void OnBH3GlobalGameServerChanged(object _, BH3GlobalGameServerChangedMessage message)
    {
        if (CurrentGameId?.GameBiz == GameBiz.bh3_global && CurrentGameId.Id != message.GameId)
        {
            CurrentGameId = new(message.GameId, GameBiz.bh3_global);
            NavigateTo(typeof(GameLauncherPage), CurrentGameId, new SuppressNavigationTransitionInfo());
        }
    }



    #region UI


    [RelayCommand]
    private void ShowLauncherConfigView()
    {
        LauncherConfigView.Visibility = Visibility.Visible;
        LauncherConfigView.Opacity = 1;
        LauncherConfigView.IsHitTestVisible = true;
        LauncherConfigView.ShowGameInfosIfNoGames();
    }



    private void HideLauncherConfigView()
    {
        LauncherConfigView.GoBack();
        LauncherConfigView.GoBack();
    }


    /// <summary>
    /// 更新窗口拖拽区域
    /// </summary>
    private void GameSwitcher_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragableArea();
    }



    private void UpdateDragableArea()
    {
        try
        {
            double x = GameSwitcher.ActualWidth + 62;
            this.XamlRoot.SetWindowDragRectangles([new Rect(x, 0, 10000, 48)]);
        }
        catch { }
    }


    #endregion



    #region Navigation





    private void UpdateNavigationView()
    {
        CurrentGameFeatureConfig = GameFeatureConfig.FromGameId(CurrentGameId);

        NavigationViewItem_Launcher.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GameLauncherPage)).ToVisibility();
        NavigationViewItem_Screenshot.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(ScreenshotPage)).ToVisibility();
        NavigationViewItem_GachaLog.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GachaLogPage)).ToVisibility();
        NavigationViewItem_HoyolabToolbox.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GameRecordPage)).ToVisibility();
        NavigationViewItem_SelfQuery.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(SelfQueryPage)).ToVisibility();
        NavigationViewItem_GenshinBeyondGacha.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GenshinBeyondGachaPage)).ToVisibility();

        if (CurrentGameId?.GameBiz.IsChinaServer() ?? false)
        {
            ToolTipService.SetToolTip(NavigationViewItem_HoyolabToolbox, Lang.HyperionToolbox);
            TextBlock_HoyolabToolbox.Text = Lang.HyperionToolbox;
        }
        if (CurrentGameId?.GameBiz.IsGlobalServer() ?? false)
        {
            ToolTipService.SetToolTip(NavigationViewItem_HoyolabToolbox, Lang.HoYoLABToolbox);
            TextBlock_HoyolabToolbox.Text = Lang.HoYoLABToolbox;
        }

        if (CurrentGameId is null)
        {
            NavigateTo(typeof(BlankPage));
        }
        else if (MainView_Frame.SourcePageType?.Name is not nameof(SettingPage))
        {
            NavigateTo(MainView_Frame.SourcePageType);
        }
    }



    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.IsSelected ?? false)
            {
                return;
            }
            if (args.IsSettingsInvoked)
            {
                NavigateTo(typeof(SettingPage));
            }
            else
            {
                if (args.InvokedItemContainer is NavigationViewItem item)
                {
                    var type = item.Tag switch
                    {
                        nameof(GameLauncherPage) => typeof(GameLauncherPage),
                        nameof(ScreenshotPage) => typeof(ScreenshotPage),
                        nameof(GachaLogPage) => typeof(GachaLogPage),
                        nameof(GameRecordPage) => typeof(GameRecordPage),
                        nameof(SelfQueryPage) => typeof(SelfQueryPage),
                        nameof(GenshinBeyondGachaPage) => typeof(GenshinBeyondGachaPage),
                        _ => null,
                    };
                    NavigateTo(type);
                }
            }
        }
        catch { }
    }



    private void NavigateTo(Type? page, object? param = null, NavigationTransitionInfo? infoOverride = null)
    {
        page ??= typeof(GameLauncherPage);
        if (page.Name is nameof(BlankPage) && CurrentGameId is null)
        {

        }
        else if (page.Name is not nameof(SettingPage) && !CurrentGameFeatureConfig.SupportedPages.Contains(page.Name))
        {
            page = typeof(GameLauncherPage);
        }
        if (page.Name is nameof(GameLauncherPage))
        {
            MainView_NavigationView.SelectedItem = NavigationViewItem_Launcher;
        }
        MainView_Frame.Navigate(page, param ?? CurrentGameId, infoOverride);
        if (page.Name is nameof(BlankPage) or nameof(GameLauncherPage))
        {
            Border_OverlayMask.Opacity = 0;
        }
        else
        {
            Border_OverlayMask.Opacity = 1;
        }
    }



    private void OnMainViewNavigateMessageReceived(object _, MainViewNavigateMessage message)
    {
        if (message.GameId is not null)
        {
            CurrentGameId = message.GameId;
            UpdateNavigationView();
            var launcher = _hoYoPlayService.GetLauncherConfig(message.GameId);
            if (launcher is not null)
            {
                var newConfig = LauncherConfigModel.FromId(launcher.Id);
                if (CurrentLauncherConfig != newConfig)
                {
                    CurrentLauncherConfig = newConfig;
                    InitializePassportUser();
                }
            }
            else
            {
                CurrentLauncherConfig = null;
            }
        }
        NavigateTo(message.Page, message.GameId);
        HideLauncherConfigView();
    }



    private async void OnEnabledLauncherConfigChanged(object _, EnabledLauncherConfigChangedMessage message)
    {
        await Task.Delay(300);
        CurrentGameId = GameSwitcher.CurrentGameIcon?.GameId;
        if (GameSwitcher.CurrentGameIcon is not null)
        {
            var newConfig = LauncherConfigModel.FromId(GameSwitcher.CurrentGameIcon.LauncherId);
            if (CurrentLauncherConfig != newConfig)
            {
                CurrentLauncherConfig = newConfig;
                InitializePassportUser();
            }
        }
        else
        {
            CurrentLauncherConfig = null;
        }
        UpdateNavigationView();
    }




    #endregion



    #region Update


    private DateTimeOffset _lastCheckUpdateTime;

    private DateTimeOffset _lastShowUpdateTime;

    private SemaphoreSlim _updateLock = new(1, 1);


    private async Task CheckUpdateOrShowRecentUpdateContentAsync()
    {
#if DEBUG || DONOT_CHECK_UPDATE
        return;
#endif
#pragma warning disable CS0162 // 检测到无法访问的代码
        if (!await _updateLock.WaitAsync(0))
        {
            return;
        }
        await Task.Delay(1000);
#pragma warning restore CS0162 // 检测到无法访问的代码
        try
        {
            if (_lastCheckUpdateTime == default && NuGetVersion.TryParse(AppConfig.AppVersion, out var appVersion))
            {
                _ = NuGetVersion.TryParse(AppConfig.LastAppVersion, out var lastVersion);
                if (appVersion != lastVersion)
                {
                    if (AppConfig.ShowUpdateContentAfterUpdateRestart)
                    {
                        new UpdateWindow().Activate();
                    }
                    else
                    {
                        AppConfig.LastAppVersion = AppConfig.AppVersion;
                    }
                    _lastCheckUpdateTime = DateTimeOffset.Now - TimeSpan.FromMinutes(55);
                    return;
                }
            }
            DateTimeOffset now = DateTimeOffset.Now;
            if (now - _lastCheckUpdateTime > TimeSpan.FromHours(1))
            {
                var release = await AppConfig.GetService<UpdateService>().CheckUpdateAsync(false);
                _lastCheckUpdateTime = now;
                if (release != null && now - _lastShowUpdateTime > TimeSpan.FromHours(6) && now.Date != _lastShowUpdateTime.Date)
                {
                    new UpdateWindow { NewVersion = release }.Activate();
                    _lastShowUpdateTime = now;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check update");
        }
        finally
        {
            _updateLock.Release();
        }
    }


    #endregion



    #region Passport


    [ObservableProperty]
    public partial PassportUserInfoModel? CurrentPassportUser { get; set; }

    public ObservableCollection<PassportUserInfoModel>? PassportUsers { get; set => SetProperty(ref field, value); }

    public bool AuthLoginGameByPassportAccount
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.AuthLoginGameByPassportAccount = value;
            }
        }
    } = AppConfig.AuthLoginGameByPassportAccount;


    private void InitializePassportUser()
    {
        try
        {
            CurrentPassportUser = null;
            PassportUsers = null;
            UpdateHypFlyoutLayout();
            if (CurrentLauncherConfig?.CanLogin ?? false)
            {
                _ = _windowsDeviceFpService.InitializeDeviceFpAsync(CurrentLauncherConfig.Region);
                PassportUsers = new(PassportService.GetPassportUserInfos(CurrentLauncherConfig.Region));
                if (PassportUsers.Count > 0)
                {
                    CurrentPassportUser = PassportUsers.FirstOrDefault(x => x.Aid == AppConfig.GetLastPassportUserAid(CurrentLauncherConfig.Region));
                    CurrentPassportUser ??= PassportUsers.FirstOrDefault();
                    UpdateHypFlyoutLayout();
                    _ = VerifyPassportSessionAsync(PassportUsers);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize passport user");
        }
    }


    private async Task VerifyPassportSessionAsync(IEnumerable<PassportUserInfoModel> list)
    {
        try
        {
            await Task.WhenAll(list.Select(x => _passportService.VerifySessionAsync(x)));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verfiy passport session");
        }
    }


    partial void OnCurrentPassportUserChanged(PassportUserInfoModel? value)
    {
        _hoYoPlayService.PassportUserInfo = value;
        if (value is not null)
        {
            AppConfig.SetLastPassportUserAid(value.Region, value.Aid);
        }
    }


    [RelayCommand]
    public async Task<PassportUserInfoModel?> HypLoginAsync()
    {
        try
        {
            if (CurrentLauncherConfig?.CanLogin ?? false)
            {
                var model = await new HypLoginWindow
                {
                    ParentWindowHandle = MainWindow.Current.WindowHandle,
                    ServerRegion = CurrentLauncherConfig.Region
                }.LoginAsync();
                if (model is not null)
                {
                    PassportUsers ??= new();
                    if (PassportUsers.FirstOrDefault(x => x.Aid == model.Aid) is PassportUserInfoModel oldModel)
                    {
                        int index = PassportUsers.IndexOf(oldModel);
                        PassportUsers.Remove(oldModel);
                        PassportUsers.Insert(index, model);
                    }
                    else
                    {
                        PassportUsers.Add(model);
                    }
                    CurrentPassportUser = model;
                    UpdateHypFlyoutLayout();
                    PassportService.SavePassportUserInfo(model);
                    MainWindow.Current.Show();
                    await AppConfig.GetService<GameRecordRoleService>().UpdateGameRolesByPassportAsync(model);
                    return model;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hyp login");
            InAppToast.MainWindow?.Error(ex, Lang.MainView_LoginFailed);
        }
        return null;
    }


    private void Button_HypLogout_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { DataContext: PassportUserInfoModel model })
            {
                PassportService.DeletePassportUserInfo(model);
                _ = _passportService.LogoutAsync(model);
                if (AppConfig.GetLastPassportUserAid(model.Region) == model.Aid)
                {
                    AppConfig.SetLastPassportUserAid(model.Region, 0);
                }
                if (CurrentPassportUser == model)
                {
                    PassportUsers?.Remove(model);
                    CurrentPassportUser = PassportUsers?.FirstOrDefault();
                }
                else
                {
                    PassportUsers?.Remove(model);
                }
                UpdateHypFlyoutLayout();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hyp logout");
        }
    }


    [RelayCommand]
    private void HypLogout()
    {
        try
        {
            var model = CurrentPassportUser;
            if (model is null)
            {
                return;
            }
            PassportService.DeletePassportUserInfo(model);
            _ = _passportService.LogoutAsync(model);
            if (AppConfig.GetLastPassportUserAid(model.Region) == model.Aid)
            {
                AppConfig.SetLastPassportUserAid(model.Region, 0);
            }
            PassportUsers?.Remove(model);
            CurrentPassportUser = PassportUsers?.FirstOrDefault();
            UpdateHypFlyoutLayout();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Hyp logout");
        }
    }


    private void UpdateHypFlyoutLayout()
    {
        if (PassportUsers?.Count > 1)
        {
            ColumnDefinition_HypLoginColumn0.MaxWidth = 200;
            ListView_HypAccunt.Visibility = Visibility.Visible;
            Button_HypLogout.Visibility = Visibility.Collapsed;
        }
        else if (PassportUsers?.Count > 0)
        {
            ColumnDefinition_HypLoginColumn0.MaxWidth = double.PositiveInfinity;
            ListView_HypAccunt.Visibility = Visibility.Collapsed;
            Button_HypLogout.Visibility = Visibility.Visible;
        }
        else
        {
            ColumnDefinition_HypLoginColumn0.MaxWidth = double.PositiveInfinity;
            ListView_HypAccunt.Visibility = Visibility.Collapsed;
            Button_HypLogout.Visibility = Visibility.Collapsed;
        }
    }


    #endregion


}



file static class BoolToVisibilityExtension
{

    public static Visibility ToVisibility(this bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

}
