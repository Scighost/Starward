using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord.StarRail;

public sealed partial class ForgottenHallPage : PageBase
{


    private readonly ILogger<ForgottenHallPage> _logger = AppService.GetLogger<ForgottenHallPage>();

    private readonly GameRecordService _gameRecordService = AppService.GetService<GameRecordService>();



    public ForgottenHallPage()
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
        InitializeForgottenHallData();
    }



    protected override void OnUnloaded()
    {
        CurrentForgottenHall = null;
        ForgottenHallList = null!;
    }



    [ObservableProperty]
    private List<ForgottenHallInfo> forgottenHallList;


    [ObservableProperty]
    private ForgottenHallInfo? currentForgottenHall;



    private void InitializeForgottenHallData()
    {
        try
        {
            CurrentForgottenHall = null;
            var list = _gameRecordService.GetForgottenHallInfoList(gameRole);
            if (list.Count != 0)
            {
                ForgottenHallList = list;
                ListView_ForgottenHall.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init forgotten hall data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
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
            await _gameRecordService.RefreshForgottenHallInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshForgottenHallInfoAsync(gameRole, 2);
            InitializeForgottenHallData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh forgotten hall data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            GameRecordPage.HandleMiHoYoApiException(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh forgotten hall data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh forgotten hall data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Error(ex);
        }
    }



    private void ListView_ForgottenHall_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is ForgottenHallInfo info)
            {
                CurrentForgottenHall = _gameRecordService.GetForgottenHallInfo(gameRole, info.ScheduleId);
                Image_Emoji.Visibility = (CurrentForgottenHall?.HasData ?? false) ? Visibility.Collapsed : Visibility.Visible;
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
