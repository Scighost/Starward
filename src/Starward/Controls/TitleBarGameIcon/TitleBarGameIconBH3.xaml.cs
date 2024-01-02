using Starward.Core;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls.TitleBarGameIcon;

public sealed partial class TitleBarGameIconBH3 : TitleBarGameIconBase
{


    public override GameBiz GameBiz { get; protected init; } = GameBiz.Honkai3rd;


    public TitleBarGameIconBH3()
    {
        this.InitializeComponent();
    }


}
