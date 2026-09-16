using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using System;
using System.Collections.Generic;
using System.Globalization;
using Windows.Foundation;

namespace Starward.Features.PlayTime;

/// <summary>
/// 日历热力图（Canvas 精确布局）：
/// 列 = 一周（周一到周日，填满 7 行后换列），左侧星期标签列、底部月份标签列
/// （月份文字锚定到该月 1 日方块所在列、与方块左缘对齐，布局完成后按方块真实位置定位，
/// 任意 DPI 下零误差）。
/// 布局参数：方块大小 <see cref="CellSize" />（默认 NaN = 按可用宽度自动计算）
/// 与方块间距 <see cref="CellGap" />（默认 2）。
/// </summary>
public sealed class CalendarHeatmap : Grid
{


    /// <summary>
    /// 方块大小（像素）。默认 NaN = 自动（按实际可用宽度算出：每列一周恰好铺满）。
    /// 设定任意正数后固定使用。
    /// </summary>
    public double CellSize { get; set; } = double.NaN;


    /// <summary>方块四周的间距（像素），修改后立即生效。</summary>
    public double CellGap { get; set; } = 1.0;


    /// <summary>
    /// 色阶上限：数值达到该值时显示最深的颜色。为 null 时取数据最大值。
    /// </summary>
    public double? ScaleMax { get; set; }

    /// <summary>
    /// 是否在月份边界处显示分割线。默认 false = 不显示。
    /// </summary>
    public bool ShowMonthSplit { get; set; }

    private const double WeekdayLabelRightMargin = 6;

    private List<HeatmapDayItem>? _days;
    private bool _rebuildPending;
    private bool _layoutPending;
    private double _cellSize;
    private double _pitch;
    private double _lastAutoWidth;
    private double _lastLabelWidthUsed;
    private int _reconcileCount;
    private Brush[] _levelBrushes = Array.Empty<Brush>();
    private readonly List<FrameworkElement> _itemSlots = new();
    private readonly List<(int FirstK, TextBlock Label)> _monthLabels = new();
    private readonly List<(int LeftK, int RightK)> _monthBoundaries = new();
    private readonly List<Polyline> _monthPolyLines = new();

    private readonly Canvas _plate = new();
    private readonly Canvas _monthLayer = new() { Height = 18 };
    private readonly Canvas _dividerLayer = new() { IsHitTestVisible = false };
    private readonly Grid _weekdayColumn = new();
    private readonly HoverCard _hoverCard = new();


    /// <summary>
    /// 可用宽度：优先使用实际渲染宽度 <see cref="FrameworkElement.ActualWidth"/>，
    /// 未布局时使用显式设置的 <see cref="FrameworkElement.Width"/>。
    /// </summary>
    private double AvailablePixelWidth => ActualWidth > 0 ? ActualWidth : Width > 0 ? Width : 0;


    private bool HasLayoutWidth() => AvailablePixelWidth > 0;


    /// <summary>
    /// 数据项（按日期升序），赋值后立即重建图表（控件尚未布局时会在布局完成后重建）
    /// </summary>
    public List<HeatmapDayItem>? Days
    {
        get => _days;
        set
        {
            _days = value;
            if (HasLayoutWidth())
            {
                Rebuild();
            }
            else
            {
                _rebuildPending = true;
            }
        }
    }


    public CalendarHeatmap()
    {
        RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        RowDefinitions.Add(new RowDefinition { Height = new GridLength(18) });
        ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        SetRow(_monthLayer, 1);
        SetColumn(_monthLayer, 1);
        Children.Add(_monthLayer);

        SetRow(_weekdayColumn, 0);
        SetColumn(_weekdayColumn, 0);
        Children.Add(_weekdayColumn);

        // 方块层：直接用 Canvas 按 (列 × pitch, 行 × pitch) 精确摆放，不用 ItemsControl/ItemsWrapGrid——
        // 后者的容器测量与格距取整行为不可控（正是“最后一列被裁”的根源），
        // Canvas 摆位与格距数学完全一致，余量由 ComputeMetrics 的向下取整保证。
        SetRow(_plate, 0);
        SetColumn(_plate, 1);
        Children.Add(_plate);

        SetRow(_dividerLayer, 0);
        SetColumn(_dividerLayer, 1);
        Children.Add(_dividerLayer);

        // 悬浮卡需覆盖整个根网格，其坐标系原点 = 根网格原点，hover 位置才能正确跟随方块
        SetRow(_hoverCard, 0);
        SetColumn(_hoverCard, 0);
        SetRowSpan(_hoverCard, 2);
        SetColumnSpan(_hoverCard, 2);
        Children.Add(_hoverCard);

        LayoutUpdated += OnLayoutUpdated;
        SizeChanged += OnSizeChanged;
    }


