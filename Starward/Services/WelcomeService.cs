using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Animation;
using System;

namespace Starward.Services;

internal class WelcomeService
{


    private readonly ILogger<WelcomeService> _logger;


    private readonly DatabaseService _databaseService;


    public event EventHandler<(Type Page, object Parameter, NavigationTransitionInfo InfoOverride)> OnNavigateTo;


    public WelcomeService(ILogger<WelcomeService> logger, DatabaseService databaseService)
    {
        _logger = logger;
        _databaseService = databaseService;
    }



    public string TextLanguage { get; set; }

    public int WindowSizeMode { get; set; }

    public int ApiCDNIndex { get; set; }

    public string UserDataFolder { get; set; }



    public void Reset()
    {
        TextLanguage = null!;
        WindowSizeMode = 0;
        ApiCDNIndex = 0;
        UserDataFolder = null!;
    }



    public void NavigateTo(Type page, object parameter, NavigationTransitionInfo infoOverride)
    {
        OnNavigateTo?.Invoke(this, (page, parameter, infoOverride));
    }



    public void ApplySetting()
    {
        _databaseService.SetDatabase(UserDataFolder);
        AppConfig.UserDataFolder = UserDataFolder;
        AppConfig.Language = TextLanguage;
        AppConfig.WindowSizeMode = WindowSizeMode;
        AppConfig.ApiCDNIndex = ApiCDNIndex;
    }


}
