using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Features.RPC;
using Starward.Features.UrlProtocol;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;


namespace Starward.Features.Setting;

[INotifyPropertyChanged]
public sealed partial class AdvancedSetting : UserControl
{

    private readonly ILogger<AdvancedSetting> _logger = AppConfig.GetLogger<AdvancedSetting>();


    private readonly RpcService _rpcService = AppConfig.GetService<RpcService>();


    public AdvancedSetting()
    {
        this.InitializeComponent();
        Loaded += AdvancedSetting_Loaded;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        this.Unloaded += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);
    }



    private async void AdvancedSetting_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(300);
        _ = GetRpcServerStateAsync();
        CheckUrlProtocol();
    }





    #region URL Protocol



    [ObservableProperty]
    public bool _EnableUrlProtocol;


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
            _EnableUrlProtocol = status is LaunchQuerySupportStatus.Available;
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




    #region RPC


    public bool KeepRpcServerRunningInBackground
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.KeepRpcServerRunningInBackground = value;
                SetRpcServerRunning(value);
            }
        }
    } = AppConfig.KeepRpcServerRunningInBackground;



    private void SetRpcServerRunning(bool value)
    {
        try
        {
            _rpcService.KeepRunningOnExited(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set rpc server running");
        }
    }



    public int RPCServerProcessId { get; set => SetProperty(ref field, value); }



    private async Task GetRpcServerStateAsync()
    {
        try
        {
            RPCServerProcessId = 0;
            StackPanel_RpcState_NotRunning.Visibility = Visibility.Collapsed;
            StackPanel_RpcState_Running.Visibility = Visibility.Collapsed;
            StackPanel_RpcState_CannotConnect.Visibility = Visibility.Collapsed;
            if (RpcService.CheckRpcServerRunning())
            {
                var info = await _rpcService.GetRpcServerInfoAsync(DateTime.UtcNow.AddSeconds(3));
                RPCServerProcessId = info.ProcessId;
                StackPanel_RpcState_Running.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanel_RpcState_NotRunning.Visibility = Visibility.Visible;
            }
        }
        catch (RpcException ex) when (ex.Status is { StatusCode: StatusCode.DeadlineExceeded })
        {
            int sessionId = Process.GetCurrentProcess().SessionId;
            var process = Process.GetProcessesByName("Starward.RPC").FirstOrDefault(x => x.SessionId == sessionId);
            if (process != null)
            {
                RPCServerProcessId = process.Id;
                StackPanel_RpcState_CannotConnect.Visibility = Visibility.Visible;
            }
            else
            {
                StackPanel_RpcState_NotRunning.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get rpc server state");
        }
    }


    [RelayCommand]
    private async Task RunRpcServerAsync()
    {
        try
        {
            await _rpcService.EnsureRpcServerRunningAsync();
            await Task.Delay(1000);
            await GetRpcServerStateAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Run rpc server");
        }
    }



    [RelayCommand]
    private async Task StopRpcServerAsync()
    {
        try
        {
            await _rpcService.StopRpcServerAsync(DateTime.UtcNow.AddSeconds(1));
            await Task.Delay(1000);
            await GetRpcServerStateAsync();
        }
        catch (RpcException ex) when (ex.Status is { StatusCode: StatusCode.DeadlineExceeded })
        {
            try
            {
                var p = Process.GetProcessById(RPCServerProcessId);
                p.Kill();
                await Task.Delay(1000);
                await GetRpcServerStateAsync();
            }
            catch { }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stop rpc server");
        }
    }





    #endregion


}
