using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Services;
using System;
using Vanara.PInvoke;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.SystemTray;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class InstallGameSystemTrayPage : Page
{


    private readonly ILogger<InstallGameSystemTrayPage> _logger = AppConfig.GetLogger<InstallGameSystemTrayPage>();

    private readonly SystemTrayService _systemTrayService = AppConfig.GetService<SystemTrayService>();



    public InstallGameSystemTrayPage()
    {
        this.InitializeComponent();
    }



    public InstallGameSystemTrayPage(GameBiz gameBiz)
    {
        this.InitializeComponent();
        this.gameBiz = gameBiz;
        Icon = gameBiz.ToGame() switch
        {
            GameBiz.Honkai3rd => new BitmapImage(new("ms-appx:///Assets/Image/icon_bh3.jpg")),
            GameBiz.GenshinImpact => new BitmapImage(new("ms-appx:///Assets/Image/icon_ys.jpg")),
            GameBiz.StarRail => new BitmapImage(new("ms-appx:///Assets/Image/icon_sr.jpg")),
            _ => null!,
        };
    }



    private GameBiz gameBiz;

    public ImageSource Icon { get; set; }

    public string GameName => gameBiz.ToGameName();

    public string GameServer => gameBiz.ToGameServer();


    [RelayCommand]
    private void OpenInstaller()
    {
        User32.ShowWindow(MainWindow.Current.HWND, ShowWindowCommand.SW_SHOWNORMAL);
        User32.SetForegroundWindow(MainWindow.Current.HWND);
    }



    [RelayCommand]
    private void Exit()
    {
        _systemTrayService.Dispose();
        MainWindow.Current.Close();
    }


    [RelayCommand]
    private void Click()
    {
        ActionButtonClicked?.Invoke(this, EventArgs.Empty);
    }



    public event EventHandler ActionButtonClicked;



    [ObservableProperty]
    private bool isActionButtonEnable;

    [ObservableProperty]
    private string? actionButtonIcon = StartIcon;
    private const string StartIcon = "\uE768";

    [ObservableProperty]
    private string? actionButtonText = Lang.DownloadGamePage_Start;

    [ObservableProperty]
    private string actionType;

    [ObservableProperty]
    private bool isProgressStateVisible = true;

    [ObservableProperty]
    private string? stateText;

    [ObservableProperty]
    private string? progressBytesText;

    [ObservableProperty]
    private string? progressText;

    [ObservableProperty]
    private double progressValue;

    [ObservableProperty]
    private string? speedText;

    [ObservableProperty]
    private string? remainTimeText;




}
