using Microsoft.Extensions.Logging;
using Starward.Core;
using System;
using System.Net.Http;
using System.Threading.Tasks;
using Windows.System;

namespace Starward.Pages.GameLauncher;

public partial class GenshinCloudLauncherPage : GameLauncherPage
{


    public GenshinCloudLauncherPage() : base()
    {

    }



    protected override void OnLoaded()
    {
        base.OnLoaded();
        Button_UninstallGame.IsEnabled = false;
        Button_SettingRepairGame.IsEnabled = false;
    }



    protected override async Task UpdateGameContentAsync()
    {
        try
        {
            var content = await _hoYoPlayService.GetGameContentAsync(GameBiz.hk4e_cn);
            GameBannerAndPost.GameContent = content;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("Cannot get game launcher content ({CurrentGameBiz}): {error}", CurrentGameBiz, ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game launcher content ({CurrentGameBiz})", CurrentGameBiz);
        }
    }




    public override bool IsGameSupportCompleteRepair => false;


    public override bool IsStartGameButtonEnable => LocalGameVersion != null && LocalGameVersion >= LatestGameVersion && IsGameExeExists && !IsGameRunning;


    public override bool IsDownloadGameButtonEnable => (LocalGameVersion == null && !IsGameExeExists) || ((LocalGameVersion == null || !IsGameExeExists) && !IsGameSupportCompleteRepair);


    public override bool IsUpdateGameButtonEnable => false;


    public override bool IsPreInstallButtonEnable => false;


    public override bool IsRepairGameButtonEnable => false;



    protected override async Task DownloadGameAsync()
    {
        await Launcher.LaunchUriAsync(new Uri("https://ys.mihoyo.com/cloud/#/download"));
    }



    protected override Task PreDownloadGameAsync()
    {
        return base.PreDownloadGameAsync();
    }



    protected override Task RepairGameAsync()
    {
        return base.RepairGameAsync();
    }


    protected override Task ReinstallGameAsync()
    {
        return base.ReinstallGameAsync();
    }


    protected override Task UninstallGameAsync()
    {
        return base.UninstallGameAsync();
    }




}
