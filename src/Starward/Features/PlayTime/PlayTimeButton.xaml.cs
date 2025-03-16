using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Features.Database;
using System;


namespace Starward.Features.PlayTime;

[INotifyPropertyChanged]
public sealed partial class PlayTimeButton : UserControl
{


    public GameBiz CurrentGameBiz { get; set; }


    private readonly ILogger<PlayTimeButton> _logger = AppConfig.GetLogger<PlayTimeButton>();


    private readonly PlayTimeService _playTimeService = AppConfig.GetService<PlayTimeService>();



    public PlayTimeButton()
    {
        this.InitializeComponent();
    }



    public TimeSpan PlayTimeTotal { get; set => SetProperty(ref field, value); }


    public TimeSpan PlayTimeMonth { get; set => SetProperty(ref field, value); }


    public TimeSpan PlayTimeWeek { get; set => SetProperty(ref field, value); }


    public TimeSpan PlayTimeDay { get; set => SetProperty(ref field, value); }


    public TimeSpan PlayTimeLast { get; set => SetProperty(ref field, value); }


    public string LastPlayTimeText { get; set => SetProperty(ref field, value); }


    public int StartUpCount { get; set => SetProperty(ref field, value); }



    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitializePlayTime();
    }



    private void InitializePlayTime()
    {
        try
        {
            PlayTimeTotal = DatabaseService.GetValue<TimeSpan>($"playtime_total_{CurrentGameBiz}", out _);
            PlayTimeMonth = DatabaseService.GetValue<TimeSpan>($"playtime_month_{CurrentGameBiz}", out _);
            PlayTimeWeek = DatabaseService.GetValue<TimeSpan>($"playtime_week_{CurrentGameBiz}", out _);
            PlayTimeDay = DatabaseService.GetValue<TimeSpan>($"playtime_day_{CurrentGameBiz}", out _);
            StartUpCount = DatabaseService.GetValue<int>($"startup_count_{CurrentGameBiz}", out _);
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
            DatabaseService.SetValue($"playtime_total_{CurrentGameBiz}", PlayTimeTotal);
            DatabaseService.SetValue($"playtime_month_{CurrentGameBiz}", PlayTimeMonth);
            DatabaseService.SetValue($"playtime_week_{CurrentGameBiz}", PlayTimeWeek);
            DatabaseService.SetValue($"playtime_day_{CurrentGameBiz}", PlayTimeDay);
            DatabaseService.SetValue($"startup_count_{CurrentGameBiz}", StartUpCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update play time");
        }
    }



    public static string TimeSpanToString(TimeSpan timeSpan)
    {

        return $"{Math.Floor(timeSpan.TotalHours)}h {timeSpan.Minutes}m";
    }


}
