using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Core.HoYoPlay;


namespace Starward.Features.ViewHost;

[INotifyPropertyChanged]
public sealed partial class MainView : UserControl
{



    public GameId CurrentGameId { get; private set => SetProperty(ref field, value); }


    public GameBiz CurrentGameBiz { get; private set => SetProperty(ref field, value); }




    public MainView()
    {
        this.InitializeComponent();
        CurrentGameId = GameSelector.CurrentGameId!;
        this.Loaded += MainView_Loaded;
        GameSelector.CurrentGameChanged += GameSelector_CurrentGameChanged;
    }

    private void GameSelector_CurrentGameChanged(object? sender, (GameId, bool DoubleTapped) e)
    {
        CurrentGameId = e.Item1;
    }

    private void MainView_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }







}
