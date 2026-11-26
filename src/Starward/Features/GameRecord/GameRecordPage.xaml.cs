using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Features.GameRecord.Genshin;
using Starward.Features.GameRecord.StarRail;
using Starward.Features.GameRecord.ZZZ;
using Starward.Features.Passport;
using Starward.Features.ViewHost;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord;

public sealed partial class GameRecordPage : PageBase
{

    private readonly ILogger<GameRecordPage> _logger = AppConfig.GetLogger<GameRecordPage>();

    private readonly GameRecordRoleService _gameRecordService = AppConfig.GetService<GameRecordRoleService>();


    public GameRecordPage()
    {
        this.InitializeComponent();
    }



    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        InitializeNavigationViewItemVisibility();
    }


    protected override async void OnLoaded()
    {
        if (AppConfig.HoyolabToolboxPaneOpen)
        {
            OpenNavigationViewPane();
        }
        else
        {
            CloseNavigationViewPane();
        }
        WeakReferenceMessenger.Default.Register<HypThirdPartyLoginMessage>(this, (r, m) =>
        {
            DispatcherQueue.TryEnqueue(() => _ = InitializeGameRoleAsync());
        });
        await Task.Delay(16);
        NavigateTo(typeof(BlankPage));
        await InitializeGameRoleAsync();
    }


    protected override void OnUnloaded()
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
        NavigationViewItem_BattleChronicle.Tapped -= NavigationViewItem_BattleChronicle_Tapped;
        NavigationViewItem_UpdateDeviceInfo.Tapped -= NavigationViewItem_UpdateDeviceInfo_Tapped;
        CurrentRole = null;
        GameRoleList = null!;
        _battleChronicleWindow = null;
    }



    #region Navigate


    public Thickness NavigationViewItemContentMargin { get; set => SetProperty(ref field, value); } = new Thickness(-2, 0, 0, 0);


    // Close pane
    private void Grid_Avatar_1_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        CloseNavigationViewPane();
    }


    // Open pane
    private void Border_Avatar_2_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        OpenNavigationViewPane();
    }


    private void OpenNavigationViewPane()
    {
        NavigationViewItemContentMargin = new Thickness(-2, 0, 0, 0);
        NavigationView_Toolbox.IsPaneOpen = true;
        Grid_Avatar_1.Visibility = Visibility.Visible;
        Border_Avatar_2.Visibility = Visibility.Collapsed;
        AppConfig.HoyolabToolboxPaneOpen = true;
    }


    private void CloseNavigationViewPane()
    {
        NavigationViewItemContentMargin = new Thickness(2, 0, 0, 0);
        NavigationView_Toolbox.IsPaneOpen = false;
        Grid_Avatar_1.Visibility = Visibility.Collapsed;
        Border_Avatar_2.Visibility = Visibility.Visible;
        AppConfig.HoyolabToolboxPaneOpen = false;
    }


    private void InitializeNavigationViewItemVisibility()
    {
        if (CurrentGameBiz.Game is GameBiz.bh3)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            // 崩坏3战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/4d94fbd5ff63c8b4344876ce21e04d10_2581928258151711511.png"));
        }
        else if (CurrentGameBiz.Game is GameBiz.hk4e)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            NavigationViewItem_TravelersDiary.Visibility = Visibility.Visible;
            NavigationViewItem_SpiralAbyss.Visibility = Visibility.Visible;
            NavigationViewItem_ImaginariumTheater.Visibility = Visibility.Visible;
            NavigationViewItem_StygianOnslaught.Visibility = Visibility.Visible;
            // 原神战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/ced4deac2162690105bbc8baad2b51a3_4109616186965788891.png"));
        }
        else if (CurrentGameBiz.Game is GameBiz.hkrpg)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            NavigationViewItem_TrailblazeMonthlyCalendar.Visibility = Visibility.Visible;
            NavigationViewItem_SimulatedUniverse.Visibility = Visibility.Visible;
            NavigationViewItem_ForgottenHall.Visibility = Visibility.Visible;
            NavigationViewItem_PureFiction.Visibility = Visibility.Visible;
            NavigationViewItem_ApocalypticShadow.Visibility = Visibility.Visible;
            NavigationViewItem_ChallengePeak.Visibility = Visibility.Visible;
            // 铁道战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/ade9545750299456a3fcbc8c3b63521d_2941971308029698042.png"));
        }
        else if (CurrentGameBiz.Game is GameBiz.nap)
        {
            NavigationViewItem_BattleChronicle.Visibility = Visibility.Visible;
            NavigationViewItem_InterKnotMonthlyReport.Visibility = Visibility.Visible;
            NavigationViewItem_ShiyuDefense.Visibility = Visibility.Visible;
            NavigationViewItem_DeadlyAssault.Visibility = Visibility.Visible;
            // 绝区零战绩图片
            Image_BattleChronicle.Source = new BitmapImage(new("ms-appx:///Assets/Image/bc8f0b7384b306c80f2a1fcca9f3d14b_8590605504999484795.png"));
        }
    }


    private void NavigateTo(Type? page, object? parameter = null, bool force_navigate = false)
    {
        if (page is null)
        {
            return;
        }
        if (!force_navigate && frame.SourcePageType == page)
        {
            return;
        }
        frame.Navigate(page, parameter ?? CurrentRole);
    }


    private void NavigationView_Toolbox_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            var item = args.InvokedItemContainer as NavigationViewItem;
            if (item != null)
            {
                if (args.InvokedItemContainer?.IsSelected ?? false)
                {
                    return;
                }
                var type = item.Tag switch
                {
                    nameof(TravelersDiaryPage) => typeof(TravelersDiaryPage),
                    nameof(SpiralAbyssPage) => typeof(SpiralAbyssPage),
                    nameof(ImaginariumTheaterPage) => typeof(ImaginariumTheaterPage),
                    nameof(StygianOnslaughtPage) => typeof(StygianOnslaughtPage),
                    nameof(TrailblazeCalendarPage) => typeof(TrailblazeCalendarPage),
                    nameof(SimulatedUniversePage) => typeof(SimulatedUniversePage),
                    nameof(ForgottenHallPage) => typeof(ForgottenHallPage),
                    nameof(PureFictionPage) => typeof(PureFictionPage),
                    nameof(ApocalypticShadowPage) => typeof(ApocalypticShadowPage),
                    nameof(ChallengePeakPage) => typeof(ChallengePeakPage),
                    nameof(InterKnotMonthlyReportPage) => typeof(InterKnotMonthlyReportPage),
                    nameof(ShiyuDefensePage) => typeof(ShiyuDefensePage),
                    nameof(DeadlyAssaultPage) => typeof(DeadlyAssaultPage),
                    _ => null,
                };
                NavigateTo(type);
            }
        }
        catch { }
    }


    private void NavigationViewItem_BattleChronicle_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        ShowBattleChronicleWindow();
    }


    private BattleChronicleWindow? _battleChronicleWindow;


    private void ShowBattleChronicleWindow()
    {
        // 窗口关闭后 AppWindow is null
        if (_battleChronicleWindow?.AppWindow is null)
        {
            _battleChronicleWindow = new BattleChronicleWindow
            {
                CurrentRole = CurrentRole,
            };
        }
        else if (_battleChronicleWindow.CurrentRole != CurrentRole)
        {
            _battleChronicleWindow.CurrentRole = CurrentRole;
        }
        _battleChronicleWindow.Activate();
    }


    #endregion




    #region Game Role Info



    public GameRecordRole? CurrentRole
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(AvatarUrl));
            }
        }
    }


    public List<GameRecordRole> GameRoleList { get; set => SetProperty(ref field, value); }


    public string AvatarUrl => !string.IsNullOrWhiteSpace(CurrentRole?.HeadIcon) ? CurrentRole.HeadIcon : $"ms-appx:///Assets/Image/icon_{(CurrentGameBiz.IsGlobalServer() ? "hoyolab" : "hyperion")}.png";


    private void LoadGameRoles(GameRecordRole? role = null)
    {
        try
        {
            if (role != null)
            {
                AppConfig.SetLastSelectGameRecordRole(CurrentGameBiz, role.Uid);
            }
            role ??= GameRecordRoleService.GetLastSelectGameRecordRoleOrTheFirstOne(CurrentGameBiz);
            var list = GameRecordRoleService.GetGameRoles(CurrentGameBiz);
            CurrentRole = role ?? list.FirstOrDefault();
            GameRoleList = list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load game roles ({gameBiz}).", CurrentGameBiz);
        }
    }


    /// <summary>
    /// 进入页面时初始化游戏角色信息
    /// </summary>
    /// <returns></returns>
    private async Task InitializeGameRoleAsync()
    {
        try
        {
            LoadGameRoles();
            if (CurrentRole is null)
            {
                await AddGameRolesByPassportAsync();
            }
            else
            {
                await UpdateSelectedGameRoleAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize game role ({gameBiz}).", CurrentGameBiz);
        }
    }


    /// <summary>
    /// 没有游戏角色时，通过当前通行证账号获取游戏角色
    /// </summary>
    /// <returns></returns>
    private async Task AddGameRolesByPassportAsync()
    {
        var passport = MainView.Current.CurrentPassportUser;
        if (passport is null)
        {
            return;
        }
        try
        {
            await _gameRecordService.UpdateGameRolesByPassportAsync(passport);
            LoadGameRoles();
            if (CurrentRole is not null)
            {
                await UpdateSelectedGameRoleAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game roles by passport ({aid}).", passport.Aid);
        }
    }


    /// <summary>
    /// 更新当前选中的游戏角色信息，失败时跳过
    /// </summary>
    /// <returns></returns>
    private async Task UpdateSelectedGameRoleAsync()
    {
        GameRecordRole? role = CurrentRole;
        if (role is null)
        {
            return;
        }
        try
        {
            await _gameRecordService.UpdateGameRoleAsync(role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update game role ({gameBiz}, {uid}).", role.GameBiz, role.Uid);
        }
        LoadGameRoles();
    }


    [RelayCommand]
    private async Task UpdateGameRolesCookieAsync()
    {
        try
        {
            if (GameRoleList is null)
            {
                return;
            }
            var aids = GameRoleList.Select(x => x.Aid).Distinct().ToList();
            await Parallel.ForEachAsync(aids, UnloadCancellationToken, async (aid, token) =>
            {
                try
                {
                    var passport = PassportService.GetPassportUserInfo(aid);
                    if (passport is null || passport.ErrorType is not PassportErrorType.Success)
                    {
                        return;
                    }
                    await _gameRecordService.UpdateGameRolesByPassportAsync(passport, token);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "UpdateGameRolesCookieAsync");
                }
            });
            LoadGameRoles();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "UpdateGameRolesCookieAsync");
        }
    }


    [RelayCommand]
    private async Task LoginAsync()
    {
        try
        {
            var model = await MainView.Current.HypLoginAsync();
            if (model is not null)
            {
                var role = GameRecordRoleService.GetGameRoles(CurrentGameBiz).FirstOrDefault(x => x.Aid == model.Aid);
                LoadGameRoles(role);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError($"Login failed: {ex}");
        }
    }


    private void ListView_GameRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is GameRecordRole role)
        {
            CurrentRole = role;
            AppConfig.SetLastSelectGameRecordRole(CurrentGameBiz, role.Uid);
        }
    }


    private void MenuFlyoutItem_DeleteGameRole_Click(object sender, RoutedEventArgs e)
    {
        GameRecordRole? gameRole = null;
        try
        {
            if (sender is FrameworkElement { Tag: GameRecordRole role })
            {
                gameRole = role;
                GameRecordRoleService.DeleteGameRole(role);
                LoadGameRoles();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game role ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }


    private async void NavigationViewItem_UpdateDeviceInfo_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        GameRecordServiceBase.ResetDeviceInfo();
        InAppToast.MainWindow?.Success(Lang.HoyolabToolboxPage_TheDeviceFingerprintIsAlreadyUpdated);
    }



    #endregion


}
