using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.ViewHost;
using Starward.GameInstall;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;


namespace Starward.Features.GameSelector;

[INotifyPropertyChanged]
public sealed partial class GameSwitcher : UserControl
{

    private readonly ILogger<GameSwitcher> _logger = AppConfig.GetLogger<GameSwitcher>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();

    public event EventHandler<PinnedGameIcon>? CurrentGameChanged;

    public ObservableCollection<PinnedGameIcon> PinnedGameIcons { get; set => SetProperty(ref field, value); } = new();

    public PinnedGameIcon? CurrentGameIcon { get; set => SetProperty(ref field, value); }



    public GameSwitcher()
    {
        InitializeComponent();
        InitializeSwitcher();
        WeakReferenceMessenger.Default.Register<PinGameIconMessage>(this, HandlePinGameIconMessage);
        WeakReferenceMessenger.Default.Register<MainViewNavigateMessage>(this, HandleMainViewNavigateMessage);
        WeakReferenceMessenger.Default.Register<EnabledLauncherConfigChangedMessage>(this, HandleEnabledLauncherConfigChangedMessage);
    }



    public void InitializeSwitcher()
    {
        try
        {
            PinnedGameIcons.CollectionChanged -= PinnedGameIcons_CollectionChanged;
            PinnedGameIcons.Clear();

            List<GameEntry> entries = GameEntryService.GetPinnedEntries();
            List<LauncherConfigModel> launchers = LauncherConfigModel.GetEnabled();
            foreach (var item in entries)
            {
                GameId gameId = new(item.GameId, item.GameBiz);
                LauncherConfigModel? launcher = launchers.FirstOrDefault(x => x.Id == item.LauncherId);
                if (launcher is null)
                {
                    continue;
                }
                GameInfo? gameInfo = _hoYoPlayService.GetCachedGameInfo(gameId);
                if (gameInfo is not null)
                {
                    PinnedGameIcons.Add(new PinnedGameIcon(launcher, gameInfo));
                }
            }
            CurrentGameIcon = PinnedGameIcons.FirstOrDefault(x => x.GameId.Id == AppConfig.CurrentGameId);
            if (CurrentGameIcon is not null)
            {
                CurrentGameIcon.IsSelected = true;
            }
            _ = UpdatePinnedGameIconsAsync(entries);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize GameSwitcher.");
        }
        finally
        {
            PinnedGameIcons.CollectionChanged += PinnedGameIcons_CollectionChanged;
        }
    }



    private async Task UpdatePinnedGameIconsAsync(List<GameEntry> list)
    {
        await Task.Delay(1000);
        var launchers = LauncherConfigModel.GetEnabled();
        try
        {
            await _hoYoPlayService.UpdateLauncherClientMapAsync(launchers.Select(x => x.ToLauncherConfig()));
        }
        catch { }

        PinnedGameIcons.CollectionChanged -= PinnedGameIcons_CollectionChanged;
        try
        {
            int index = 0;
            foreach (var item in list)
            {
                GameId gameId = new(item.GameId, item.GameBiz);
                LauncherConfigModel? launcher = launchers.FirstOrDefault(x => x.Id == item.LauncherId);
                if (launcher is null)
                {
                    continue;
                }
                GameInfo? gameInfo = await _hoYoPlayService.GetGameInfoAsync(gameId);
                if (gameInfo is null)
                {
                    continue;
                }
                PinnedGameIcon? icon = PinnedGameIcons.FirstOrDefault(x => x.GameId == gameId);
                if (icon is null)
                {
                    icon = new PinnedGameIcon(launcher, gameInfo);
                    PinnedGameIcons.Insert(index, icon);
                }
                else
                {
                    icon.UpdateInfo(gameInfo);
                }
                index++;
            }
        }
        finally
        {
            PinnedGameIcons.CollectionChanged += PinnedGameIcons_CollectionChanged;
        }
    }


    private async void HandlePinGameIconMessage(object sender, PinGameIconMessage e)
    {
        try
        {
            GameId gameId = e.GameId;
            if (gameId.GameBiz == GameBiz.bh3_global)
            {
                gameId = new GameId(GameId.bh3_glb, GameBiz.bh3_global);
            }
            var icon = PinnedGameIcons.FirstOrDefault(x => x.GameId == gameId);
            if (e.IsPinned)
            {
                GameInfo? gameInfo = gameId as GameInfo ?? await _hoYoPlayService.GetGameInfoAsync(gameId);
                if (gameInfo is null)
                {
                    return;
                }
                if (icon is null)
                {
                    LauncherConfigModel? model = e.LauncherModel ?? LauncherConfigModel.FromId(e.LauncherConfig?.Id);
                    if (model is null)
                    {
                        return;
                    }
                    icon = new PinnedGameIcon(model, gameInfo);
                    PinnedGameIcons.Add(icon);
                }
                else
                {
                    icon.UpdateInfo(gameInfo);
                }
            }
            else if (icon is not null)
            {
                PinnedGameIcons.Remove(icon);
            }
        }
        catch { }
    }


    private void HandleMainViewNavigateMessage(object sender, MainViewNavigateMessage e)
    {
        try
        {
            if (e.GameId is not null)
            {
                CurrentGameIcon?.IsSelected = false;
                if (PinnedGameIcons.FirstOrDefault(x => x.GameId == e.GameId) is PinnedGameIcon icon)
                {
                    icon.IsSelected = true;
                    CurrentGameIcon = icon;
                }
            }
        }
        catch { }
    }


    private void HandleEnabledLauncherConfigChangedMessage(object sender, EnabledLauncherConfigChangedMessage e)
    {
        InitializeSwitcher();
    }



    /// <summary>
    /// 待选游戏变更时，保存配置
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void PinnedGameIcons_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        try
        {
            GameEntryService.SavePinnedIcons(PinnedGameIcons);
        }
        catch { }
    }



    /// <summary>
    /// 点击没有被选择的游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIcon_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is PinnedGameIcon icon)
        {
            CurrentGameIcon?.IsSelected = false;
            CurrentGameIcon = icon;
            icon.IsSelected = true;

            CurrentGameChanged?.Invoke(this, icon);
            AppConfig.CurrentGameId = icon.GameId.Id;
        }
    }



    /// <summary>
    /// 鼠标移入到游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIcon_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is PinnedGameIcon icon)
        {
            if (!icon.IsSelected)
            {
                icon.MaskOpacity = 0;
            }
        }
    }



    /// <summary>
    /// 鼠标移出游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Grid_GameIcon_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.DataContext is PinnedGameIcon icon)
        {
            if (!icon.IsSelected)
            {
                icon.MaskOpacity = 1;
            }
        }
    }



}
