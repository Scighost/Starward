using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
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
    }



    [ObservableProperty]
    private ObservableCollection<InstallGameStateModel> _installServices = new();



    private void _installGameManager_InstallTaskAdded(object? sender, InstallGameStateModel e)
    {
        try
        {
            _semaphoreSlim.Wait();
            _timer.Start();
            Button_Controller.Visibility = Visibility.Visible;
            if (!InstallServices.Contains(e))
            {
                InstallServices.Add(e);
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }



    private void _installGameManager_InstallTaskRemoved(object? sender, InstallGameStateModel e)
    {
        try
        {
            _semaphoreSlim.Wait();
            InstallServices.Remove(e);
            if (InstallServices.Count == 0)
            {
                _timer.Stop();
                Button_Controller.Visibility = Visibility.Collapsed;
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }




    private void _timer_Tick(DispatcherQueueTimer sender, object args)
    {
        try
        {
            _semaphoreSlim.Wait();
            foreach (var model in InstallServices)
            {
                model.UpdateState();
            }
        }
        finally
        {
            _semaphoreSlim.Release();
        }
    }


}