    private void OnLayoutUpdated(object? sender, object e)
    {
        // 标签列以“真实布局后的 Auto 列宽”为唯一权威：只要它与上次计算用的宽度不符，
        // 就用真实宽度重建（每次布局后都校验，直至收敛）。字体解析、Margin 计入方式、
        // DPI 列宽吸附等一切未知差异都会在这一步被矫正，不再依赖任何文本测量的估算值。
        if (_days is { Count: > 0 })
        {
            double realLabelWidth = _weekdayColumn.ActualWidth;
            if (realLabelWidth > 0 && Math.Abs(realLabelWidth - _lastLabelWidthUsed) > 0.01)
            {
                if (_reconcileCount < 3)
                {
                    _reconcileCount++;
                    Rebuild();
                    return;
                }
            }
            else
            {
                _reconcileCount = 0;
            }
        }
        if (_layoutPending && CanPlaceOverlays())
        {
            _layoutPending = false;
            PlaceMonthOverlays();
        }
        if (_rebuildPending && HasLayoutWidth())
        {
            _rebuildPending = false;
            Rebuild();
        }
    }


    private bool CanPlaceOverlays()
    {
        if (_plate.ActualWidth <= 0)
        {
            return false;
        }
        // 需要：第 0 槽（col0 行0）、第 1 槽（col0 行1）、第 7 槽（col1 行0）都已布局
        return _itemSlots.Count > 7
            && _itemSlots[0].ActualWidth > 0
            && _itemSlots[1].ActualWidth > 0
            && _itemSlots[7].ActualWidth > 0;
    }


