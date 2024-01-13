using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Core.GameRecord;
using Starward.Core.GameRecord.Genshin.SpiralAbyss;
using Starward.Helpers;
using Starward.Messages;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SpiralAbyssPage : PageBase
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
        if (e.Parameter is GameRecordRole role)
        {
            gameRole = role;
        }
    }



    protected override async void OnLoaded()
    {
        await Task.Delay(160);
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
                AbyssList = list;
                ListView_AbyssList.SelectedIndex = 0;
            }
            else
            {
                Image_Emoji.Visibility = Visibility.Visible;
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
            if (gameRole is null)
            {
                return;
            }
            await _gameRecordService.RefreshSpiralAbyssInfoAsync(gameRole, 1);
            await _gameRecordService.RefreshSpiralAbyssInfoAsync(gameRole, 2);
            InitializeAbyssData();
        }
        catch (miHoYoApiException ex)
        {
            _logger.LogError(ex, "Refresh abyss data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            if (ex.ReturnCode is 1034 or 5003)
            {
                NotificationBehavior.Instance.ShowWithButton(InfoBarSeverity.Warning, Lang.Common_AccountError, ex.Message, Lang.HoyolabToolboxPage_VerifyAccount, () =>
                {
                    WeakReferenceMessenger.Default.Send(new VerifyAccountMessage(gameRole!, "https://webstatic.mihoyo.com/app/community-game-records/index.html?bbs_presentation_style=fullscreen#/ys/deep?role_id={role_id}&server={server}"));
                });
            }
            else
            {
                NotificationBehavior.Instance.Warning(Lang.Common_AccountError, ex.Message);
            }
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Refresh abyss data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Warning(Lang.Common_NetworkError, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Refresh abyss data ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
            NotificationBehavior.Instance.Error(ex);
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
                Image_Emoji.Visibility = HasData ? Visibility.Collapsed : Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Selection changed ({gameBiz}, {uid}).", gameRole?.GameBiz, gameRole?.Uid);
        }
    }


    private void UserControl_AbyssLevel_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (e.NewSize.Width > 720)
        {
            VisualStateManager.GoToState((Control)sender, "WideState", false);
        }
        else
        {
            VisualStateManager.GoToState((Control)sender, "NarrowState", false);
        }
    }



    public static string FloorX(int x)
    {
        return Lang.SpiralAbyssPage_FloorX.Replace("{X}", x.ToString());
    }



    public static string LevelX(int x)
    {
        return Lang.SpiralAbyssPage_ChamberX.Replace("{X}", x.ToString());
    }


}
