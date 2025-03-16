using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core.HoYoPlay;
using System;
using System.Threading.Tasks;


namespace Starward.Features.GameLauncher;

[INotifyPropertyChanged]
public sealed partial class GameAuthLoginButton : UserControl
{

    private readonly ILogger<GameAuthLoginButton> _logger = AppConfig.GetLogger<GameAuthLoginButton>();


    private readonly GameAuthLoginService _gameAuthLoginService = AppConfig.GetService<GameAuthLoginService>();


    public GameAuthLoginButton()
    {
        this.InitializeComponent();
    }



    public GameId CurrentGameId { get; set; }


    public long? HyperionAid { get; set => SetProperty(ref field, value); }


    public string? ErrorMessage { get; set => SetProperty(ref field, value); }



    [ObservableProperty]
    public partial bool EnableLoginAuthTicket { get; set; } = AppConfig.EnableLoginAuthTicket ?? false;
    partial void OnEnableLoginAuthTicketChanged(bool value)
    {
        AppConfig.EnableLoginAuthTicket = value;
        AppConfig.SaveConfiguration();
    }


    private void Button_GameAuthLogin_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeGameAuthLoginCommand.Execute(null);
    }



    [RelayCommand]
    private async Task InitializeGameAuthLoginAsync()
    {
        try
        {
            if (CurrentGameId is null || CurrentGameId.GameBiz.Server is not "cn")
            {
                return;
            }
            if (string.IsNullOrWhiteSpace(AppConfig.stoken) || string.IsNullOrWhiteSpace(AppConfig.mid))
            {
                return;
            }
            Button_GameAuthLogin.Visibility = Visibility.Visible;
            ErrorMessage = null;
            HyperionAid = await _gameAuthLoginService.GetHyperionAidAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "InitializeGameAuthLogin");
            ErrorMessage = ex.Message;
        }
    }


}
