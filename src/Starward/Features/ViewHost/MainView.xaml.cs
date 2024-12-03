using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Core.HoYoPlay;


namespace Starward.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class MainView : UserControl
{



    public GameId CurrentGameId { get; private set; }


    public GameBiz CurrentGameBiz { get; private set; }




    public MainView()
    {
        this.InitializeComponent();
        this.Loaded += MainView_Loaded;
    }





    private void MainView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }







}
