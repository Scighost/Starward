using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Input;
using Starward.Core;
using System;
using System.Numerics;
using System.Windows.Input;

namespace Starward.Controls.TitleBarGameIcon;

[INotifyPropertyChanged]
public abstract partial class TitleBarGameIconBase : UserControl
{



    protected readonly Compositor compositor;


    public abstract GameBiz GameBiz { get; protected init; }


    public TitleBarGameIconBase()
    {
        compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        this.Loaded += (_, _) => UpdateCornerRadius(false);
    }



    public ICommand Command
    {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(TitleBarGameIconBase), new PropertyMetadata(default));


    public void Select(GameBiz biz)
    {
        IsSelected = biz.ToGame() == GameBiz;
    }


    protected bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            UpdateCornerRadius(value);
            BorderMaskOpacity = value ? 0 : 1;
        }
    }


    [ObservableProperty]
    protected double borderMaskOpacity = 1;


    protected bool isTapped;




    [RelayCommand]
    protected void Click(GameBiz biz)
    {
        IsSelected = true;
        Command?.Execute(biz);
    }


    protected void Button_Click(object sender, RoutedEventArgs e)
    {
        if (!IsSelected)
        {
            Click(GameBiz);
        }
    }


    protected void Button_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        Click(GameBiz);
    }

    protected void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateCornerRadius(true);
    }

    protected void Button_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!isTapped)
        {
            UpdateCornerRadius(isSelected);
        }
    }

    protected void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        isTapped = true;
    }

    protected void MenuFlyout_Closed(object sender, object e)
    {
        isTapped = false;
        UpdateCornerRadius(isSelected);
    }



    protected void UpdateCornerRadius(bool isSelect)
    {
        var visual = ElementCompositionPreview.GetElementVisual(this);
        CompositionRoundedRectangleGeometry geometry;
        if (visual.Clip is CompositionGeometricClip clip && clip.Geometry is CompositionRoundedRectangleGeometry geo)
        {
            geometry = geo;
            geometry.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
        }
        else
        {
            geometry = compositor.CreateRoundedRectangleGeometry();
            geometry.Size = new Vector2((float)ActualWidth, (float)ActualHeight);
            geometry.CornerRadius = Vector2.Zero;
            clip = compositor.CreateGeometricClip(geometry);
            visual.Clip = clip;
        }
        var animation = compositor.CreateVector2KeyFrameAnimation();
        animation.Duration = TimeSpan.FromSeconds(0.3);
        if (isSelect)
        {
            animation.InsertKeyFrame(1, new Vector2(8, 8));
        }
        else
        {
            animation.InsertKeyFrame(1, new Vector2((float)ActualWidth / 2, (float)ActualHeight / 2));
        }
        geometry.StartAnimation(nameof(CompositionRoundedRectangleGeometry.CornerRadius), animation);
    }




}
