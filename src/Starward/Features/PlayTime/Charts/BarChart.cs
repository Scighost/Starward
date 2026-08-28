using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using Windows.Foundation;

namespace Starward.Features.PlayTime;

/// <summary>
/// 通用柱状图：纵轴刻度与网格线随数据自适应（5 等分），圆角柱体，悬停显示提示卡。
/// </summary>
public sealed class BarChart : Grid
{


    private List<BarChartItem>? _items;

    /// <summary>
    /// 数据项，赋值后立即重建图表
    /// </summary>
    public List<BarChartItem>? Items
    {
        get => _items;
        set
        {
            _items = value;
            Rebuild();
        }
    }


    /// <summary>
    /// 绘图区高度（像素）
    /// </summary>
    public double PlotHeight { get; set; } = 180;


    /// <summary>
    /// 柱子最大宽度（像素）：柱宽 = 列宽 - 10，且不超过该值
    /// </summary>
    public double MaxBarWidth { get; set; } = 38;


    private readonly HoverCard _hoverCard = new() { AlwaysAbove = true };



    public BarChart()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
    }



    private void Rebuild()
    {
        Children.Clear();
        RowDefinitions.Clear();
        ColumnDefinitions.Clear();
        // 释放上一轮柱体的悬浮绑定（_hoverCard 是本控件复用的长生命周期实例）
        _hoverCard.Clear();

        var items = _items;
        if (items is null || items.Count == 0)
        {
            return;
        }

        RowDefinitions.Add(new RowDefinition { Height = new GridLength(PlotHeight) });
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        double max = 0;
        foreach (var item in items)
        {
            max = Math.Max(max, item.Value);
        }
        double axisMax = Math.Max(60, Math.Ceiling(max / 60.0) * 60);
        Brush secondary = ChartHelpers.GetResource<Brush>("TextFillColorSecondaryBrush");
        Style? caption = Application.Current.Resources.TryGetValue("CaptionTextBlockStyle", out var v) ? v as Style : null;

        // 纵轴标签列宽适配最长文本（如 "226h"），避免换行
        string[] labelTexts = new string[3];
        for (int i = 0; i < 3; i++)
        {
            labelTexts[i] = FormatHours(axisMax * i / 2.0);
        }
        double labelWidth = 0;
        foreach (var text in labelTexts)
        {
            var probe = new TextBlock { Text = text, Style = caption };
            probe.Measure(new Size(double.PositiveInfinity, 0));
            labelWidth = Math.Max(labelWidth, probe.DesiredSize.Width);
        }
        double labelBoxWidth = Math.Ceiling(labelWidth) + 2;
        ColumnDefinitions[0] = new ColumnDefinition { Width = new GridLength(labelBoxWidth + 4) };

        // 纵轴网格线与刻度（0、中间值、最大值），线宽覆盖绘图区、文本垂直中心与线重合
        var axisLabels = new Canvas();
        Grid.SetRow(axisLabels, 0);
        Grid.SetColumn(axisLabels, 0);
        var axisLines = new Grid();
        Grid.SetRow(axisLines, 0);
        Grid.SetColumn(axisLines, 1);
        const int ticks = 3;
        for (int i = 0; i <= ticks - 1; i++)
        {
            double f = (double)i / (ticks - 1);
            double lineY = PlotHeight * (1 - f);
            var label = new TextBlock
            {
                Text = labelTexts[i],
                Width = labelBoxWidth,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                Style = caption,
                Foreground = secondary,
            };
            Canvas.SetLeft(label, 2);
            Canvas.SetTop(label, lineY - 7);
            axisLabels.Children.Add(label);
            axisLines.Children.Add(new Rectangle
            {
                Height = 1,
                VerticalAlignment = VerticalAlignment.Top,
                Margin = new Thickness(0, Math.Clamp(lineY - 0.5, 0, PlotHeight - 1), 0, 0),
                Fill = ChartHelpers.GetResource<Brush>("DividerStrokeColorDefaultBrush"),
                Opacity = 0.6,
            });
        }
        Children.Add(axisLabels);
        Children.Add(axisLines);

        // 柱体与横轴标签：柱宽 = 列宽 - 10，不超过 56px（列少时避免过粗）
        double columnWidth = ActualWidth > 0
            ? Math.Max(12, (ActualWidth - labelBoxWidth - 4) / items.Count - 10)
            : 28;
        double barWidth = Math.Min(columnWidth, MaxBarWidth);
        Brush accent = ChartHelpers.GetResource<Brush>("AccentFillColorDefaultBrush");
        var barsHost = new Grid { VerticalAlignment = VerticalAlignment.Bottom };
        var labelsHost = new Grid();

        // 悬停高亮：贯穿整个绘图区（0h ~ 纵轴最大值）的背景矩形，位于柱体后面。
        var hoverHighlight = new Rectangle
        {
            Width = barWidth,
            Height = PlotHeight,
            Fill = ChartHelpers.GetResource<Brush>("CardBackgroundFillColorDefaultBrush"),
            RadiusX = 4,
            RadiusY = 4,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Top,
            IsHitTestVisible = false,
            Visibility = Visibility.Collapsed,
        };
        hoverHighlight.SetValue(Grid.ColumnProperty, 0);
        barsHost.Children.Add(hoverHighlight);

        foreach (var item in items)
        {
            barsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            labelsHost.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            int index = barsHost.ColumnDefinitions.Count - 1;

            var bar = new Border
            {
                Width = barWidth,
                Height = item.Value > 0 ? Math.Clamp(item.Value / axisMax * PlotHeight, 3, PlotHeight) : 0,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom,
                CornerRadius = new CornerRadius(4, 4, 0, 0),
                Background = accent,
            };
            bar.SetValue(Grid.ColumnProperty, index);
            barsHost.Children.Add(bar);
            if (!string.IsNullOrEmpty(item.Tooltip))
            {
                // 透明的命中区域：宽度与柱体相同、高度覆盖整个绘图区。
                // 即使柱体很矮，只要鼠标在它所在的整列范围内即可显示提示。
                var hit = new Rectangle
                {
                    Width = barWidth,
                    Height = PlotHeight,
                    Fill = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Top,
                };
                hit.SetValue(Grid.ColumnProperty, index);
                barsHost.Children.Add(hit);
                _hoverCard.Bind(hit, () => item.Tooltip);
                hit.PointerEntered += (_, _) =>
                {
                    hoverHighlight.SetValue(Grid.ColumnProperty, index);
                    hoverHighlight.Visibility = Visibility.Visible;
                };
                hit.PointerExited += (_, _) => hoverHighlight.Visibility = Visibility.Collapsed;
            }

            var label = new TextBlock
            {
                Text = item.Label,
                FontSize = 10,
                TextAlignment = TextAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 6, 0, 0),
                Foreground = secondary,
            };
            label.SetValue(Grid.ColumnProperty, index);
            labelsHost.Children.Add(label);
        }
        barsHost.SetValue(Grid.RowProperty, 0);
        barsHost.SetValue(Grid.ColumnProperty, 1);
        labelsHost.SetValue(Grid.RowProperty, 1);
        labelsHost.SetValue(Grid.ColumnProperty, 1);
        Children.Add(barsHost);
        Children.Add(labelsHost);

        // 悬浮提示层，覆盖整个图表区域
        SetRowSpan(_hoverCard, 2);
        SetColumnSpan(_hoverCard, 2);
        Children.Add(_hoverCard);
    }


    /// <summary>
    /// 把分钟数格式化为小时文本（如 0h、1.5h、3h）
    /// </summary>
    private static string FormatHours(double minutes)
    {
        double hours = minutes / 60.0;
        return $"{hours:0.#}h";
    }

}
