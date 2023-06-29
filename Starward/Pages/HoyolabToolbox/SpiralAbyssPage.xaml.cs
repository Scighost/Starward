using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SpiralAbyssPage : Page
{


    private readonly ILogger<SpiralAbyssPage> _logger = AppConfig.GetLogger<SpiralAbyssPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public SpiralAbyssPage()
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
        InitializeAbyssData();
    }





    [ObservableProperty]
    private bool hasData;



    [ObservableProperty]
    private List<SpiralAbyssInfo> abyssList;


    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(RevealRankInternalStar))]
    private SpiralAbyssInfo? currentAbyss;


    public List<int> RevealRankInternalStar => Enumerable.Range(0, Math.Clamp((CurrentAbyss?.RevealRank?.Count ?? 1) - 1, 0, int.MaxValue)).ToList();


    private void InitializeAbyssData()
    {
        try
        {
            CurrentAbyss = null;
            var list = _gameRecordService.GetSpiralAbyssInfoList(gameRole);
            if (list.Any())
            {
                HasData = true;
                AbyssList = list;
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
            await _gameRecordService.RefreshSpiralAbyssInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshSpiralAbyssInfoAsync(gameRole, 2);
            InitializeAbyssData();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh abyss data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }




    private void ListView_AbyssList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is SpiralAbyssInfo info)
            {
                CurrentAbyss = _gameRecordService.GetSpiralAbyssInfo(gameRole, info.ScheduleId);
                HasData = CurrentAbyss?.TotalBattleCount > 0;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



}
