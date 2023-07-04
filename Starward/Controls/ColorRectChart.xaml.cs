using Microsoft.UI.Composition;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;


public sealed partial class ColorRectChart : UserControl
{



    public record struct ChartLegend(string? Legend, int Percent, Color Color);




    private readonly Compositor compositor;


    public ColorRectChart()
    {
        this.InitializeComponent();
        compositor = ElementCompositionPreview.GetElementVisual(this).Compositor;
    }




    private List<ChartLegend>? _series;
    public List<ChartLegend>? Series
    {
        get => _series;
        set
        {
            _series = value;
            ApplySeries(value);
        }
    }



    private void ApplySeries(List<ChartLegend>? newValue)
    {
        try
        {
            FillBrush.GradientStops.Clear();
            if (newValue is null)
            {
                FillBrush.GradientStops.Add(new GradientStop
                {
                    Color = Color.FromArgb(0x60, 0, 0, 0),
                    Offset = 0,
                });
                return;
            }
            double sum = newValue.Sum(x => x.Percent);
            if (sum == 0)
            {
                FillBrush.GradientStops.Add(new GradientStop
                {
                    Color = Color.FromArgb(0x60, 0, 0, 0),
                    Offset = 0,
                });
                return;
            }
            double offset = 0;
            foreach (var item in newValue)
            {
                FillBrush.GradientStops.Add(new GradientStop
                {
                    Color = item.Color,
                    Offset = offset,
                });
                offset += item.Percent / sum;
                FillBrush.GradientStops.Add(new GradientStop
                {
                    Color = item.Color,
                    Offset = offset,
                });
            }
            var visual = ElementCompositionPreview.GetElementVisual(this);
            var clip = compositor.CreateRectangleClip();
            visual.Clip = clip;
            clip.Bottom = 10000;
            var animation = compositor.CreateScalarKeyFrameAnimation();
            animation.Duration = TimeSpan.FromMilliseconds(1000);
            animation.InsertKeyFrame(0, 0);
            animation.InsertKeyFrame(0.6f, ActualWidth == 0 ? 1000 : (float)ActualWidth);
            animation.InsertKeyFrame(1, 10000);
            clip.StartAnimation(nameof(clip.Right), animation);
        }
        catch { }
    }








}
