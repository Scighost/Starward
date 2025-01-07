using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Geometry;
using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using Starward.Core;
using Starward.Features.Database;
using Starward.Frameworks;
using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;


namespace Starward.Features.PlayTime;

[INotifyPropertyChanged]
public sealed partial class PlayTimeButton : UserControl
{


    public GameBiz CurrentGameBiz { get; set; }


    private readonly ILogger<PlayTimeButton> _logger = AppService.GetLogger<PlayTimeButton>();


    private readonly PlayTimeService _playTimeService = AppService.GetService<PlayTimeService>();



    public PlayTimeButton()
    {
        this.InitializeComponent();
    }





    public TimeSpan PlayTimeTotal { get; set => SetProperty(ref field, value); }


    public TimeSpan PlayTime7Days { get; set => SetProperty(ref field, value); }


    public TimeSpan PlayTimeLast { get; set => SetProperty(ref field, value); }


    public string LastPlayTimeText { get; set => SetProperty(ref field, value); }


    public int StartUpCount { get; set => SetProperty(ref field, value); }



    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        InitializePlayTime();
    }



    private void InitializePlayTime()
    {
        try
        {
            PlayTimeTotal = DatabaseService.GetValue<TimeSpan>($"playtime_total_{CurrentGameBiz}", out _);
            StartUpCount = DatabaseService.GetValue<int>($"startup_count_{CurrentGameBiz}", out _);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(CurrentGameBiz);
            if (time > DateTimeOffset.MinValue)
            {
                LastPlayTimeText = time.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize play time");
        }
    }



    [RelayCommand]
    private void UpdatePlayTime()
    {
        try
        {
            PlayTimeTotal = _playTimeService.GetPlayTimeTotal(CurrentGameBiz);
            StartUpCount = _playTimeService.GetStartUpCount(CurrentGameBiz);
            (var time, PlayTimeLast) = _playTimeService.GetLastPlayTime(CurrentGameBiz);
            if (time > DateTimeOffset.MinValue)
            {
                LastPlayTimeText = time.LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss");
            }
            DatabaseService.SetValue($"playtime_total_{CurrentGameBiz}", PlayTimeTotal);
            DatabaseService.SetValue($"startup_count_{CurrentGameBiz}", StartUpCount);

            CalculateRecent7Days();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update play time");
        }
    }





    TimeSpan[]? _recent7Days;



    private void CalculateRecent7Days()
    {
        try
        {
            _recent7Days = null;
            var now = DateTimeOffset.Now;
            var day = new DateTimeOffset(now.Date, now.Offset);
            var list = new TimeSpan[7];
            TimeSpan total = TimeSpan.Zero;
            for (int i = 0; i < 7; i++)
            {
                var start = day.AddDays(-i);
                var end = start.AddDays(1);
                TimeSpan playTime = _playTimeService.CalculatePlayTime(CurrentGameBiz, start, end);
                list[6 - i] = playTime;
                total += playTime;
            }
            PlayTime7Days = total;
            TimeSpan max = list.Max();
            if (max == TimeSpan.Zero)
            {
                Grid_TrendChart.Visibility = Visibility.Collapsed;
                return;
            }
            Grid_TrendChart.Visibility = Visibility.Visible;
            var visual = ElementCompositionPreview.GetElementVisual(Border_TrendChart);
            var compositor = visual.Compositor;
            // 防止不断刷新时宽度不停增加
            double width = Border_TrendChart.ActualWidth - 1;
            double height = Border_TrendChart.ActualHeight;
            Debug.WriteLine(width);

            var pathFig = new PathFigure();
            using var builder = new CanvasPathBuilder(CanvasDevice.GetSharedDevice());
            builder.BeginFigure(0, (float)height);
            float xStep = (float)(width / 6);
            for (int i = 0; i < list.Length; i++)
            {
                float y = (float)(height - list[i].TotalHours / max.TotalHours * height);
                float x = xStep * i;
                builder.AddLine(x, y);
                pathFig.Segments.Add(new LineSegment { Point = new Windows.Foundation.Point(x, y) });
                if (i == 0)
                {
                    pathFig.StartPoint = new Windows.Foundation.Point(x, y);
                }
            }
            builder.AddLine((float)width, (float)height);
            builder.EndFigure(CanvasFigureLoop.Closed);
            var pathGeo = new PathGeometry();
            pathGeo.Figures.Add(pathFig);
            Path_TrendChart.Data = pathGeo;

            using var cg = CanvasGeometry.CreatePath(builder);
            var path = new CompositionPath(cg);
            var geometry = compositor.CreatePathGeometry(path);
            var clip = compositor.CreateGeometricClip(geometry);
            visual.Clip = clip;

            _recent7Days = list;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Calculate recent 7 days");
        }
    }



    private void Grid_TrendChart_Loaded(object sender, RoutedEventArgs e)
    {
        CalculateRecent7Days();
    }



    private void Grid_TrendChart_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        TextBlock_TrendChart_Time.Opacity = 1;
        TextBlock_TrendChart_Date.Opacity = 1;
        Line_TrendChart.Opacity = 1;
    }



    private void Grid_TrendChart_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        TextBlock_TrendChart_Time.Opacity = 0;
        TextBlock_TrendChart_Date.Opacity = 0;
        Line_TrendChart.Opacity = 0;
    }



    private void Grid_TrendChart_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            if (_recent7Days != null && sender is FrameworkElement fe)
            {
                double width = Border_TrendChart.ActualWidth;
                double step = width / 6;
                double x = e.GetCurrentPoint(Border_TrendChart).Position.X;
                for (int i = 0; i < 7; i++)
                {
                    if (x >= step * (i - 0.5) && x < step * (i + 0.5))
                    {
                        Vector3 lineOffset = new Vector3((float)(step * i - width / 2), 0, 0);
                        Vector3 dateOffset = lineOffset;
                        Vector3 timeOffset = lineOffset;
                        if (i == 0)
                        {
                            dateOffset += new Vector3(6, 0, 0);
                            timeOffset += new Vector3(12, 0, 0);
                        }
                        if (i == 6)
                        {
                            dateOffset -= new Vector3(6, 0, 0);
                            timeOffset -= new Vector3(12, 0, 0);
                        }
                        Line_TrendChart.Translation = lineOffset;
                        var date = (DateTimeOffset.Now - TimeSpan.FromDays(6 - i));
                        TextBlock_TrendChart_Date.Text = $"{date.Month}-{date.Day}";
                        TextBlock_TrendChart_Date.Translation = dateOffset;
                        var time = _recent7Days[i];
                        TextBlock_TrendChart_Time.Text = $"{time.TotalHours:N0}h {time.Minutes}m";
                        TextBlock_TrendChart_Time.Translation = timeOffset;
                        break;
                    }
                }
            }
        }
        catch { }
    }



}
