using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Helpers;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class HoyolabToolboxPage : Page
{


    private readonly ILogger<HoyolabToolboxPage> _logger = AppConfig.GetLogger<HoyolabToolboxPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public HoyolabToolboxPage()
    {
        this.InitializeComponent();
    }




    private GameBiz gameBiz;




    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameBiz biz)
        {
            gameBiz = biz switch
            {
                GameBiz.hk4e_cloud => GameBiz.hk4e_cn,
                _ => biz
            };
            _gameRecordService.IsHoyolab = gameBiz.IsGlobalServer();
            _gameRecordService.Language = System.Globalization.CultureInfo.CurrentUICulture.Name;
            InitializeNavigationViewItemVisibility();
        }
    }




    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (AppConfig.HoyolabToolboxPaneOpen)
        {
            OpenNavigationViewPane();
        }
        else
        {
            CloseNavigationViewPane();
        }
        _gameRecordService.GameRecordRoleChanged += _gameRecordService_GameRecordRoleChanged;
        _gameRecordService.NavigateChanged += _gameRecordService_NavigateChanged;
        await Task.Delay(16);
        NavigateTo(typeof(BlankPage));
        await CheckAgreementAsync();
        LoadGameRoles();
    }



    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _gameRecordService.GameRecordRoleChanged -= _gameRecordService_GameRecordRoleChanged;
        _gameRecordService.NavigateChanged -= _gameRecordService_NavigateChanged;
    }




    private async Task CheckAgreementAsync()
    {
        try
        {
            if (!AppConfig.AcceptHoyolabToolboxAgreement)
            {
                var dialog = new ContentDialog
                {
                    Title = Lang.Common_Disclaimer,
                    Content = Lang.HoyolabToolboxPage_DisclaimerContent,
                    PrimaryButtonText = Lang.Common_Accept,
                    SecondaryButtonText = Lang.Common_Reject,
                    XamlRoot = this.XamlRoot,
                };
                var sw = Stopwatch.StartNew();
                var result = await dialog.ShowAsync();
                sw.Stop();
                if (result is ContentDialogResult.Secondary)
                {
                    MainPage.Current.NavigateTo(typeof(LauncherPage));
                    return;
                }
                if (sw.ElapsedMilliseconds < 5000)
                {
                    dialog.Title = Lang.HoyolabToolboxPage_WarningAgain;
                    result = await dialog.ShowAsync();
                    if (result is ContentDialogResult.Secondary)
                    {
                        MainPage.Current.NavigateTo(typeof(LauncherPage));
                        return;
                    }
                }
                AppConfig.AcceptHoyolabToolboxAgreement = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check agreement.");
        }
    }




    #region Navigation Style


    [ObservableProperty]
    private Thickness navigationViewItemContentMargin = new Thickness(-2, 0, 0, 0);


    // Close pane
    private void Grid_Avatar_1_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        CloseNavigationViewPane();
    }

    private void Grid_Avatar_1_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            fe.CenterPoint = new Vector3(fe.ActualSize, 0) / 2;
            fe.Scale = new Vector3(0.95f);
        }
    }

    private void Grid_Avatar_1_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            fe.Scale = Vector3.One;
        }
    }


    // Open pane
    private void Border_Avatar_2_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        OpenNavigationViewPane();
    }

    private void Border_Avatar_2_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            fe.CenterPoint = new Vector3(fe.ActualSize, 0) / 2;
            fe.Scale = new Vector3(0.95f);
        }
    }

    private void Border_Avatar_2_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe)
        {
            fe.Scale = Vector3.One;
        }
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
        if (gameBiz.ToGame() is GameBiz.GenshinImpact)
        {
            NavigationViewItem_SpiralAbyss.Visibility = Visibility.Visible;
            NavigationViewItem_TravelersDiary.Visibility = Visibility.Visible;
        }
        if (gameBiz.ToGame() is GameBiz.StarRail)
        {
            NavigationViewItem_SimulatedUniverse.Visibility = Visibility.Visible;
            NavigationViewItem_ForgottenHall.Visibility = Visibility.Visible;
            NavigationViewItem_TrailblazeMonthlyCalendar.Visibility = Visibility.Visible;
        }
    }




    #endregion




    #region Game Role Info



    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(AvatarUrl))]
    private GameRecordUser? currentUser;


    [ObservableProperty]
    private GameRecordRole? currentRole;


    [ObservableProperty]
    private List<GameRecordRole> gameRoleList;


    public string AvatarUrl => CurrentUser?.AvatarUrl ?? $"ms-appx:///Assets/Image/icon_{(gameBiz.IsGlobalServer() ? "hoyolab" : "hyperion")}.png";



    private void LoadGameRoles(GameRecordRole? role = null)
    {
        try
        {
            if (role != null)
            {
                _gameRecordService.SetLastSelectGameRecordRole(gameBiz, role);
            }
            role ??= _gameRecordService.GetLastSelectGameRecordRole(gameBiz);
            var list = _gameRecordService.GetGameRoles(gameBiz);
            CurrentRole = role ?? list.FirstOrDefault();
            GameRoleList = list;
            CurrentUser = _gameRecordService.GetGameRecordUser(CurrentRole);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load game roles ({gameBiz}).", gameBiz);
        }
    }



    private void _gameRecordService_GameRecordRoleChanged(object? sender, GameRecordRole? e)
    {
        LoadGameRoles(e);
    }



    [RelayCommand]
    private void WebLogin()
    {
        NavigateTo(typeof(LoginPage), gameBiz);
    }




    [RelayCommand]
    private async Task RefreshGameRoleInfoAsync()
    {
        try
        {
            if (CurrentRole is null)
            {
                await _gameRecordService.RefreshAllGameRolesInfoAsync();
            }
            else
            {
                await _gameRecordService.RefreshGameRoleInfoAsync(CurrentRole);
            }
            LoadGameRoles();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh game role info ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_AccountError, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh game role info ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh game role info ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
            NotificationBehavior.Instance.Error(ex);
        }
    }



    private void ListView_GameRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is GameRecordRole role)
        {
            CurrentRole = role;
            if (frame.SourcePageType?.Name is not nameof(LoginPage))
            {
                NavigateTo(frame.SourcePageType);
            }
        }
    }




    private void MenuFlyoutItem_CopyCookie_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { Tag: GameRecordRole role })
            {

                ClipboardHelper.SetText(role.Cookie);
            }
        }
        catch { }
    }



    private void MenuFlyoutItem_DeleteGameRole_Click(object sender, RoutedEventArgs e)
    {
        GameRecordRole? gameRole = null;
        try
        {
            if (sender is FrameworkElement { Tag: GameRecordRole role })
            {
                gameRole = role;
                _gameRecordService.DeleteGameRole(role);
                LoadGameRoles();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete game role ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



    [RelayCommand]
    private async Task InputCookieAsync()
    {
        try
        {
            var textbox = new TextBox
            {
                IsSpellCheckEnabled = false,
            };
            var dialog = new ContentDialog
            {
                Title = Lang.HoyolabToolboxPage_InputCookie,
                Content = textbox,
                PrimaryButtonText = Lang.Common_Confirm,
                SecondaryButtonText = Lang.Common_Cancel,
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.XamlRoot,
            };
            var result = await dialog.ShowAsync();
            if (result is ContentDialogResult.Primary)
            {
                var cookie = textbox.Text;
                if (string.IsNullOrWhiteSpace(cookie))
                {
                    _logger.LogInformation("Input cookie is null or white space.");
                    return;
                }
                var user = await _gameRecordService.AddRecordUserAsync(cookie);
                var roles = await _gameRecordService.AddGameRolesAsync(cookie);
                NotificationBehavior.Instance.Success(null, string.Format(Lang.LoginPage_AlreadyAddedGameRoles, roles.Count, string.Join("\r\n", roles.Select(x => $"{x.Nickname}  {x.Uid}"))), 5000);
                LoadGameRoles(roles.FirstOrDefault(x => x.GameBiz == gameBiz.ToString()));
            }
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Input cookie");
            NotificationBehavior.Instance.Warning(Lang.Common_AccountError, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Input cookie");
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Input cookie");
            NotificationBehavior.Instance.Error(ex);
        }
    }



    #endregion




    #region Navigate



    private void NavigateTo(Type? page, object? parameter = null)
    {
        if (page is null)
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
                var type = item.Tag switch
                {
                    nameof(SpiralAbyssPage) => typeof(SpiralAbyssPage),
                    nameof(TravelersDiaryPage) => typeof(TravelersDiaryPage),
                    nameof(SimulatedUniversePage) => typeof(SimulatedUniversePage),
                    nameof(ForgottenHallPage) => typeof(ForgottenHallPage),
                    nameof(TrailblazeCalendarPage) => typeof(TrailblazeCalendarPage),
                    _ => null,
                };
                NavigateTo(type);
            }
        }
        catch { }
    }


    private void _gameRecordService_NavigateChanged(object? sender, (Type Page, object? Parameter) e)
    {
        NavigateTo(e.Page, e.Parameter);
    }



    #endregion


}
