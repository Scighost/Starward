using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Starward.Features.PlayTime;

/// <summary>
/// 图表共享的自绘悬浮提示卡：鼠标移入数据元素即在其上方显示，无系统 ToolTip 的延迟。
/// 需作为覆盖整个图表区域的最后一层子元素加入可视化树。
/// </summary>
public sealed class HoverCard : Canvas
{


    private readonly Border _card = new()
    {
        Padding = new Thickness(12, 6, 12, 6),
        Background = ChartHelpers.GetResource<Brush>("CustomOverlayAcrylicBrush"),
        BorderThickness = new Thickness(0),
        CornerRadius = new CornerRadius(4),
    };

    private readonly TextBlock _text = new()
    {
        TextAlignment = TextAlignment.Center,
        FontSize = 12,
    };

    private readonly Dictionary<FrameworkElement, Func<string?>> _bindings = new();

    private const double HoverGap = 8.0;


    /// <summary>
    /// 为 true 时提示卡总是出现在锚定元素的顶部（不再因空间不足翻转到底部）。
    /// </summary>
    public bool AlwaysAbove { get; set; }



    public HoverCard()
    {
        _card.Child = _text;
        Children.Add(_card);
        IsHitTestVisible = false;
        Visibility = Visibility.Collapsed;
    }



    /// <summary>
    /// 让一个数据元素在指针进入时显示悬浮卡，离开时隐藏
    /// </summary>
    public void Bind(FrameworkElement element, Func<string?> textProvider)
    {
        if (_bindings.TryAdd(element, textProvider))
        {
            element.PointerEntered += OnElementPointerEntered;
            element.PointerExited += OnElementPointerExited;
        }
        else
        {
            _bindings[element] = textProvider;
        }
    }


    /// <summary>
    /// 解除所有绑定。宿主重建图表元素时必须调用，
    /// 否则 <see cref="_bindings"/> 会一直强引用已被移出可视化树的元素及其闭包。
    /// </summary>
    public void Clear()
    {
        foreach (var element in _bindings.Keys)
        {
            element.PointerEntered -= OnElementPointerEntered;
            element.PointerExited -= OnElementPointerExited;
        }
        _bindings.Clear();
        Hide();
    }


    private void OnElementPointerEntered(object sender, PointerRoutedEventArgs e)
    {
        if (sender is FrameworkElement element && _bindings.TryGetValue(element, out var provider))
        {
            string? text = provider();
            if (!string.IsNullOrEmpty(text))
            {
                Show(element, text);
            }
        }
    }


    private void OnElementPointerExited(object sender, PointerRoutedEventArgs e)
    {
        Hide();
    }



    public void Show(FrameworkElement? anchor, string text)
    {
        if (anchor is null)
        {
            return;
        }
        _text.Text = text;
        _card.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

        var pos = anchor.TransformToVisual(this).TransformPoint(new Point(0, 0));
        double left = pos.X + anchor.ActualWidth / 2 - _card.DesiredSize.Width / 2;
        double top = AlwaysAbove
            ? Math.Max(2, pos.Y - _card.DesiredSize.Height - HoverGap)
            : pos.Y - _card.DesiredSize.Height - HoverGap;
        if (!AlwaysAbove && top < 0)
        {
            top = pos.Y + anchor.ActualHeight + HoverGap;
        }

        // 水平方向钳制在宿主区域内（首次显示宿主可能尚未布局、ActualWidth 为 0，此时跳过钳制而非拉回左上角）
        double hostW = ActualWidth > 0 ? ActualWidth : 0;
        if (hostW > 0)
        {
            left = Math.Clamp(left, 0, Math.Max(0, hostW - _card.DesiredSize.Width));
        }

        SetLeft(_card, left);
        SetTop(_card, Math.Max(0, top));
        Visibility = Visibility.Visible;
    }


    public void Hide()
    {
        Visibility = Visibility.Collapsed;
    }

}
