using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Messages;
using Starward.Services.Download;
using System;
using System.Collections.ObjectModel;
using System.Threading;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class InstallGameController : UserControl
{


    private readonly ILogger<InstallGameController> _logger = AppConfig.GetLogger<InstallGameController>();


    private readonly InstallGameManager _installGameManager = InstallGameManager.Instance;


    private readonly SemaphoreSlim _semaphoreSlim = new SemaphoreSlim(1);


    private readonly DispatcherQueueTimer _timer;


    public InstallGameController()
    {
        this.InitializeComponent();
        _installGameManager.InstallTaskAdded += _installGameManager_InstallTaskAdded;
        _installGameManager.InstallTaskRemoved += _installGameManager_InstallTaskRemoved;
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += _timer_Tick;
        WeakReferenceMessenger.Default.Register<ShowInstallGameControllerFlyoutMessage>(this, (_, _) =>
        DispatcherQueue.TryEnqueue(() =>
        {
            Button_Controller.Visibility = Visibility.Visible;
            Flyout_InstallGame.ShowAt(Button_Controller);
        }));
    }



    [ObservableProperty]
    private ObservableCollection<InstallGameStateModel> _installServices = new();


    [ObservableProperty]
    private bool _ProgressIsIndeterminate;


    [ObservableProperty]
    private double _ProgressValue;


    [ObservableProperty]
    private bool _ProgressActive;


    private async void _installGameManager_InstallTaskAdded(object? sender, InstallGameStateModel e)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            _timer.Start();
            Button_Controller.Visibility = Visibility.Visible;
            if (!InstallServices.Contains(e))
            {
                e.InstallFailed -= Model_InstallFailed;
                e.InstallFailed += Model_InstallFailed;
                InstallServices.Add(e);
                UpdateSpeedState();
                Flyout_InstallGame.ShowAt(Button_Controller);
            }
        }
        catch { }
        finally
        {
            _semaphoreSlim.Release();
        }
    }



    private async void _installGameManager_InstallTaskRemoved(object? sender, InstallGameStateModel e)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            InstallServices.Remove(e);
            e.InstallFailed -= Model_InstallFailed;
            if (InstallServices.Count == 0)
            {
                _timer.Stop();
                Button_Controller.Visibility = Visibility.Collapsed;
                Flyout_InstallGame.Hide();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }




    private async void _timer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            await _semaphoreSlim.WaitAsync();
            UpdateSpeedState();
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }



    private void UpdateSpeedState()
    {
        long totalBytes = 0;
        long finishedBytes = 0;
        bool determinate = false;
        foreach (var model in InstallServices)
        {
            model.UpdateState();
            if (model.Service.State is InstallGameState.Download)
            {
                totalBytes += model.Service.TotalBytes;
                finishedBytes += model.Service.FinishBytes;
            }
            if (model.Service.State is not InstallGameState.Verify and not InstallGameState.Decompress and not InstallGameState.Clean)
            {
                determinate = true;
            }
        }
        if (totalBytes > 0)
        {
            ProgressIsIndeterminate = false;
            ProgressValue = 100d * finishedBytes / totalBytes;
        }
        else if (determinate)
        {
            ProgressIsIndeterminate = false;
        }
        else
        {
            ProgressIsIndeterminate = true;
        }
    }



    private void Model_InstallFailed(object? sender, Exception e)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            Image_Alert.Visibility = Visibility.Visible;
        });
    }


    private void Grid_ActionButtonOverlay_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            if (grid.FindName("StackPanel_ActionButton") is StackPanel stackPanel)
            {
                stackPanel.Opacity = 1;
            }
            if (grid.FindName("TextBlock_ProgressValue") is TextBlock textBlock)
            {
                textBlock.Opacity = 0;
            }
        }
    }



    private void Grid_ActionButtonOverlay_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (sender is Grid grid)
        {
            if (grid.FindName("StackPanel_ActionButton") is StackPanel stackPanel)
            {
                stackPanel.Opacity = 0;
            }
            if (grid.FindName("TextBlock_ProgressValue") is TextBlock textBlock)
            {
                textBlock.Opacity = 1;
            }
        }
    }


    private void Flyout_InstallGame_Opened(object sender, object e)
    {
        _timer.Interval = TimeSpan.FromSeconds(0.1);
        Image_Alert.Visibility = Visibility.Collapsed;
    }


    private void Flyout_InstallGame_Closed(object sender, object e)
    {
        _timer.Interval = TimeSpan.FromSeconds(1);
    }

}
