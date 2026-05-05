using Aprillz.MewUI;
using Aprillz.MewUI.Controls;
using Starward.Setup.Locale;

namespace Starward.Setup.Views;

public class TextWindow : WindowBase
{


    const int WindowWidth = 460;

    const int WindowHeight = 260;



    private string promptText;



    public TextWindow(string text) : base()
    {
        this.Resizable(WindowWidth, WindowHeight);
        this.Title = "Starward";
        promptText = text;
        BuildUI();
    }




    private void BuildUI()
    {
        this.Content = new Grid().Rows("3*,2*").Children(
             new TextBlock().Row(0)
                            .Top()
                            .Left()
                            .FontSize(14)
                            .Text("Starward")
                            .Foreground(Theme.Palette.DisabledAccent)
                            .Margin(12, 8, 0, 0),
             BuildHeader(),

             new TextBlock().Row(1)
                            .Center()
                            .FontSize(13)
                            .Margin(24, 0, 24, 0)
                            .TextWrapping(TextWrapping.Wrap)
                            .Text(promptText)

          );
    }



    private StackPanel BuildHeader()
    {
        return new StackPanel().Row(0)
                               .Margin(0, 32, 0, 0)
                               .Center()
                               .Horizontal()
                               .Spacing(24)
                               .Children(
            new Image().Center()
                       .Size(120)
                       .ImageScaleQuality(ImageScaleQuality.HighQuality)
                       .SourceResource<InstallWindow>("Starward.Setup.Firefly.png"),
            new StackPanel().CenterVertical()
                            .Spacing(4)
                            .Children(
                new TextBlock().FontWeight(FontWeight.Bold)
                               .FontSize(28)
                               .Foreground(Theme.Palette.Accent)
                               .Text("Starward"),
                new TextBlock().FontSize(14)
                               .Foreground(Theme.Palette.DisabledAccent)
                               .Text(Lang.GameLauncherForMiHoYo)));
    }



}
