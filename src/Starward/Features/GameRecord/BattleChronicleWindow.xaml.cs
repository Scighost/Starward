using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Starward.Core.GameRecord;
using Starward.Frameworks;
using Windows.Graphics;


namespace Starward.Features.GameRecord;

[INotifyPropertyChanged]
public sealed partial class BattleChronicleWindow : WindowEx
{


    public BattleChronicleWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
    }



    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        Title = Lang.HoyolabToolboxPage_BattleChronicle;
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
        CenterInScreen();
    }




    private void CenterInScreen()
    {
        RectInt32 workArea = DisplayArea.GetFromWindowId(MainWindowId, DisplayAreaFallback.Nearest).WorkArea;
        int h = (int)(workArea.Height * 0.95);
        int w = (int)(h / 16.0 * 9.0);
        if (w > workArea.Width)
        {
            w = (int)(workArea.Width * 0.95);
            h = (int)(w * 16.0 / 9.0);
        }
        int x = workArea.X + (workArea.Width - w) / 2;
        int y = workArea.Y + (workArea.Height - h) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }




    public GameRecordRole? CurrentRole { get; set => SetProperty(ref field, value); }




    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        RootGrid.Loaded -= RootGrid_Loaded;
        _ = bbsWebBridge.LoadPageAsync();
    }




}
