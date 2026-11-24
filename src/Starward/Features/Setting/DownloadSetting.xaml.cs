using Microsoft.Extensions.Logging;
using Starward.Features.RPC;
using Starward.GameInstall;
using System;
using System.Threading.Tasks;


namespace Starward.Features.Setting;

public sealed partial class DownloadSetting : PageBase
{

    private readonly ILogger<DownloadSetting> _logger = AppConfig.GetLogger<DownloadSetting>();



    public DownloadSetting()
    {
        this.InitializeComponent();
    }



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
                await client.SetRateLimiterAsync(new RateLimiterMessage { Enable = limit > 0, BytesPerSecond = limit }, deadline: DateTime.UtcNow.AddSeconds(3));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Set rate limiter");
        }
    }


    #endregion



}
