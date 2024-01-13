using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.StarRail.PureFiction;
using Starward.Helpers;
using Starward.Messages;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class PureFictionPage : PageBase
{


    private readonly ILogger<PureFictionPage> _logger = AppConfig.GetLogger<PureFictionPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public PureFictionPage()
    {
        this.InitializeComponent();
    }



    private GameRecordRole gameRole;


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is GameRecordRole role)
        {
            gameRole = role;
        }
    }



    protected override async void OnLoaded()
    {
        await Task.Delay(16);
        InitializePureFictionInfoData();
    }




    [ObservableProperty]
    private List<PureFictionInfo> pureFictionList;


    [ObservableProperty]
    private PureFictionInfo? currentPureFiction;



    private void InitializePureFictionInfoData()
    {
        try
        {
            CurrentPureFiction = null;
            var list = _gameRecordService.GetPureFictionInfoList(gameRole);
            if (list.Count != 0)
            {
                PureFictionList = list;
                ListView_ForgottenHall.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init pure fiction data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }




    [RelayCommand]
    private async Task RefreshDataAsync()
    {
        try
        {
            if (gameRole is null)
            {
                return;
            }
            await _gameRecordService.RefreshPureFictionInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshPureFictionInfoAsync(gameRole, 2);
            InitializePureFictionInfoData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh pure fiction data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            if (ex.ReturnCode is 1034 or 5003)
            {
                NotificationBehavior.Instance.ShowWithButton(InfoBarSeverity.Warning, Lang.Common_AccountError, ex.Message, Lang.HoyolabToolboxPage_VerifyAccount, () =>
                {
                    WeakReferenceMessenger.Default.Send(new VerifyAccountMessage(gameRole!, "https://webstatic.mihoyo.com/app/community-game-records/rpg/index.html?bbs_presentation_style=fullscreen#/rpg/oblivious?role_id={role_id}&server={server}&isPrev=&type=story"));
                });
            }
            else
            {
                NotificationBehavior.Instance.Warning(Lang.Common_AccountError, ex.Message);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh pure fiction data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh pure fiction data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Error(ex);
        }
    }



    private void ListView_ForgottenHall_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is PureFictionInfo info)
            {
                CurrentPureFiction = _gameRecordService.GetPureFictionInfo(gameRole, info.ScheduleId);
                Image_Emoji.Visibility = (CurrentPureFiction?.HasData ?? false) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }

    private void TextBlock_Deepest_IsTextTrimmedChanged(TextBlock sender, IsTextTrimmedChangedEventArgs args)
    {
        TextBlock_Deepest.SetValue(Grid.ColumnSpanProperty, 2);
        TextBlock_Battles.SetValue(Grid.RowProperty, 1);
        TextBlock_Battles.SetValue(Grid.ColumnProperty, 1);
        TextBlock_Battles.SetValue(Grid.ColumnSpanProperty, 2);
    }

}
