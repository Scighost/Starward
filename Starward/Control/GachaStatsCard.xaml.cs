// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Model;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Control;

public sealed partial class GachaStatsCard : UserControl
{

    public GachaStatsCard()
    {
        this.InitializeComponent();
    }



    public GachaTypeStats WarpTypeStats
    {
        get { return (GachaTypeStats)GetValue(WarpTypeStatsProperty); }
        set { SetValue(WarpTypeStatsProperty, value); }
    }

    // Using a DependencyProperty as the backing store for WarpTypeStats.  This enables animation, styling, binding, etc...
    public static readonly DependencyProperty WarpTypeStatsProperty =
        DependencyProperty.Register("WarpTypeStats", typeof(GachaTypeStats), typeof(GachaStatsCard), new PropertyMetadata(null));





}
