using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Messages;
using Starward.Models;
using Starward.Services.Launcher;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using Windows.Graphics;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.


namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class GameBizSelector : UserControl
{


    private const string IconPin = "\uE718";

    private const string IconUnpin = "\uE77A";


    public event EventHandler<(GameBiz, bool DoubleTapped)>? GameBizChanged;


    public GameBizSelector()
    {
        this.InitializeComponent();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, OnLanguageChanged);
    }



    public GameBiz CurrentGameBiz { get; set; }



    [ObservableProperty]
    private ObservableCollection<GameBizIcon> gameBizIcons = new();


    [ObservableProperty]
    private GameBizIcon? currentGameBizIcon;


    [ObservableProperty]
    private bool isPinned;




    public void InitializeGameBiz(GameBiz gameBiz)
    {
        CurrentGameBiz = gameBiz;
        string? bizs = AppConfig.SelectedGameBizs;
        GameBizIcons.Clear();
        foreach (string str in bizs?.Split(',') ?? [])
        {
            if (GameBiz.TryParse(str, out GameBiz biz))
            {
                var icon = new GameBizIcon { GameBiz = biz };
                GameBizIcons.Add(icon);
            }
        }

        if (GameBizIcons.FirstOrDefault(x => x.GameBiz == CurrentGameBiz) is GameBizIcon icon2)
        {
            CurrentGameBizIcon = icon2;
            CurrentGameBizIcon.CurrentGameBiz = true;
            CurrentGameBizIcon.MaskOpacity = 0;
        }
        else if (CurrentGameBiz.IsKnown())
        {
            CurrentGameBizIcon = new GameBizIcon { GameBiz = CurrentGameBiz };
        }

        int a = VisualTreeHelper.GetChildrenCount(StackPanel_GameServerSelector);
        for (int i = 0; i < a; i++)
        {
            var obj = VisualTreeHelper.GetChild(StackPanel_GameServerSelector, i);
            int b = VisualTreeHelper.GetChildrenCount(obj);
            for (int j = 0; j < b; j++)
            {
                if (VisualTreeHelper.GetChild(obj, j) is ToggleSwitch toggleSwitch)
                {
                    if (toggleSwitch.Tag is string str)
                    {
                        if (bizs?.Contains(str) ?? false)
                        {
                            toggleSwitch.IsOn = true;
                        }
                    }
                    toggleSwitch.Toggled -= ToggleSwitch_GameBizSelector_Toggled;
                    toggleSwitch.Toggled += ToggleSwitch_GameBizSelector_Toggled;
                }
            }
        }

        if (AppConfig.IsGameBizSelectorPinned)
        {
            Pin();
        }
    }



    [RelayCommand]
    public void AutoSearchGameBizs()
    {
        try
        {
            var service = AppConfig.GetService<GameLauncherService>();
            var sb = new StringBuilder();
            foreach (GameBiz biz in GameBiz.AllGameBizs)
            {
                if (service.IsGameExeExists(biz))
                {
                    sb.Append(biz.ToString());
                    sb.Append(',');
                }
            }
            AppConfig.SelectedGameBizs = sb.ToString().TrimEnd(',');
            InitializeGameBiz(CurrentGameBiz);
        }
        catch (Exception ex)
        {

        }
    }


    [RelayCommand]
    private void OpenGameBizSelector()
    {
        Border_GameBizSelector.Translation = Vector3.Zero;
        Border_FullMask.IsHitTestVisible = true;
        Border_FullMask.Opacity = 1;
        UpdateDragRectangles();
        if (IsPinned || GameBizIcons.Count == 0)
        {
            Popup_GameBizSelector.IsOpen = true;
        }
    }





    private void Button_GameBizSelector_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
        {
            if (CurrentGameBizIcon is not null)
            {
                CurrentGameBizIcon.CurrentGameBiz = false;
                CurrentGameBizIcon.MaskOpacity = 1;
            }

            CurrentGameBizIcon = icon;
            CurrentGameBiz = icon.GameBiz;
            icon.CurrentGameBiz = true;
            icon.MaskOpacity = 0;

            Border_FullMask.Opacity = 0;
            Border_FullMask.IsHitTestVisible = false;
            Popup_GameBizSelector.IsOpen = false;
            if (!IsPinned)
            {
                Border_GameBizSelector.Translation = new Vector3(0, -100, 0);
                UpdateDragRectangles();
            }

            GameBizChanged?.Invoke(this, (icon.GameBiz, false));
        }
    }



    private void Button_GameBizSelector_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
        {
            if (CurrentGameBizIcon is not null)
            {
                CurrentGameBizIcon.CurrentGameBiz = false;
                CurrentGameBizIcon.MaskOpacity = 1;
            }

            CurrentGameBizIcon = icon;
            CurrentGameBiz = icon.GameBiz;
            icon.CurrentGameBiz = true;
            icon.MaskOpacity = 0;

            Border_FullMask.Opacity = 0;
            Border_FullMask.IsHitTestVisible = false;
            Popup_GameBizSelector.IsOpen = false;
            if (!IsPinned)
            {
                Border_GameBizSelector.Translation = new Vector3(0, -100, 0);
                UpdateDragRectangles();
            }

            GameBizChanged?.Invoke(this, (icon.GameBiz, true));
        }
    }


    private void Button_GameBizSelector_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
        {
            if (!icon.CurrentGameBiz)
            {
                icon.MaskOpacity = 0;
            }
        }
    }


    private void Button_GameBizSelector_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Button button && button.DataContext is GameBizIcon icon)
        {
            if (!icon.CurrentGameBiz)
            {
                icon.MaskOpacity = 1;
            }
        }
    }



    private void Border_FullMask_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        Border_FullMask.Opacity = 0;
        Border_FullMask.IsHitTestVisible = false;
        Popup_GameBizSelector.IsOpen = false;
        if (!IsPinned)
        {
            Border_GameBizSelector.Translation = new Vector3(0, -100, 0);
            UpdateDragRectangles();
        }
    }



    private void ToggleSwitch_GameBizSelector_Toggled(object sender, RoutedEventArgs e)
    {
        if (sender is ToggleSwitch toggle && GameBiz.TryParse(toggle.Tag as string, out GameBiz biz))
        {
            if (GameBizIcons.FirstOrDefault(x => x.GameBiz == biz) is GameBizIcon icon)
            {
                GameBizIcons.Remove(icon);
            }
            if (toggle.IsOn)
            {
                if (CurrentGameBiz == biz)
                {
                    GameBizIcons.Add(new GameBizIcon { GameBiz = biz, CurrentGameBiz = true, MaskOpacity = 0 });
                }
                else
                {
                    GameBizIcons.Add(new GameBizIcon { GameBiz = biz });
                }
            }
            AppConfig.SelectedGameBizs = string.Join(',', GameBizIcons.Select(x => x.GameBiz.ToString()));
        }
    }



    [RelayCommand]
    private void OpenGameBizPopup()
    {
        Popup_GameBizSelector.IsOpen = !Popup_GameBizSelector.IsOpen;
    }



    [RelayCommand]
    private void Pin()
    {
        IsPinned = !IsPinned;
        if (IsPinned)
        {
            FontIcon_Pin.Glyph = IconUnpin;
            Border_GameBizSelector.Translation = Vector3.Zero;
            if (!Popup_GameBizSelector.IsOpen)
            {
                Border_FullMask.Opacity = 0;
                Border_FullMask.IsHitTestVisible = false;
            }
        }
        else
        {
            FontIcon_Pin.Glyph = IconPin;
            if (!Popup_GameBizSelector.IsOpen)
            {
                Border_FullMask.Opacity = 0;
                Border_FullMask.IsHitTestVisible = false;
                Border_GameBizSelector.Translation = new Vector3(0, -100, 0);
            }
            var temp = CurrentGameBizIcon;
            CurrentGameBizIcon = null;
            CurrentGameBizIcon = temp;
        }
        AppConfig.IsGameBizSelectorPinned = IsPinned;
    }



    private void Border_GameBizSelector_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }



    public void UpdateDragRectangles()
    {
        try
        {
            var scale = MainWindow.Current.UIScale;
            int x = (int)(56 * scale);
            if (Border_GameBizSelector.Translation == Vector3.Zero)
            {
                x = (int)((68 + Border_GameBizSelector.ActualWidth) * scale);
            }
            int height = (int)(48 * scale);
            var rect = new RectInt32(x, 0, 10000, height);
            MainWindow.Current.SetDragRectangles(rect);
        }
        catch { }
    }



    public void OnLanguageChanged(object? sender, LanguageChangedMessage message)
    {
        if (message.Completed)
        {
            this.Bindings.Update();
        }
    }

}
