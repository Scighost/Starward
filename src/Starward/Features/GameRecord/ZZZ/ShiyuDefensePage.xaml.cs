using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.ZZZ.ShiyuDefense;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord.ZZZ;

public sealed partial class ShiyuDefensePage : PageBase
{


    private readonly ILogger<ShiyuDefensePage> _logger = AppConfig.GetLogger<ShiyuDefensePage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public ShiyuDefensePage()
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
        InitializeShiyuDefenseInfoData();
    }



    protected override void OnUnloaded()
    {
        CurrentShiyuDefense = null;
        CurrentShiyuDefenseV2 = null;
        ShiyuDefenseList = null!;
    }



    public List<ShiyuDefenseInfo> ShiyuDefenseList { get; set => SetProperty(ref field, value); }


    public ShiyuDefenseInfo? CurrentShiyuDefense { get; set => SetProperty(ref field, value); }


    public ShiyuDefenseInfoV2? CurrentShiyuDefenseV2 { get; set => SetProperty(ref field, value); }



    private void InitializeShiyuDefenseInfoData()
    {
        try
        {
            CurrentShiyuDefense = null;
            CurrentShiyuDefenseV2 = null;
            var list = _gameRecordService.GetShiyuDefenseInfoList(gameRole);
            if (list.Count != 0)
            {
                ShiyuDefenseList = list;
                ListView_ShiyuDefense.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init shiyu defense data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
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
            await _gameRecordService.RefreshShiyuDefenseInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshShiyuDefenseInfoAsync(gameRole, 2);
            InitializeShiyuDefenseInfoData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh shiyu defense data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            GameRecordPage.HandleMiHoYoApiException(ex);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh shiyu defense data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh shiyu defense data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            InAppToast.MainWindow?.Error(ex);
        }
    }



    private void ListView_ShiyuDefense_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is ShiyuDefenseInfo info)
            {
                CurrentShiyuDefense = null;
                CurrentShiyuDefenseV2 = null;
                var detail = _gameRecordService.GetShiyuDefenseInfo(gameRole, info.ScheduleId);
                if (detail is ShiyuDefenseInfo infoV1)
                {
                    CurrentShiyuDefense = infoV1;
                }
                else if (detail is ShiyuDefenseInfoV2 infov2)
                {
                    CurrentShiyuDefenseV2 = infov2;
                }
                Image_Emoji.Visibility = (info?.HasData ?? false) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }


    public static string PerformancesTime(int second)
    {
        var ts = TimeSpan.FromSeconds(second);
        return $"{ts.Minutes}m {ts.Seconds:D2}s";
    }


    public static Visibility ElementWeaknessVisibility(int value, int weakOrResist)
    {
        return value == weakOrResist ? Visibility.Visible : Visibility.Collapsed;
    }



    public static Visibility V1VersionVisibility(string value)
    {
        return value is "v1" ? Visibility.Visible : Visibility.Collapsed;
    }


    public static Visibility V2VersionVisibility(string value)
    {
        return value is "v2" ? Visibility.Visible : Visibility.Collapsed;
    }



}
