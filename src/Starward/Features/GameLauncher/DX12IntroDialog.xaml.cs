using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml.Controls;
using Starward.Core.HoYoPlay;


namespace Starward.Features.GameLauncher;

[INotifyPropertyChanged]
public sealed partial class DX12IntroDialog : ContentDialog
{


    public GameDXConfig GameDXConfig { get; set; }



    public DX12IntroDialog()
    {
        this.InitializeComponent();
    }



    [RelayCommand]
    private void Close()
    {
        this.Hide();
    }


}