    private void OnSizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (_days is { Count: > 0 } && double.IsNaN(CellSize) && HasLayoutWidth()
            && Math.Abs(ActualWidth - _lastAutoWidth) > 0.5)
        {
            Rebuild();
        }
    }


    private void Rebuild()
    {
        // 释放上一轮方格的悬浮绑定（每个方格一条，_hoverCard 是本控件复用的长生命周期实例）
        _hoverCard.Clear();
        _plate.Children.Clear();
        _monthLayer.Children.Clear();
        _dividerLayer.Children.Clear();
        _monthLabels.Clear();
        _monthBoundaries.Clear();
        _monthPolyLines.Clear();
        _weekdayColumn.Children.Clear();
        _weekdayColumn.RowDefinitions.Clear();

        var days = _days;
        if (days is null || days.Count == 0)
        {
            return;
        }

        var start = days[0].Date;
        int pad = ((int)start.DayOfWeek + 6) % 7; // 周一起始的占位格数
        int total = pad + days.Count;
        int columnCount = (int)Math.Ceiling(total / 7.0);

        // 标签列宽度：优先使用上一次真实布局后的 Auto 列宽（这正是根 Grid 实际分给星列的扣除值）。
        // 仅首次构建（尚无布局历史）用 24px 通用值兜底——它只影响首个可能来不及渲染的中间帧，
        // 随后 OnLayoutUpdated 的 reconcile 会立刻用真实宽度重建并收敛。
        double labelColumnWidth = _weekdayColumn.ActualWidth > 0 ? _weekdayColumn.ActualWidth : 24;
        _lastLabelWidthUsed = labelColumnWidth;
        double avail = AvailablePixelWidth;
        ComputeMetrics(columnCount, Math.Max(0, avail - labelColumnWidth - 1));
        double cellPitch = _pitch;

        // 左侧星期标签：行高与方块行高（pitch）一致，天然逐行对齐；只显示周一与周日
        var monday = new DateTime(2024, 1, 1); // 周一
        for (int r = 0; r < 7; r++)
        {
            _weekdayColumn.RowDefinitions.Add(new RowDefinition { Height = new GridLength(cellPitch) });
            if (r is > 0 and < 6)
            {
                continue;
            }
            var label = new TextBlock
            {
                Text = monday.AddDays(r).ToString("ddd", CultureInfo.CurrentUICulture),
                FontSize = 10,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, WeekdayLabelRightMargin, 0),
                Foreground = ChartHelpers.GetResource<Brush>("TextFillColorSecondaryBrush"),
                IsTextScaleFactorEnabled = false,
            };
            Grid.SetRow(label, r);
            _weekdayColumn.Children.Add(label);
        }

        // 占位格 + 方块；月份标签锚定到该月 1 日方块所在列（布局后定位）
        double scaleMax = ScaleMax ?? ComputeScaleMax(days);
        EnsureLevelBrushes();
        Brush transparent = new SolidColorBrush(Windows.UI.Color.FromArgb(0, 0, 0, 0));
        _itemSlots.Clear();
        _itemSlots.Capacity = total;

        for (int i = 0; i < pad; i++)
        {
            _itemSlots.Add(CreateSlot(null, scaleMax, transparent));
        }

        int currentMonth = -1;
        int lastK = -1;
        int dayIndex = 0;
        foreach (var day in days)
        {
            int k = pad + dayIndex;
            int month = day.Date.Year * 12 + day.Date.Month;
            if (month != currentMonth)
            {
                currentMonth = month;
                // 只有该月 1 日确实存在方块时才标注月份。
                // 首个不完整的月份（数据从月中开始）压根没有 1 日，若退化成按首个可见日标注，
                // 文字就会落在那一列的真实起始日上而非 1 日，属于错位，因此直接不标注。
                if (day.Date.Day == 1)
                {
                    var label = new TextBlock
                    {
                        Text = day.Date.ToDateTime(TimeOnly.MinValue).ToString("MMM", CultureInfo.CurrentUICulture),
                        TextAlignment = TextAlignment.Left,
                        FontSize = 11,
                        Foreground = ChartHelpers.GetResource<Brush>("TextFillColorSecondaryBrush"),
                        IsTextScaleFactorEnabled = false,
                    };
                    _monthLayer.Children.Add(label);
                    _monthLabels.Add((k, label));
                }
                if (ShowMonthSplit && lastK >= 0)
                {
                    // 月边界折线：上段沿新月首列左缝，在月首格顶部行缝折向月末列缝，再向下贯通
                    var polyline = new Polyline
                    {
                        Stroke = ChartHelpers.GetResource<Brush>("ControlStrongStrokeColorDefaultBrush"),
                        StrokeThickness = 1,
                        Opacity = 0.45,
                        StrokeLineJoin = PenLineJoin.Round,
                        IsHitTestVisible = false,
                        Points = new PointCollection { new(0, 0), new(0, 0), new(0, 0), new(0, 0) },
                    };
                    _dividerLayer.Children.Add(polyline);
                    _monthBoundaries.Add((lastK, k));
                    _monthPolyLines.Add(polyline);
                }
            }
            var daySlot = CreateSlot(day, scaleMax, transparent);
            lastK = k;
            dayIndex++;
            _itemSlots.Add(daySlot);
        }

        // 按 (列 × pitch, 行 × pitch) 精确摆放所有格位（下标 i = 列 × 7 + 行）
        for (int i = 0; i < _itemSlots.Count; i++)
        {
            var slot = _itemSlots[i];
            Canvas.SetLeft(slot, (i / 7) * _pitch);
            Canvas.SetTop(slot, (i % 7) * _pitch);
            _plate.Children.Add(slot);
        }
        if (CanPlaceOverlays())
        {
            PlaceMonthOverlays();
        }
        else
        {
            _layoutPending = true;
        }
    }


    /// <summary>
    /// 一个格位：外层槽（尺寸 = pitch）与内部方块（尺寸 = cellSize、圆形圆角、居中），
    /// 槽之间无空隙、块与块之间保留 2 × CellGap 的视觉间距。
    /// </summary>
    private FrameworkElement CreateSlot(HeatmapDayItem? day, double scaleMax, Brush transparent)
    {
        var slot = new Grid
        {
            Width = _pitch,
            Height = _pitch,
            VerticalAlignment = VerticalAlignment.Top,
        };
        var border = new Border
        {
            Width = _cellSize,
            Height = _cellSize,
            CornerRadius = new CornerRadius(2),
            Background = day is { } d ? LevelBrush(d.Value, scaleMax) ?? transparent : transparent,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };
        slot.Children.Add(border);
        if (day is { } d2 && !string.IsNullOrEmpty(d2.Tooltip))
        {
            _hoverCard.Bind(border, () => d2.Tooltip);
        }
        return slot;
    }


    /// <summary>
    /// 计算格距与方块尺寸：格距 = 可用宽度（扣除星期标签列）÷ 周列数（铺满），
    /// 方块大小 = 格距 - 2 × <see cref="CellGap" />；显式 <see cref="CellSize" /> 时固定使用。
    /// </summary>
    private void ComputeMetrics(int columnCount, double itemWidth)
    {
        if (double.IsNaN(CellSize))
        {
            if (itemWidth > 0 && columnCount > 0)
            {
                // 向下取整保证列总宽不超过可用宽度（Round 可能向上进位导致最后一列溢出被裁切）
                double pitch = Math.Clamp(Math.Floor(itemWidth / columnCount * 100) / 100, 8, 44);
                _cellSize = Math.Max(2, pitch - CellGap * 2);
                _pitch = _cellSize + CellGap * 2;
            }
            else
            {
                _cellSize = 14;
                _pitch = _cellSize + CellGap * 2;
            }
        }
        else
        {
            _cellSize = Math.Max(2, CellSize);
            _pitch = _cellSize + CellGap * 2;
        }
        _lastAutoWidth = ActualWidth;
    }


    /// <summary>
    /// 用方格层的实测槽位做线性校准后摆放月份标签与月边界折线：
    /// 列宽 = 第 2 列槽与原点的实测距离，行高 = 下一行槽的实测距离，
    /// 因此与方格真实布局完全一致（消除任何逐列累积误差）。
    /// 月份标签：锚定到该月 1 日方块所在列，文字左缘与方块左缘对齐。
    /// 只为该月 1 日确实存在于数据中的月份建标签，因此首个不完整的月份（数据从月中开始）没有标签。
    /// 折线：月首日非周一 → 上段沿月首列右缝到月首格顶、横折、下段沿月首列左缝贯通；
    /// 月首日周一 → 纯竖直线。缝中心 = 像素格线中心。
    /// </summary>
    private void PlaceMonthOverlays()
    {
        var p0 = _itemSlots[0].TransformToVisual(_dividerLayer).TransformPoint(new Point(0, 0));
        var p1 = _itemSlots[1].TransformToVisual(_dividerLayer).TransformPoint(new Point(0, 0));
        var p7 = _itemSlots[7].TransformToVisual(_dividerLayer).TransformPoint(new Point(0, 0));
        double baseX = p0.X;
        double baseY = p0.Y;
        double colW = p7.X - p0.X;
        double rowH = p1.Y - p0.Y;
        if (colW <= 0 || rowH <= 0)
        {
            colW = _pitch;
            rowH = _pitch;
        }
        double height = baseY + 7 * rowH;

        foreach (var (firstK, label) in _monthLabels)
        {
            // 锚定到该月 1 日方块所在列，文字左缘 = 方块左缘（槽左缘 + CellGap）
            int colA = firstK / 7;
            double left = Math.Floor(baseX + colA * colW) + CellGap;
            Canvas.SetLeft(label, left);
            Canvas.SetTop(label, 2); // 底部月份层（高 18px）内垂直居中
        }
        for (int i = 0; i < _monthBoundaries.Count; i++)
        {
            var (_, rightK) = _monthBoundaries[i];
            int colR = rightK / 7;
            int rowR = rightK % 7;
            double xLeft = Math.Floor(baseX + colR * colW) + 0.5;
            if (rowR == 0)
            {
                _monthPolyLines[i].Points = new PointCollection
                {
                    new(xLeft, 0),
                    new(xLeft, height),
                    new(xLeft, height),
                    new(xLeft, height),
                };
            }
            else
            {
                double xRight = Math.Floor(baseX + (colR + 1) * colW) + 0.5;
                double jogY = Math.Floor(baseY + rowR * rowH) + 0.5;
                _monthPolyLines[i].Points = new PointCollection
                {
                    new(xRight, 0),
                    new(xRight, jogY),
                    new(xLeft, jogY),
                    new(xLeft, height),
                };
            }
        }
    }


    private static double ComputeScaleMax(List<HeatmapDayItem> days)
    {
        double max = 0;
        foreach (var d in days)
        {
            max = Math.Max(max, d.Value);
        }
        return max > 0 ? max : 1;
    }


    /// <summary>
    /// 解析 8 级色阶画刷：第 0 级为无数据（卡片背景色）；
    /// 第 1~7 级用 WinUI 强调色系，由暗（Dark3）到亮（Light3）—— 暗色代表时长低、亮色代表时长高，随系统强调色自动变化。
    /// </summary>
    private void EnsureLevelBrushes()
    {
        if (_levelBrushes.Length == 8)
        {
            return;
        }
        bool dark = Application.Current.RequestedTheme == ApplicationTheme.Dark;
        _levelBrushes = new Brush[8]
        {
            ChartHelpers.GetResource<Brush>("CardBackgroundFillColorDefaultBrush"),
            LevelColorBrush("SystemAccentColorDark3", dark ? "#0A331E" : "#0F3A20"),
            LevelColorBrush("SystemAccentColorDark2", dark ? "#0E4429" : "#154F27"),
            LevelColorBrush("SystemAccentColorDark1", dark ? "#125B27" : "#1B5E32"),
            LevelColorBrush("SystemAccentColor", dark ? "#187632" : "#216E39"),
            LevelColorBrush("SystemAccentColorLight1", dark ? "#1F8B3B" : "#30A14E"),
            LevelColorBrush("SystemAccentColorLight2", dark ? "#26A641" : "#40C463"),
            LevelColorBrush("SystemAccentColorLight3", dark ? "#39D353" : "#9BE9A8"),
        };
    }


    private static SolidColorBrush LevelColorBrush(string colorResourceKey, string fallbackHex)
    {
        if (Application.Current.Resources.TryGetValue(colorResourceKey, out var value) && value is Windows.UI.Color color)
        {
            return new SolidColorBrush(color);
        }
        return new SolidColorBrush(ParseHex(fallbackHex));
    }


    /// <summary>
    /// 数值映射为色阶画刷；负数返回 null（占位透明），0 为无数据底色。
    /// 色阶分段采用线性增长的窗口：第 k 级覆盖的时长 = k × w（w = scaleMax / 28），
    /// 即每个 level 包含的时间长度相对上一个 level 线性增长，7 级总界恰好达到 scaleMax。
    /// </summary>
    public Brush? LevelBrush(double value, double scaleMax)
    {
        EnsureLevelBrushes();
        if (value < 0)
        {
            return null;
        }
        double ceiling = scaleMax <= 0 ? 1 : scaleMax;
        int level = 0;
        if (value > 0)
        {
            level = 7;
            for (int k = 1; k <= 7; k++)
            {
                if (value <= ceiling * k * (k + 1) / 56.0)
                {
                    level = k;
                    break;
                }
            }
        }
        return _levelBrushes[level];
    }


    private static Windows.UI.Color ParseHex(string hex)
    {
        hex = hex.TrimStart('#');
        return Windows.UI.Color.FromArgb(
            255,
            Convert.ToByte(hex.Substring(0, 2), 16),
            Convert.ToByte(hex.Substring(2, 2), 16),
            Convert.ToByte(hex.Substring(4, 2), 16));
    }

}
