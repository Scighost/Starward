using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using NuGet.Versioning;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.Gacha;
using Starward.Features.GameLauncher;
using Starward.Features.GamepadControl;
using Starward.Features.GameRecord;
using Starward.Features.GameSetting;
using Starward.Features.RPC;
using Starward.Features.Screenshot;
using Starward.Features.SelfQuery;
using Starward.Features.Setting;
using Starward.Features.Update;
using Starward.Helpers;
using System;
using System.Net.Http;
using System.Threading.Tasks;


namespace Starward.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class MainView : UserControl
{


    private readonly ILogger<MainView> _logger = AppConfig.GetLogger<MainView>();


    public GameId? CurrentGameId { get; private set => SetProperty(ref field, value); }


    private GameFeatureConfig CurrentGameFeatureConfig { get; set; }



    public MainView()
    {
        this.InitializeComponent();
        InitializeMainView();
    }



    private void InitializeMainView()
    {
        this.Loaded += MainView_Loaded;
        GameId? gameId = GameSelector.CurrentGameId;
        if (gameId?.GameBiz == GameBiz.bh3_global)
        {
            string? id = AppConfig.LastGameIdOfBH3Global;
            if (!string.IsNullOrWhiteSpace(id))
            {
                gameId.Id = id;
            }
        }
        CurrentGameId = gameId;
        CurrentGameFeatureConfig = GameFeatureConfig.FromGameId(CurrentGameId);
        UpdateNavigationView();
        WeakReferenceMessenger.Default.Register<MainViewNavigateMessage>(this, OnMainViewNavigateMessageReceived);
        WeakReferenceMessenger.Default.Register<BH3GlobalGameServerChangedMessage>(this, OnBH3GlobalGameServerChanged);
    }




