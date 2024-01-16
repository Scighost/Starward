using Microsoft.UI.Xaml.Data;
using System;

namespace Starward.Converters;

internal class SimulatedUniverseBuffIconConverter : IValueConverter
{

    private const string Icon_120 = "ms-appx:///Assets/Image/RogueInterveneKnight.png";
    private const string Icon_121 = "ms-appx:///Assets/Image/RogueInterveneMemory.png";
    private const string Icon_122 = "ms-appx:///Assets/Image/RogueInterveneWarlock.png";
    private const string Icon_123 = "ms-appx:///Assets/Image/RogueIntervenePirest.png";
    private const string Icon_124 = "ms-appx:///Assets/Image/RogueInterveneRogue.png";
    private const string Icon_125 = "ms-appx:///Assets/Image/RogueInterveneWarrior.png";
    private const string Icon_126 = "ms-appx:///Assets/Image/RogueInterveneJoy.png";
    private const string Icon_127 = "ms-appx:///Assets/Image/RogueIntervenePropagation.png";
    private const string Icon_128 = "ms-appx:///Assets/Image/RogueInterveneMage.png";
    private const string TransparentBackground = "ms-appx:///Assets/Image/Transparent.png";

    public object Convert(object value, Type targetType, object parameter, string language)
    {
        return (int)value switch
        {
            120 => Icon_120,
            121 => Icon_121,
            122 => Icon_122,
            123 => Icon_123,
            124 => Icon_124,
            125 => Icon_125,
            126 => Icon_126,
            127 => Icon_127,
            128 => Icon_128,
            _ => TransparentBackground,
        };
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }

}
