using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.StarRail.ForgottenHall;
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
public sealed partial class ForgottenHallPage : Page
{


    private readonly ILogger<ForgottenHallPage> _logger = AppConfig.GetLogger<ForgottenHallPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public ForgottenHallPage()
    {
        this.InitializeComponent();
    }



    private GameRecordRole gameRole;


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameRecordRole role)
        {
            gameRole = role;
        }
    }



    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(16);
        InitializeForgottenHallData();
    }



    [ObservableProperty]
    private bool hasData;


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
            if (list.Any())
            {
                HasData = true;
                ForgottenHallList = list;
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
            await _gameRecordService.RefreshForgottenHallInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshForgottenHallInfoAsync(gameRole, 2);
            InitializeForgottenHallData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh forgotten hall data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



    private void ListView_ForgottenHall_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is ForgottenHallInfo info)
            {
                CurrentForgottenHall = _gameRecordService.GetForgottenHallInfo(gameRole, info.ScheduleId);
                HasData = CurrentForgottenHall?.HasData ?? false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



}
