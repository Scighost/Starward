using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.StarRail.SimulatedUniverse;
using Starward.Helpers;
using Starward.Messages;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SimulatedUniversePage : PageBase
{


    private readonly ILogger<SimulatedUniversePage> _logger = AppConfig.GetLogger<SimulatedUniversePage>();

    private readonly GameRecordService _gameRecordService = AppConfig.GetService<GameRecordService>();



    public SimulatedUniversePage()
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
        InitializeSimulatedUniverseRecord();
    }




    [ObservableProperty]
    private SimulatedUniverseBasicStats basicInfo;


    [ObservableProperty]
    private SimulatedUniverseRecord? currentRecord;


    [ObservableProperty]
    private List<SimulatedUniverseRecordBasic> recordBasicList;




    private void InitializeSimulatedUniverseRecord()
    {
        try
        {
            if (gameRole is null)
            {
                Image_Emoji.Visibility = Visibility.Visible;
                return;
            }
            CurrentRecord = null;
            var list = _gameRecordService.GetSimulatedUniverseRecordBasics(gameRole);
            if (list.Count != 0)
            {
                RecordBasicList = list;
                ListView_SimulatedUniverse.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Init simulated universe record basic data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }



    [RelayCommand]
    private async Task GetSimulatedUniverseDetailAsync()
    {
        await GetSimulatedUniverseInfoBasicAsync(true);
        InitializeSimulatedUniverseRecord();
    }



    private async Task GetSimulatedUniverseInfoBasicAsync(bool detail = false)
    {
        try
        {
            if (gameRole is null)
            {
                return;
            }
            var info = await _gameRecordService.GetSimulatedUniverseInfoAsync(gameRole, detail);
            BasicInfo = info.BasicInfo;
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Get simulated universe data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            if (ex.ReturnCode is 1034 or 5003)
            {
                NotificationBehavior.Instance.ShowWithButton(InfoBarSeverity.Warning, Lang.Common_AccountError, ex.Message, Lang.HoyolabToolboxPage_VerifyAccount, () =>
                {
                    WeakReferenceMessenger.Default.Send(new VerifyAccountMessage(gameRole!, "https://webstatic.mihoyo.com/app/community-game-records/rpg/index.html?bbs_presentation_style=fullscreen#/rpg/rogue?role_id={role_id}&server={server}"));
                });
            }
            else
            {
                NotificationBehavior.Instance.Warning(Lang.Common_AccountError, ex.Message);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Get simulated universe data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get simulated universe data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Error(ex);
        }
    }



    private void ListView_SimulatedUniverse_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is SimulatedUniverseRecordBasic data)
        {
            GetSimulatedUniverseRecord(data);
        }
    }



    private void GetSimulatedUniverseRecord(SimulatedUniverseRecordBasic data)
    {
        try
        {
            if (gameRole is null)
            {
                return;
            }
            CurrentRecord = _gameRecordService.GetSimulatedUniverseRecord(gameRole, data.ScheduleId);
            Image_Emoji.Visibility = (CurrentRecord?.HasData ?? false) ? Visibility.Collapsed : Visibility.Visible;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get stimulated record (Uid {uid}, ScheduleId {scheduleId})", gameRole.Uid, data.ScheduleId);
        }
    }



}
