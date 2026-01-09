using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.StarRail.ChallengePeak;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord.StarRail;

public sealed partial class ChallengePeakPage : PageBase
{


    private readonly ILogger<ChallengePeakPage> _logger = AppConfig.GetLogger<ChallengePeakPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public ChallengePeakPage()
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
        InitializeChallengePeakData();
    }



    protected override void OnUnloaded()
    {
        CurrentChallengePeakData = null;
        ChallengePeakDataList = null!;
    }



    private List<ChallengePeakData> ChallengePeakDataList { get; set => SetProperty(ref field, value); }


    private ChallengePeakData? CurrentChallengePeakData { get; set => SetProperty(ref field, value); }

    private ChallengePeakRecord? CurrentChallengePeakRecord { get; set => SetProperty(ref field, value); }



    private void InitializeChallengePeakData()
    {
        try
        {
            CurrentChallengePeakData = null;
            var list = _gameRecordService.GetStarRailChallengePeakDataList(gameRole);
            if (list.Count != 0)
            {
                ChallengePeakDataList = list;
                ListView_ForgottenHall.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
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
            await _gameRecordService.RefreshStarRailChallengePeakDataAsync(gameRole);
            InitializeChallengePeakData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            GameRecordPage.HandleMiHoYoApiException(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh apocalyptic shadow data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Error(ex);
        }
    }



    private void ListView_ForgottenHall_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is ChallengePeakData data)
            {
                CurrentChallengePeakData = _gameRecordService.GetStarRailChallengePeakData(gameRole, data.GroupId);
                if (CurrentChallengePeakData is not null)
                {
                    Image_Emoji.Visibility = Visibility.Collapsed;
                    CurrentChallengePeakRecord = CurrentChallengePeakData.ChallengePeakRecords.FirstOrDefault();
                }
                else
                {
                    Image_Emoji.Visibility = Visibility.Visible;
                    CurrentChallengePeakRecord = null;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



}
