using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Win32;
using Starward.Core;
using Starward.Core.HoYoPlay;
using Starward.Frameworks;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.System;


namespace Starward.Features.CloudGame;

[INotifyPropertyChanged]
public sealed partial class CloudGameButton : UserControl
{



    public CloudGameButton()
    {
        this.InitializeComponent();
    }



    public GameId CurrentGameId { get; set; }



    public string? ExePath { get; set => SetProperty(ref field, value); }




    private void Button_CloudGame_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            string key = "";
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
                }
            }
        }
        catch (Exception ex)
        {

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
                    await Task.Delay(3000);
                    if (!p.HasExited)
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = AppSetting.StarwardExecutePath,
                            Arguments = $"playtime --biz {CurrentGameId.GameBiz} --pid {p.Id}",
                            CreateNoWindow = true,
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {

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

        }
    }




}
