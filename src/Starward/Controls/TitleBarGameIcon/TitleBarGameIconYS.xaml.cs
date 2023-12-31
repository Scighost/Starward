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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls.TitleBarGameIcon;

public sealed partial class TitleBarGameIconYS : UserControl
{


    private readonly Compositor compositor;

    public GameBiz GameBiz { get; set; } = GameBiz.GenshinImpact;


    public TitleBarGameIconYS()
    {
        this.InitializeComponent();
        compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
        this.Loaded += (_, _) => UpdateCornerRadius(false);
    }



    public ICommand Command
    {
        get { return (ICommand)GetValue(CommandProperty); }
        set { SetValue(CommandProperty, value); }
    }

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register("Command", typeof(ICommand), typeof(TitleBarGameIconBH3), new PropertyMetadata(default));


    public void Select(GameBiz biz)
    {
        IsSelected = biz.ToGame() == GameBiz;
    }


    private bool isSelected;
    public bool IsSelected
    {
        get => isSelected;
        set
        {
            isSelected = value;
            UpdateCornerRadius(value);
            Border_Mask.Opacity = value ? 0 : 1;
        }
    }



    private bool isTapped;




    [RelayCommand]
    private void Click(GameBiz biz)
    {
        IsSelected = true;
        Command?.Execute(biz);
    }


    private void Button_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        Click(GameBiz);
    }

    private void Button_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        UpdateCornerRadius(true);
    }

    private void Button_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        if (!isTapped)
        {
            UpdateCornerRadius(isSelected);
        }
    }

    private void Button_RightTapped(object sender, RightTappedRoutedEventArgs e)
    {
        isTapped = true;
    }

    private void MenuFlyout_Closed(object sender, object e)
    {
        isTapped = false;
        UpdateCornerRadius(isSelected);
    }



    private void UpdateCornerRadius(bool isSelect)
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
