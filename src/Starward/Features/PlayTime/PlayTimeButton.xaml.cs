using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Features.Database;
using System;
using System.Threading.Tasks;


namespace Starward.Features.PlayTime;

[INotifyPropertyChanged]
public sealed partial class PlayTimeButton : UserControl
{


    public GameBiz CurrentGameBiz { get; set; }


    private readonly ILogger<PlayTimeButton> _logger = AppConfig.GetLogger<PlayTimeButton>();



    public PlayTimeButton()
    {
        this.InitializeComponent();
    }



    public TimeSpan PlayTimeTotal { get; set => SetProperty(ref field, value); }



    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitializePlayTime();
    }



    private void InitializePlayTime()
    {
        try
        {
            GameBiz gameBiz = CurrentGameBiz.IsBilibili() ? $"{CurrentGameBiz.Game}_cn" : CurrentGameBiz;
            PlayTimeTotal = DatabaseService.GetValue<TimeSpan>($"playtime_total_{gameBiz}", out _);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize play time");
        }
    }



    public static string TimeSpanToString(TimeSpan timeSpan)
    {

        return $"{Math.Floor(timeSpan.TotalHours)}h {timeSpan.Minutes}m";
    }


    [RelayCommand]
    private async Task OpenStatsDialogAsync()
    {
        await new PlayTimeStatsDialog
        {
            CurrentGameBiz = CurrentGameBiz,
            XamlRoot = this.XamlRoot,
        }.ShowAsync();
        InitializePlayTime();
    }


}
