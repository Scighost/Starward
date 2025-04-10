using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.ZZZ.DeadlyAssault;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.GameRecord.ZZZ;

public sealed partial class DeadlyAssaultPage : PageBase
{


    private readonly ILogger<DeadlyAssaultPage> _logger = AppConfig.GetLogger<DeadlyAssaultPage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public DeadlyAssaultPage()
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
        InitializeDeadlyAssaultInfoData();
    }



    protected override void OnUnloaded()
    {
        CurrentDeadlyAssault = null;
        DeadlyAssaultList = null!;
    }



    public List<DeadlyAssaultInfo> DeadlyAssaultList { get; set => SetProperty(ref field, value); }


    public DeadlyAssaultInfo? CurrentDeadlyAssault { get; set => SetProperty(ref field, value); }



    private void InitializeDeadlyAssaultInfoData()
    {
        try
        {
            CurrentDeadlyAssault = null;
            var list = _gameRecordService.GetDeadlyAssaultInfoList(gameRole);
            if (list.Count != 0)
            {
                DeadlyAssaultList = list;
                ListView_DeadlyAssault.SelectedIndex = 0;
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
            await _gameRecordService.RefreshDeadlyAssaultInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshDeadlyAssaultInfoAsync(gameRole, 2);
            InitializeDeadlyAssaultInfoData();
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



    private void ListView_DeadlyAssault_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (e.AddedItems.FirstOrDefault() is DeadlyAssaultInfo info)
            {
                CurrentDeadlyAssault = _gameRecordService.GetDeadlyAssaultInfo(gameRole, info.ZoneId);
                Image_Emoji.Visibility = (CurrentDeadlyAssault?.HasData ?? false) ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



    public static string RankPercentText(int value)
    {
        int d = value / 100;
        int p = value % 100;
        return $"{d}.{p:D2}%";
    }



}