    private void MainView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        CheckSystemProxy();
        HotkeyManager.InitializeHotkey(this.XamlRoot.GetWindowHandle());
        _ = CheckUpdateOrShowRecentUpdateContentAsync();
        AppConfig.GetService<RpcService>().TrySetEnviromentAsync();
        if (AppConfig.EnableGamepadController)
        {
            GamepadController.Initialize();
        }
        LogUploadService.Start();
    }




    private void GameSelector_CurrentGameChanged(object? sender, (GameId, bool DoubleTapped) e)
    {
        if (e.Item1.GameBiz == GameBiz.bh3_global)
        {
            // 崩坏3国际服区服
            string? id = AppConfig.LastGameIdOfBH3Global;
            if (!string.IsNullOrWhiteSpace(id))
            {
                e.Item1.Id = id;
            }
        }
        CurrentGameId = e.Item1;
        CurrentGameFeatureConfig = GameFeatureConfig.FromGameId(CurrentGameId);
        UpdateNavigationView();
    }



    private void OnBH3GlobalGameServerChanged(object _, BH3GlobalGameServerChangedMessage message)
    {
        if (CurrentGameId?.GameBiz == GameBiz.bh3_global)
        {
            CurrentGameId.Id = message.GameId;
            OnPropertyChanged(nameof(CurrentGameId));
            NavigateTo(typeof(GameLauncherPage), CurrentGameId, new SuppressNavigationTransitionInfo());
        }
    }




    #region Navigation





    private void UpdateNavigationView()
    {
        NavigationViewItem_Launcher.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GameLauncherPage)).ToVisibility();
        NavigationViewItem_GameSetting.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GameSettingPage)).ToVisibility();
        NavigationViewItem_Screenshot.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(ScreenshotPage)).ToVisibility();
        NavigationViewItem_GachaLog.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GachaLogPage)).ToVisibility();
        NavigationViewItem_HoyolabToolbox.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GameRecordPage)).ToVisibility();
        NavigationViewItem_SelfQuery.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(SelfQueryPage)).ToVisibility();
        NavigationViewItem_GenshinBeyondGacha.Visibility = CurrentGameFeatureConfig.SupportedPages.Contains(nameof(GenshinBeyondGachaPage)).ToVisibility();

        // 抽卡记录名称
        string gachalogText = CurrentGameId?.GameBiz.Game switch
        {
            GameBiz.hk4e => Lang.GachaLogService_WishRecords,
            GameBiz.hkrpg => Lang.GachaLogService_WarpRecords,
            GameBiz.nap => Lang.GachaLogService_SignalSearchRecords,
            _ => "",
        };

        if (CurrentGameId?.GameBiz.IsChinaServer() ?? false)
        {
            ToolTipService.SetToolTip(NavigationViewItem_HoyolabToolbox, Lang.HyperionToolbox);
            TextBlock_HoyolabToolbox.Text = Lang.HyperionToolbox;
        }
        if (CurrentGameId?.GameBiz.IsGlobalServer() ?? false)
        {
            ToolTipService.SetToolTip(NavigationViewItem_HoyolabToolbox, Lang.HoYoLABToolbox);
            TextBlock_HoyolabToolbox.Text = Lang.HoYoLABToolbox;
        }

        if (CurrentGameId is null)
        {
            NavigateTo(typeof(BlankPage));
        }
        else if (MainView_Frame.SourcePageType?.Name is not nameof(SettingPage))
        {
            NavigateTo(MainView_Frame.SourcePageType);
        }
    }



    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        try
        {
            if (args.InvokedItemContainer?.IsSelected ?? false)
            {
                return;
            }
            if (args.IsSettingsInvoked)
            {
                NavigateTo(typeof(SettingPage));
            }
            else
            {
                if (args.InvokedItemContainer is NavigationViewItem item)
                {
                    var type = item.Tag switch
                    {
                        nameof(GameLauncherPage) => typeof(GameLauncherPage),
                        nameof(GameSettingPage) => typeof(GameSettingPage),
                        nameof(ScreenshotPage) => typeof(ScreenshotPage),
                        nameof(GachaLogPage) => typeof(GachaLogPage),
                        nameof(GameRecordPage) => typeof(GameRecordPage),
                        nameof(SelfQueryPage) => typeof(SelfQueryPage),
                        nameof(GenshinBeyondGachaPage) => typeof(GenshinBeyondGachaPage),
                        _ => null,
                    };
                    NavigateTo(type);
                }
            }
        }
        catch { }
    }



    private void NavigateTo(Type? page, object? param = null, NavigationTransitionInfo? infoOverride = null)
    {
        page ??= typeof(GameLauncherPage);
        if (page.Name is nameof(BlankPage) && CurrentGameId is null)
        {

        }
        else if (page.Name is not nameof(SettingPage) && !CurrentGameFeatureConfig.SupportedPages.Contains(page.Name))
        {
            page = typeof(GameLauncherPage);
        }
        if (page.Name is nameof(GameLauncherPage))
        {
            MainView_NavigationView.SelectedItem = NavigationViewItem_Launcher;
        }
        MainView_Frame.Navigate(page, param ?? CurrentGameId, infoOverride);
        if (page.Name is nameof(BlankPage) or nameof(GameLauncherPage))
        {
            Border_OverlayMask.Opacity = 0;
        }
        else
        {
            Border_OverlayMask.Opacity = 1;
        }
    }



    private void OnMainViewNavigateMessageReceived(object _, MainViewNavigateMessage message)
    {
        NavigateTo(message.Page);
    }




    #endregion




    private async Task CheckUpdateOrShowRecentUpdateContentAsync()
    {
        try
        {
#if CI || DEBUG
            return;
#endif
#pragma warning disable CS0162 // 检测到无法访问的代码
            await Task.Delay(500);
#pragma warning restore CS0162 // 检测到无法访问的代码
            if (NuGetVersion.TryParse(AppConfig.AppVersion, out var appVersion))
            {
                _ = NuGetVersion.TryParse(AppConfig.LastAppVersion, out var lastVersion);
                if (appVersion != lastVersion)
                {
                    if (AppConfig.ShowUpdateContentAfterUpdateRestart)
                    {
                        new UpdateWindow().Activate();
                    }
                    else
                    {
                        AppConfig.LastAppVersion = AppConfig.AppVersion;
                    }
                    return;
                }
            }
            var release = await AppConfig.GetService<UpdateService>().CheckUpdateAsync(false);
            if (release != null)
            {
                new UpdateWindow { NewVersion = release }.Activate();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check update");
        }
    }




    private async void CheckSystemProxy()
    {
        try
        {
            await Task.Delay(1500);
            Uri? proxy = HttpClient.DefaultProxy.GetProxy(new Uri("https://starward.scighost.com"));
            if (proxy is not null)
            {
                InAppToast.MainWindow?.Information(Lang.MainView_CheckSystemProxy_SystemProxyIsEnabled, proxy.ToString(), 5000);
            }
        }
        catch { }
    }





}



file static class BoolToVisibilityExtension
{

    public static Visibility ToVisibility(this bool value)
    {
        return value ? Visibility.Visible : Visibility.Collapsed;
    }

}
