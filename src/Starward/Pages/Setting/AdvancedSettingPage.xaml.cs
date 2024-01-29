using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Starward.Services;
using System;
using System.Threading.Tasks;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class AdvancedSettingPage : PageBase
{


    private readonly ILogger<AdvancedSettingPage> _logger = AppConfig.GetLogger<AdvancedSettingPage>();



    public AdvancedSettingPage()
    {
        this.InitializeComponent();
        CheckUrlProtocol();
    }







    #region URL Protocol



    [ObservableProperty]
    private bool enableUrlProtocol;


    partial void OnEnableUrlProtocolChanged(bool value)
    {
        try
        {
            if (value)
            {
                UrlProtocolService.RegisterProtocol();
            }
            else
            {
                UrlProtocolService.UnregisterProtocol();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Enable url protocol changed");
        }
    }



    private async void CheckUrlProtocol()
    {
        try
        {
            var status = await Launcher.QueryUriSupportAsync(new Uri("starward://"), LaunchQuerySupportType.Uri);
#pragma warning disable MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            enableUrlProtocol = status is LaunchQuerySupportStatus.Available;
#pragma warning restore MVVMTK0034 // Direct field reference to [ObservableProperty] backing field
            OnPropertyChanged(nameof(EnableUrlProtocol));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Check url protocol");
        }
    }




    [RelayCommand]
    private async Task TestUrlProtocolAsync()
    {
        try
        {
            await Launcher.LaunchUriAsync(new Uri("starward://test"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Test url protocol");
        }
    }



    #endregion








}
