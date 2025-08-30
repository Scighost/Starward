using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.Genshin.StygianOnslaught;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord.Genshin;

public sealed partial class StygianOnslaughtPage : PageBase
{


    private readonly ILogger<StygianOnslaughtPage> _logger = AppConfig.GetLogger<StygianOnslaughtPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public StygianOnslaughtPage()
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
        await Task.Delay(160);
        InitializeAbyssData();
    }



    protected override void OnUnloaded()
    {
        CurrentInfo = null;
        CurrentSelectedBattle = null;
        SOList = null!;
    }



    public bool HasData { get; set => SetProperty(ref field, value); }



    public List<StygianOnslaughtInfo> SOList { get; set => SetProperty(ref field, value); }


    public StygianOnslaughtInfo? CurrentInfo { get => field; set => SetProperty(ref field, value); }


    public StygianOnslaughtBattle? CurrentSelectedBattle { get => field; set => SetProperty(ref field, value); }


    private void InitializeAbyssData()
    {
        try
        {
            CurrentInfo = null;
            var list = _gameRecordService.GetStygianOnslaughtInfoList(gameRole);
            if (list.Count != 0)
            {
                SOList = list;
                ListView_StygianOnslaughtPageList.SelectedIndex = 0;
                Segmented_PlayerMode.SelectedIndex = 0;
                CurrentSelectedBattle = SOList[0].SinglePlayer;
                SegmentedItem_MultiPlayer.IsEnabled = SOList[0].MultiPlayer?.HasData ?? false;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
                CurrentInfo = null;
                CurrentSelectedBattle = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init abyss data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
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
            await _gameRecordService.RefreshStygianOnslaughtInfosAsync(gameRole);
            InitializeAbyssData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh stygian onslaught data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            GameRecordPage.HandleMiHoYoApiException(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh stygian onslaught data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh stygian onslaught data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Error(ex);
        }
    }




    private void ListView_StygianOnslaughtPageList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is StygianOnslaughtInfo info)
            {
                CurrentInfo = _gameRecordService.GetStygianOnslaughtInfo(gameRole, info.ScheduleId);
                CurrentSelectedBattle = CurrentInfo?.SinglePlayer;
                HasData = CurrentInfo?.Schedule?.ScheduleId > 0;
                Segmented_PlayerMode.SelectedIndex = 0;
                SegmentedItem_MultiPlayer.IsEnabled = CurrentInfo?.MultiPlayer?.HasData ?? false;
                Image_Emoji.Visibility = HasData ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



    private void Segmented_PlayerMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (Segmented_PlayerMode.SelectedIndex is 1)
            {
                CurrentSelectedBattle = CurrentInfo?.MultiPlayer;
            }
            else
            {
                CurrentSelectedBattle = CurrentInfo?.SinglePlayer;
            }
        }
        catch { }
    }



    public static Visibility RankToVisibility(int rank)
    {
        return rank > 0 ? Visibility.Visible : Visibility.Collapsed;
    }



    public static string BestTypeToString(int type)
    {
        return type switch
        {
            1 => Lang.SpiralAbyssPage_StrongestSingleStrike,
            2 => Lang.StygianOnslaughtPage_HighestTotalDamageDealt,
            _ => "",
        };
    }


    public static string DifficultyToImage(int difficulty)
    {
        return difficulty switch
        {
            >= 1 and <= 7 => $"ms-appx:///Assets/Image/UI_LeyLineChallenge_Medal_{difficulty}.png",
            _ => $"ms-appx:///Assets/Image/UI_LeyLineChallenge_Medal_0.png",
        };
    }


}
