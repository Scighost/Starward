using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Features.RPC;
using Starward.Helpers;
using Starward.RPC.GameInstall;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.System;


namespace Starward.Features.Setting;

[INotifyPropertyChanged]
public sealed partial class DownloadSetting : UserControl
{

    private readonly ILogger<DownloadSetting> _logger = AppConfig.GetLogger<DownloadSetting>();



    public DownloadSetting()
    {
        this.InitializeComponent();
        this.Loaded += DownloadSetting_Loaded;
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
        this.Unloaded += (_, _) => WeakReferenceMessenger.Default.UnregisterAll(this);
    }



    private async void DownloadSetting_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await Task.Delay(300);
        InitializeDefaultInstallPath();
    }




    #region 默认安装文件夹



    /// <summary>
    /// 默认安装文件夹
    /// </summary>
    public string? DefaultInstallPath { get; set => SetProperty(ref field, value); }



    /// <summary>
    /// 初始化默认安装路径
    /// </summary>
    private void InitializeDefaultInstallPath()
    {
        try
        {
            string? path = AppConfig.DefaultGameInstallationPath;
            if (Directory.Exists(path))
            {
                DefaultInstallPath = path;
            }
            else
            {
                AppConfig.DefaultGameInstallationPath = null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get default intall path");
        }
    }



    /// <summary>
    /// 更改默认安装路径
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task ChangeDefaultInstallPathAsync()
    {
        try
        {
            var path = await FileDialogHelper.PickFolderAsync(this.XamlRoot);
            if (Directory.Exists(path))
            {
                DefaultInstallPath = path;
                AppConfig.DefaultGameInstallationPath = path;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to change default install path");
        }
    }



    /// <summary>
    /// 打开默认安装路径
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenDefaultInstallPathAsync()
    {
        try
        {
            if (Directory.Exists(DefaultInstallPath))
            {
                await Launcher.LaunchUriAsync(new Uri(DefaultInstallPath));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open default install path");
        }
    }



    /// <summary>
    /// 删除默认安装路径
    /// </summary>
    [RelayCommand]
    private void DeleteDefaultInstallPath()
    {
        DefaultInstallPath = null;
        AppConfig.DefaultGameInstallationPath = null;
    }



    #endregion




    #region 硬链接



    public bool EnableHardLink
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.EnableHardLink = value;
            }
        }
    } = AppConfig.EnableHardLink;



    #endregion




    #region 下载限速



    /// <summary>
    /// 下载限速
    /// </summary>
    public int SpeedLimit
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                AppConfig.SpeedLimitKBPerSecond = value;
                _ = SetRateLimiterAsync(value);
            }
        }
    } = AppConfig.SpeedLimitKBPerSecond;



    private async Task SetRateLimiterAsync(int value)
    {
        try
        {
            if (RpcService.CheckRpcServerRunning())
            {
                var client = RpcService.CreateRpcClient<GameInstaller.GameInstallerClient>();
                int limit = Math.Clamp(value * 1024, 0, int.MaxValue);
                await client.SetRateLimiterAsync(new RateLimiterMessage { BytesPerSecond = limit }, deadline: DateTime.UtcNow.AddSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set rate limiter");
        }
    }


    #endregion




}
