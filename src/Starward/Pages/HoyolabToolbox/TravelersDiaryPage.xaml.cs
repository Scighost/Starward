using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Controls;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.Genshin.TravelersDiary;
using Starward.Helpers;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class TravelersDiaryPage : Page
{


    private readonly ILogger<TravelersDiaryPage> _logger = AppConfig.GetLogger<TravelersDiaryPage>();


    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public TravelersDiaryPage()
    {
        this.InitializeComponent();
        WinUiPlot_Historical.Interaction.ContextMenuItems = new ScottPlot.Control.ContextMenuItem[0];
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
        await InitializeDataAsync();
    }




    [ObservableProperty]
    private TravelersDiarySummary currentSummary;


    [ObservableProperty]
    private TravelersDiaryMonthData? selectMonthData;


    [ObservableProperty]
    private List<TravelersDiaryMonthData> monthDataList;


    [ObservableProperty]
    private List<ColorRectChart.ChartLegend>? currentSeries;


    [ObservableProperty]
    private List<ColorRectChart.ChartLegend>? selectSeries;



    private static readonly Dictionary<int, Color> actionColorMap = new Dictionary<int, Color>()
    {
        [0] = Color.FromArgb(0xFF, 0x72, 0xA7, 0xC6),
        [1] = Color.FromArgb(0xFF, 0xD4, 0x64, 0x63),
        [2] = Color.FromArgb(0xFF, 0x6F, 0xB0, 0xB2),
        [3] = Color.FromArgb(0xFF, 0xBC, 0x99, 0x59),
        [4] = Color.FromArgb(0xFF, 0x72, 0x98, 0x6F),
        [5] = Color.FromArgb(0xFF, 0x79, 0x6B, 0xA6),
        [6] = Color.FromArgb(0xFF, 0x59, 0x7D, 0x9F),
    };



    [RelayCommand]
    private async Task InitializeDataAsync()
    {
        await Task.Delay(16);
        await GetCurrentSummaryAsync();
        GetMonthDataList();
    }




    private async Task GetCurrentSummaryAsync()
    {
        try
        {
            if (gameRole is null)
            {
                return;
            }
            CurrentSummary = await _gameRecordService.GetTravelersDiarySummaryAsync(gameRole);
            MenuFlyout_GetDetails.Items.Clear();
            foreach (int month in CurrentSummary.OptionalMonth)
            {
                MenuFlyout_GetDetails.Items.Add(new MenuFlyoutItem
                {
                    Text = new DateTime(2023, month, 1).ToString("MMM"),
                    Command = GetDataDetailsCommand,
                    CommandParameter = month,
                });
            }
            CurrentSeries = CurrentSummary.MonthData.PrimogemsGroupBy.Select(x => new ColorRectChart.ChartLegend(x.ActionName, x.Percent, actionColorMap.GetValueOrDefault(x.ActionId))).ToList();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Get realtime traveler's diary data details ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_AccountError, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Get realtime traveler's diary data details ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get realtime traveler's diary data details ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Error(ex);
        }
    }



    private void GetMonthDataList()
    {
        try
        {
            SelectMonthData = null;
            MonthDataList = _gameRecordService.GetTravelersDiaryMonthDataList(gameRole);
            Image_Emoji.Visibility = MonthDataList.Any() ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load traveler's diary month data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }





    [RelayCommand]
    private async Task GetDataDetailsAsync(int month)
    {
        try
        {
            if (gameRole is null)
            {
                return;
            }
            await _gameRecordService.GetTravelersDiarySummaryAsync(gameRole, month);
            await _gameRecordService.GetTravelersDiaryDetailAsync(gameRole, month, 1);
            await _gameRecordService.GetTravelersDiaryDetailAsync(gameRole, month, 2);
            GetMonthDataList();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Get traveler's diary data details ({gameBiz}, {uid}, {month}).", gameRole?.GameBiz, gameRole?.Uid, month);
            NotificationBehavior.Instance.Warning(Lang.Common_AccountError, ex.Message);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Get traveler's diary data details ({gameBiz}, {uid}, {month}).", gameRole?.GameBiz, gameRole?.Uid, month);
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get traveler's diary data details ({gameBiz}, {uid}, {month}).", gameRole?.GameBiz, gameRole?.Uid, month);
            NotificationBehavior.Instance.Error(ex);
        }
    }





    private void ListView_MonthDataList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is TravelersDiaryMonthData data)
            {
                SelectMonthData = data;
                SelectSeries = SelectMonthData.PrimogemsGroupBy.Select(x => new ColorRectChart.ChartLegend(x.ActionName, x.Percent, actionColorMap.GetValueOrDefault(x.ActionId))).ToList();
                RefreshDailyDataPlot(data);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }




    private void RefreshDailyDataPlot(TravelersDiaryMonthData data)
    {
        try
        {
            var items_primogems = _gameRecordService.GetTravelersDiaryDetailItems(data.Uid, data.Year, data.Month, 1);
            var items_mora = _gameRecordService.GetTravelersDiaryDetailItems(data.Uid, data.Year, data.Month, 2);
            int days = DateTime.DaysInMonth(data.Year, data.Month);
            var x = Enumerable.Range(1, days).Select(x => (double)x).ToArray();

            var stats_primogems = new double[days];
            foreach (var item in items_primogems)
            {
                var day = item.Time.Day;
                if (day <= days)
                {
                    stats_primogems[day - 1] += item.Number;
                }
            }

            var stats_mora = new double[days];
            foreach (var item in items_mora)
            {
                var day = item.Time.Day;
                if (day <= days)
                {
                    stats_mora[day - 1] += item.Number;
                }
            }

            WinUiPlot_Historical.Plot.Clear();
            WinUiPlot_Historical.Plot.Style.Background(ScottPlot.Colors.Transparent, ScottPlot.Colors.Transparent);
            WinUiPlot_Historical.Plot.Style.ColorAxes(ScottPlot.Color.FromARGB(0xC5FFFFFF));
            WinUiPlot_Historical.Plot.Style.ColorGrids(ScottPlot.Color.FromARGB(0x20FFFFFF));
            var color_primo = ScottPlot.Color.FromARGB(0xFF66BCF2);
            var color_mora = ScottPlot.Color.FromARGB(0xFFF2DE77);

            WinUiPlot_Historical.Plot.LeftAxis.MajorTickColor = color_primo;
            WinUiPlot_Historical.Plot.LeftAxis.MinorTickColor = color_primo;
            WinUiPlot_Historical.Plot.LeftAxis.FrameLineStyle.Color = color_primo;
            WinUiPlot_Historical.Plot.LeftAxis.MinorTickLength = 0;
            WinUiPlot_Historical.Plot.LeftAxis.Min = 0;
            WinUiPlot_Historical.Plot.LeftAxis.Max = stats_primogems.Max() * 1.05;

            WinUiPlot_Historical.Plot.RightAxis.MajorTickColor = color_mora;
            WinUiPlot_Historical.Plot.RightAxis.MinorTickColor = color_mora;
            WinUiPlot_Historical.Plot.RightAxis.FrameLineStyle.Color = color_mora;
            WinUiPlot_Historical.Plot.RightAxis.MinorTickLength = 0;
            WinUiPlot_Historical.Plot.RightAxis.Min = 0;
            WinUiPlot_Historical.Plot.RightAxis.Max = stats_mora.Max() * 1.05;

            WinUiPlot_Historical.Plot.BottomAxis.MinorTickLength = 0;

            var scatter_primogems = WinUiPlot_Historical.Plot.Add.Scatter(x, stats_primogems, color_primo);
            scatter_primogems.Axes.YAxis = WinUiPlot_Historical.Plot.LeftAxis;
            var scatter_mora = WinUiPlot_Historical.Plot.Add.Scatter(x, stats_mora, color_mora);
            scatter_mora.Axes.YAxis = WinUiPlot_Historical.Plot.RightAxis;

            WinUiPlot_Historical.Plot.SetAxisLimits(0.5, days + 0.5);
            WinUiPlot_Historical.Refresh();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh daily data plot");
        }
    }


    private void WinUiPlot_Historical_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        e.Handled = true;
    }


}
