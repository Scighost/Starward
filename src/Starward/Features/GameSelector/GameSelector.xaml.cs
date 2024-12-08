using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using Starward.Helpers;
using Starward.Messages;
using Starward.Services.Launcher;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using Windows.Foundation;


namespace Starward.Features.GameSelector;

[INotifyPropertyChanged]
public sealed partial class GameSelector : UserControl
{

    private const string IconPin = "\uE718";

    private const string IconUnpin = "\uE77A";


    public event EventHandler<(GameId, bool DoubleTapped)>? CurrentGameChanged;


    private HoYoPlayService _hoyoplayService = AppService.GetService<HoYoPlayService>();



    public GameSelector()
    {
        this.InitializeComponent();
        InitializeGameSelector();
    }



    public GameBiz CurrentGameBiz { get; set; }


    public GameId? CurrentGameId { get; set; }


    public ObservableCollection<GameBizIcon> GameBizIcons { get; set => SetProperty(ref field, value); } = new();


    public GameBizIcon? CurrentGameBizIcon { get; set => SetProperty(ref field, value); }


    public bool IsPinned { get; set => SetProperty(ref field, value); }




    public void InitializeGameSelector()
    {
        GameBizIcons.Clear();

        // 从配置文件读取已选的 GameBiz
        string? bizs = AppSetting.SelectedGameBizs;
        foreach (string str in bizs?.Split(',') ?? [])
        {
            if (GameBiz.TryParse(str, out GameBiz biz))
            {
                // 已知的 GameBiz
                GameBizIcons.Add(new GameBizIcon(biz));
            }
            else if (_hoyoplayService.GetCachedGameInfo(biz) is GameInfo info)
            {
                // 由 HoYoPlay API 获取，但未适配的 GameBiz
                GameBizIcons.Add(new GameBizIcon(info));
            }
        }

        // 选取当前游戏
        GameBiz lastSelectedGameBiz = AppSetting.CurrentGameBiz;
        if (GameBizIcons.FirstOrDefault(x => x.GameBiz == lastSelectedGameBiz) is GameBizIcon icon)
        {
            CurrentGameBizIcon = icon;
            CurrentGameBizIcon.IsSelected = true;
            CurrentGameBiz = lastSelectedGameBiz;
        }
        else if (lastSelectedGameBiz.IsKnown())
        {
            CurrentGameBizIcon = new GameBizIcon(lastSelectedGameBiz);
            CurrentGameBiz = lastSelectedGameBiz;
        }

        CurrentGameId = CurrentGameBizIcon?.GameId;
        if (CurrentGameId is not null)
        {
            CurrentGameChanged?.Invoke(this, (CurrentGameId, false));
        }

        if (AppSetting.IsGameBizSelectorPinned)
        {
            Pin();
        }
    }



    [RelayCommand]
    public void AutoSearchInstalledGames()
    {
        try
        {
            // todo 从注册表自动搜索已安装的游戏
            var service = AppService.GetService<GameLauncherService>();
            var sb = new StringBuilder();
            foreach (GameBiz biz in GameBiz.AllGameBizs)
            {
                if (service.IsGameExeExists(biz))
                {
                    sb.Append(biz.ToString());
                    sb.Append(',');
                }
            }
            AppSetting.SelectedGameBizs = sb.ToString().TrimEnd(',');
            InitializeGameSelector();
        }
        catch (Exception ex)
        {

        }
    }




    #region Game Icon



    /// <summary>
    /// 游戏图标区域是否可见
    /// </summary>
    public bool GameIconsAreaVisible
    {
        get => Border_GameIconsArea.Translation == Vector3.Zero;
        set
        {
            if (value)
            {
                Border_GameIconsArea.Translation = Vector3.Zero;
            }
            else
            {
                Border_GameIconsArea.Translation = new Vector3(0, -100, 0);
            }
            UpdateDragRectangles();
        }
    }


    /// <summary>
    /// 更新窗口拖拽区域
    /// </summary>
    public void UpdateDragRectangles()
    {
        try
        {
            double x = Border_CurrentGameIcon.ActualWidth;
            if (GameIconsAreaVisible)
            {
                x = Border_CurrentGameIcon.ActualWidth + Border_GameIconsArea.ActualWidth;
            }
            this.XamlRoot.SetWindowDragRectangles([new Rect(x, 0, 10000, 48)]);
        }
        catch { }
    }



