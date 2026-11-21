using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Starward.Features.Gacha.Endfield;

public sealed partial class EndfieldGachaStatsCard : UserControl
{
    public EndfieldGachaStatsCard()
    {
        InitializeComponent();
    }

    public EndfieldGachaPoolStats PoolStats
    {
        get => (EndfieldGachaPoolStats)GetValue(PoolStatsProperty);
        set => SetValue(PoolStatsProperty, value);
    }

    public static readonly DependencyProperty PoolStatsProperty = DependencyProperty.Register(
        nameof(PoolStats), typeof(EndfieldGachaPoolStats), typeof(EndfieldGachaStatsCard), new PropertyMetadata(null));
}
