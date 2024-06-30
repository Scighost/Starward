using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Services;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class PlaytimeButton : UserControl
{


    public GameBiz CurrentGameBiz { get; set; }


    private readonly ILogger<PlaytimeButton> _logger = AppConfig.GetLogger<PlaytimeButton>();


    private readonly DatabaseService _databaseService = AppConfig.GetService<DatabaseService>();


    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();



    public PlaytimeButton()
    {
        this.InitializeComponent();
    }


    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitializePlayTime();
    }



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


}