    /// <summary>
    /// 鼠标移入到当前游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Border_CurrentGameIcon_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        // 显示所有游戏图标
        GameIconsAreaVisible = true;
    }



    /// <summary>
    /// 鼠标移出当前游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Border_CurrentGameIcon_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (FullBackgroundVisible || IsPinned)
        {
            // 当前游戏图标被固定或者全屏显示时，不隐藏所有游戏图标
            return;
        }
        if (sender is UIElement ele)
        {
            var postion = e.GetCurrentPoint(sender as UIElement).Position;
            if (postion.X > ele.ActualSize.X - 1 && postion.Y > 0 && postion.Y < ele.ActualSize.Y)
            {
                // 从右侧移出，此时进入到所有游戏图标区域，不隐藏
                return;
            }
        }
        // 其他方向移出，隐藏所有游戏图标
        GameIconsAreaVisible = false;
    }



    /// <summary>
    /// 鼠标移出所有游戏图标区域
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Border_GameIconsArea_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (FullBackgroundVisible || IsPinned)
        {
            return;
        }
        GameIconsAreaVisible = false;
    }



    /// <summary>
    /// 点击没有被选择的游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_GameIcon_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
        {
            if (CurrentGameBizIcon is not null)
            {
                CurrentGameBizIcon.IsSelected = false;
            }

            CurrentGameBizIcon = icon;
            CurrentGameBiz = icon.GameBiz;
            CurrentGameId = icon.GameId;
            icon.IsSelected = true;

            CurrentGameChanged?.Invoke(this, (icon.GameId, false));
            AppSetting.CurrentGameBiz = icon.GameBiz;
        }
    }



    /// <summary>
    /// 双击没有被选择的游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_GameIcon_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
        {
            if (CurrentGameBizIcon is not null)
            {
                CurrentGameBizIcon.IsSelected = false;
            }

            CurrentGameBizIcon = icon;
            CurrentGameBiz = icon.GameBiz;
            CurrentGameId = icon.GameId;
            icon.IsSelected = true;
            HideFullBackground();

            CurrentGameChanged?.Invoke(this, (icon.GameId, true));
            AppSetting.CurrentGameBiz = icon.GameBiz;
        }
    }


    /// <summary>
    /// 鼠标移入到游戏图标
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Button_GameIcon_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
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
    private void Button_GameIcon_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
        {
            if (!icon.IsSelected)
            {
                icon.MaskOpacity = 1;
            }
        }
    }



    /// <summary>
    /// 游戏图标区域大小变化
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Border_GameIconsArea_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }




    [RelayCommand]
    private void Pin()
    {
        IsPinned = !IsPinned;
        if (IsPinned)
        {
            FontIcon_Pin.Glyph = IconUnpin;
            GameIconsAreaVisible = true;
        }
        else
        {
            FontIcon_Pin.Glyph = IconPin;
            // 避免在固定时更换当前游戏，取消固定后，左上角的图标不改变的问题
            var temp = CurrentGameBizIcon;
            CurrentGameBizIcon = null;
            CurrentGameBizIcon = temp;
            if (!FullBackgroundVisible)
            {
                GameIconsAreaVisible = false;
            }
        }
        AppSetting.IsGameBizSelectorPinned = IsPinned;
    }




    #endregion






    #region Full Background 黑色半透明背景



    public bool FullBackgroundVisible => Border_FullBackground.Opacity > 0;


    [RelayCommand]
    private void ShowFullBackground()
    {
        Border_FullBackground.Opacity = 1;
        Border_FullBackground.IsHitTestVisible = true;
        GameIconsAreaVisible = true;
    }


    private void HideFullBackground()
    {
        Border_FullBackground.Opacity = 0;
        Border_FullBackground.IsHitTestVisible = false;
        if (!IsPinned)
        {
            GameIconsAreaVisible = false;
        }
    }


    private void Border_FullBackground_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        var position = e.GetPosition(sender as UIElement);
        if (position.X <= Border_CurrentGameIcon.ActualWidth && position.Y <= Border_CurrentGameIcon.ActualHeight)
        {
            Border_FullBackground.Opacity = 0;
            Border_FullBackground.IsHitTestVisible = false;
        }
        else
        {
            HideFullBackground();
        }
    }



    #endregion






    public void OnLanguageChanged(object? sender, LanguageChangedMessage message)
    {
        if (message.Completed)
        {
            this.Bindings.Update();
        }
    }









}

