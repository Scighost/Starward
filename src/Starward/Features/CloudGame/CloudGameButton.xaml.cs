using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Features.GameLauncher;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;


namespace Starward.Features.CloudGame;

[INotifyPropertyChanged]
public sealed partial class CloudGameButton : UserControl
{



    private readonly ILogger<CloudGameButton> _logger = AppConfig.GetLogger<CloudGameButton>();



    public CloudGameButton()
    {
        this.InitializeComponent();
    }



    public GameId CurrentGameId { get; set; }



    public string? ExePath { get; set => SetProperty(ref field, value); }


    public string? RunningProcessInfo { get; set => SetProperty(ref field, value); }



    private void Flyout_Opened(object sender, object e)
    {
        try
        {
            string key = "";
            RunningProcessInfo = null;
            if (CurrentGameId?.GameBiz == GameBiz.hk4e_cn)
            {
                key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Genshin Impact Cloud Game";
            }
            else if (CurrentGameId?.GameBiz == GameBiz.hk4e_global)
            {
                key = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Genshin Impact Cloud";
            }
            if (!string.IsNullOrWhiteSpace(key))
            {
                string? folder = Registry.GetValue(key, "InstallPath", null) as string;
                string? exeName = Registry.GetValue(key, "ExeName", null) as string;
                string? path = Path.Join(folder, exeName);
                if (File.Exists(path))
                {
                    ExePath = path;
                    int session = Process.GetCurrentProcess().SessionId;
                    Process? p = Process.GetProcessesByName(exeName!.Replace(".exe", "")).FirstOrDefault(x => x.SessionId == session);
                    if (p is not null)
                    {
                        RunningProcessInfo = $"{Lang.LauncherPage_GameIsRunning}\n{exeName} ({p.Id})";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get cloud game state {GameBiz}", CurrentGameId?.GameBiz);
        }
    }










    [RelayCommand]
    private async Task StartGameAsync()
    {
        try
        {
            if (File.Exists(ExePath))
            {
                Process? p = Process.Start(new ProcessStartInfo
                {
                    FileName = ExePath,
                    UseShellExecute = true,
                    WorkingDirectory = Path.GetDirectoryName(ExePath),
                });
                if (p is not null)
                {
                    WeakReferenceMessenger.Default.Send(new GameStartedMessage());
                    await Task.Delay(3000);
                    if (!p.HasExited)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = AppConfig.StarwardExecutePath,
                            Arguments = $"playtime --biz {CurrentGameId.GameBiz} --pid {p.Id}",
                            CreateNoWindow = true,
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start cloud game {GameBiz}", CurrentGameId?.GameBiz);
        }
    }



    [RelayCommand]
    private async Task InstallGameAsync()
    {
        try
        {
            string url = "";
            if (CurrentGameId?.GameBiz == GameBiz.hk4e_cn)
            {
                url = "https://ys.mihoyo.com/cloud/#/download";
            }
            else if (CurrentGameId?.GameBiz == GameBiz.hk4e_global)
            {
                url = "https://cloudgenshin.hoyoverse.com/";
            }
            if (!string.IsNullOrWhiteSpace(url))
            {
                await Launcher.LaunchUriAsync(new Uri(url));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Install cloud game {GameBiz}", CurrentGameId?.GameBiz);
        }
    }


}
