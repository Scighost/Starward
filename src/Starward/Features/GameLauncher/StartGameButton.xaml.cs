using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using System.Windows.Input;


namespace Starward.Features.GameLauncher;

[INotifyPropertyChanged]
public sealed partial class StartGameButton : UserControl
{


    private static Brush AccentFillColorDefaultBrush => (Brush)Application.Current.Resources["AccentFillColorDefaultBrush"];
    private static Brush TextOnAccentFillColorDisabled => (Brush)Application.Current.Resources["TextOnAccentFillColorDisabledBrush"];
    private static Brush TextOnAccentFillColorPrimaryBrush => (Brush)Application.Current.Resources["TextOnAccentFillColorPrimaryBrush"];


    public StartGameButton()
    {
        this.InitializeComponent();
        this.ActualThemeChanged += StartGameButton_ActualThemeChanged;
    }



    public GameState State
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateButtonState();
            }
        }
    }


    public bool PointerOver
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateButtonState();
            }
        }
    }


    public bool CanExecute
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                UpdateButtonState();
            }
        }
    } = true;



    public string? RunningGameInfo { get; set => SetProperty(ref field, value); }




    public bool TextBlock_StartGame_Visibility => State is GameState.StartGame;
    public bool TextBlock_GameIsRunning_Visibility => State is GameState.GameIsRunning;
    public bool TextBlock_InstallGame_Visibility => State is GameState.InstallGame;
    public bool TextBlock_UpdateGame_Visibility => State is GameState.UpdateGame;
    public bool TextBlock_ResumeDownload_Visibility => State is GameState.ResumeDownload or GameState.Paused;
    public double TextBlock_ResumeDownload_Opacity => (State is GameState.ResumeDownload || PointerOver) ? 1 : 0;
    public bool TextBlock_Waiting_Visibility => State is GameState.Waiting;
    public bool TextBlock_Pause_Visibility => State is GameState.Downloading && PointerOver;
    public double TextBlock_Pause_Opacity => TextBlock_Pause_Visibility ? 1 : 0;
    public bool TextBlock_Paused_Visibility => State is GameState.Paused && !PointerOver;


    public bool Rect_AccentBackground_Visibility => CanExecute && !(State is GameState.GameIsRunning or GameState.Downloading or GameState.Paused);

    public bool ProgressRing_IndeterminateLoading_Visibility => (State is GameState.StartGame or GameState.InstallGame or GameState.UpdateGame or GameState.ResumeDownload or GameState.Paused) && !CanExecute;




    public ICommand Command { get; set => SetProperty(ref field, value); }


    public ICommand SettingCommand { get; set => SetProperty(ref field, value); }





    private void UpdateButtonState()
    {
        OnPropertyChanged(nameof(TextBlock_StartGame_Visibility));
        OnPropertyChanged(nameof(TextBlock_StartGame_Visibility));
        OnPropertyChanged(nameof(TextBlock_GameIsRunning_Visibility));
        OnPropertyChanged(nameof(TextBlock_InstallGame_Visibility));
        OnPropertyChanged(nameof(TextBlock_UpdateGame_Visibility));
        OnPropertyChanged(nameof(TextBlock_ResumeDownload_Visibility));
        OnPropertyChanged(nameof(TextBlock_ResumeDownload_Opacity));
        OnPropertyChanged(nameof(TextBlock_Waiting_Visibility));
        OnPropertyChanged(nameof(TextBlock_Pause_Visibility));
        OnPropertyChanged(nameof(TextBlock_Pause_Opacity));
        OnPropertyChanged(nameof(TextBlock_Paused_Visibility));
        OnPropertyChanged(nameof(Rect_AccentBackground_Visibility));
        OnPropertyChanged(nameof(ProgressRing_IndeterminateLoading_Visibility));

        Button_GameAction.Foreground = (CanExecute, Rect_AccentBackground_Visibility, PointerOver) switch
        {
            (false, _, _) => TextOnAccentFillColorDisabled,
            (true, false, true) => AccentFillColorDefaultBrush,
            (true, false, false) => TextOnAccentFillColorDisabled,
            _ => TextOnAccentFillColorPrimaryBrush
        };
        Button_Setting.Foreground = (Rect_AccentBackground_Visibility, PointerOver) switch
        {
            (false, true) => AccentFillColorDefaultBrush,
            (false, false) => TextOnAccentFillColorDisabled,
            _ => TextOnAccentFillColorPrimaryBrush
        };
    }



    private void Grid_Root_PointerEntered(object sender, PointerRoutedEventArgs e)
    {
        PointerOver = true;
        if (State is GameState.GameIsRunning or GameState.Downloading)
        {
            Flyout_DownloadProgress.ShowAt(Grid_Root, new FlyoutShowOptions
            {
                Placement = FlyoutPlacementMode.Top,
                ShowMode = FlyoutShowMode.Transient,
            });
        }
    }



    private void Grid_Root_PointerExited(object sender, PointerRoutedEventArgs e)
    {
        PointerOver = false;
        Flyout_DownloadProgress.Hide();
    }



    private void StartGameButton_ActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateButtonState();
    }



}
