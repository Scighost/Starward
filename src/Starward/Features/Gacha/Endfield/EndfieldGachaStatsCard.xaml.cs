using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Starward.Features.Gacha.Endfield;

public sealed partial class EndfieldGachaStatsCard : UserControl
{
    public EndfieldGachaStatsCard()
    {
        InitializeComponent();
    }

    private void Rarity_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is Pivot pivot && Segmented_Rarity is { } tabs
            && pivot.SelectedIndex >= 0 && pivot.SelectedIndex < tabs.Items.Count
            && tabs.SelectedIndex != pivot.SelectedIndex)
        {
            tabs.SelectedIndex = pivot.SelectedIndex;
        }
        else if (sender == Segmented_Rarity && Pivot_Rarity is { } pages
            && Segmented_Rarity.SelectedIndex >= 0 && Segmented_Rarity.SelectedIndex < pages.Items.Count
            && pages.SelectedIndex != Segmented_Rarity.SelectedIndex)
        {
            pages.SelectedIndex = Segmented_Rarity.SelectedIndex;
        }
    }

    public EndfieldGachaPoolStats PoolStats
    {
        get => (EndfieldGachaPoolStats)GetValue(PoolStatsProperty);
        set => SetValue(PoolStatsProperty, value);
    }

    public static readonly DependencyProperty PoolStatsProperty = DependencyProperty.Register(
        nameof(PoolStats), typeof(EndfieldGachaPoolStats), typeof(EndfieldGachaStatsCard), new PropertyMetadata(null));
}
