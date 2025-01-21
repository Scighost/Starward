using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.Genshin.ImaginariumTheater;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord.Genshin;

public sealed partial class ImaginariumTheaterPage : PageBase
{

    private readonly ILogger<ImaginariumTheaterPage> _logger = AppService.GetLogger<ImaginariumTheaterPage>();

    private readonly GameRecordService _gameRecordService = AppService.GetService<GameRecordService>();


    public ImaginariumTheaterPage()
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
        InitializeTheaterData();
    }



    protected override void OnUnloaded()
    {
        CurrentTheater = null;
        TheaterList = null!;
    }




    [ObservableProperty]
    private bool hasData;



    [ObservableProperty]
    private List<ImaginariumTheaterInfo> theaterList;


    [ObservableProperty]
    private ImaginariumTheaterInfo? currentTheater;



    private void InitializeTheaterData()
    {
        try
        {
            CurrentTheater = null;
            var list = _gameRecordService.GetImaginariumTheaterInfoList(gameRole);
            if (list.Count != 0)
            {
                TheaterList = list;
                ListView_TheaterList.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init theater data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
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
            await _gameRecordService.RefreshImaginariumTheaterInfoAsync(gameRole);
            InitializeTheaterData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh theater data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            GameRecordPage.HandleMiHoYoApiException(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh theater data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh theater data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Error(ex);
        }
    }




    private void ListView_TheaterList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is ImaginariumTheaterInfo info)
            {
                CurrentTheater = _gameRecordService.GetImaginariumTheaterInfo(gameRole, info.ScheduleId);
                HasData = CurrentTheater?.HasData ?? false;
                Image_Emoji.Visibility = HasData ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }




    public static string DifficultyMode(int mode)
    {
        return mode switch
        {
            1 => Lang.ImaginariumTheaterPage_EasyMode,
            2 => Lang.ImaginariumTheaterPage_NormalMode,
            3 => Lang.ImaginariumTheaterPage_HardMode,
            4 => Lang.ImaginariumTheaterPage_VisionaryMode,
            _ => "",
        };
    }



    public static string ActX(int x)
    {
        return string.Format(Lang.ImaginariumTheaterPage_Act0, x);
    }



    public static Visibility StarIconVisibility(int value)
    {
        return value == 0 ? Visibility.Collapsed : Visibility.Visible;
    }



    public static string PerformancesTime(int second)
    {
        var ts = TimeSpan.FromSeconds(second);
        return $"{ts.Minutes}m {ts.Seconds}s";
    }



    public static Visibility FightStatisicVisibility(int value)
    {
        return value == 0 ? Visibility.Collapsed : Visibility.Visible;
    }


    private static BitmapImage Medal0;
    private static BitmapImage Medal1;
    private static BitmapImage Medal2;
    private static BitmapImage Medal3;
    private static BitmapImage Medal4;

    public static BitmapImage? HeraldryImage(int heraldry)
    {
        return heraldry switch
        {
            0 => Medal0 ?? new BitmapImage(new("ms-appx:///Assets/Image/UI_RoleCombat_Medal_0.png")),
            1 => Medal1 ?? new BitmapImage(new("ms-appx:///Assets/Image/UI_RoleCombat_Medal_1.png")),
            2 => Medal2 ?? new BitmapImage(new("ms-appx:///Assets/Image/UI_RoleCombat_Medal_2.png")),
            3 => Medal3 ?? new BitmapImage(new("ms-appx:///Assets/Image/UI_RoleCombat_Medal_3.png")),
            4 => Medal4 ?? new BitmapImage(new("ms-appx:///Assets/Image/UI_RoleCombat_Medal_4.png")),
            _ => null,
        };
    }




    public static string DifficultyIdToMaxRound(int difficulty)
    {
        return difficulty switch
        {
            1 => "3",
            2 => "6",
            3 => "8",
            4 => "10",
            _ => "-",
        };
    }



    public static Visibility IsBrilliantBlessingVisibility(int? level)
    {
        return level switch
        {
            null or 0 => Visibility.Collapsed,
            _ => Visibility.Visible,
        };
    }



    public static double BrilliantBlessingBuffOpacity(int level)
    {
        return level switch
        {
            0 => 0.3,
            _ => 1,
        };
    }


}



public class TheaterStarIconVisibilityConverter : IValueConverter
{

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return value is 0 ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}



