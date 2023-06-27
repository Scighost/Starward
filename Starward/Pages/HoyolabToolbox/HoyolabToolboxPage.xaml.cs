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
using System.Linq;
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
        }
    }




    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        _gameRecordService.GameRecordRoleChanged += _gameRecordService_GameRecordRoleChanged;
        await Task.Delay(16);
        LoadGameRoles();
    }



    private void Page_Unloaded(object sender, RoutedEventArgs e)
    {
        _gameRecordService.GameRecordRoleChanged -= _gameRecordService_GameRecordRoleChanged;
    }






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



    private void NavigateTo(Type page)
    {
        frame.Navigate(page, gameBiz);
    }




    [RelayCommand]
    private void WebLogin()
    {
        NavigateTo(typeof(LoginPage));
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh game role info ({gameBiz}, {uid}).", CurrentRole?.GameBiz, CurrentRole?.Uid);
        }
    }



    private void ListView_GameRoles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is GameRecordRole role)
        {
            CurrentRole = role;
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


}
