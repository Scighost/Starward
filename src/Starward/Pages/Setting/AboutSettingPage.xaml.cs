using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Core.Metadata;
using Starward.Helpers;
using Starward.Services;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class AboutSettingPage : PageBase
{


    private readonly ILogger<AboutSettingPage> _logger = AppConfig.GetLogger<AboutSettingPage>();


    private readonly UpdateService _updateService = AppConfig.GetService<UpdateService>();


    private readonly MetadataClient _metadataClient = AppConfig.GetService<MetadataClient>();


    public AboutSettingPage()
    {
        this.InitializeComponent();
    }




    [ObservableProperty]
    private bool enablePreviewRelease = AppConfig.EnablePreviewRelease;
    partial void OnEnablePreviewReleaseChanged(bool value)
    {
        AppConfig.EnablePreviewRelease = value;
    }


    [ObservableProperty]
    private bool isUpdated;


    [ObservableProperty]
    private string? updateErrorText;


    [RelayCommand]
    private async Task CheckUpdateAsync()
    {
        try
        {
            IsUpdated = false;
            UpdateErrorText = null;
            var release = await _updateService.CheckUpdateAsync(true);
            if (release != null)
            {
                MainWindow.Current.OverlayFrameNavigateTo(typeof(UpdatePage), release);
            }
            else
            {
                IsUpdated = true;
            }
        }
        catch (Exception ex)
        {
            UpdateErrorText = ex.Message;
            _logger.LogError(ex, "Check update");
        }
    }



    [RelayCommand]
    private async Task VerifyFileAsync()
    {
        try
        {
            var release = await _metadataClient.GetReleaseAsync(EnablePreviewRelease, RuntimeInformation.OSArchitecture);
            if (release.Version != AppConfig.AppVersion)
            {
                NotificationBehavior.Instance.Warning(Lang.AboutSettingPage_VerifyFailed, string.Format(Lang.AboutSettingPage_CurrentVersionIsNotTheLatestVersion, release.Version));
                return;
            }
            bool failed = false;
            if (AppConfig.IsPortable)
            {
                string? baseDir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('/', '\\'));
                await Parallel.ForEachAsync(release.SeparateFiles, async (file, _) =>
                {
                    if (failed)
                    {
                        return;
                    }
                    var path = Path.Join(baseDir, file.Path);
                    if (File.Exists(path))
                    {
                        if (failed)
                        {
                            return;
                        }
                        using var fs = File.OpenRead(path);
                        string hash = Convert.ToHexString(await SHA256.HashDataAsync(fs));
                        if (hash != file.Hash)
                        {
                            failed = true;
                        }
                    }
                    else
                    {
                        failed = true;
                    }
                });
            }
            else
            {
                string baseDir = AppContext.BaseDirectory;
                string prefix = $"app-{release.Version}";
                await Parallel.ForEachAsync(release.SeparateFiles, async (file, _) =>
                {
                    if (failed)
                    {
                        return;
                    }
                    if (file.Path.StartsWith(prefix))
                    {
                        var path = Path.Join(baseDir, file.Path.Replace(prefix, ""));
                        if (File.Exists(path))
                        {
                            if (failed)
                            {
                                return;
                            }
                            using var fs = File.OpenRead(path);
                            string hash = Convert.ToHexString(await SHA256.HashDataAsync(fs));
                            if (hash != file.Hash)
                            {
                                failed = true;
                            }
                        }
                        else
                        {
                            failed = true;
                        }
                    }
                });
            }
            if (failed)
            {
                async void action()
                {
                    if (AppConfig.IsPortable)
                    {
                        StartToRepair();
                    }
                    else
                    {
                        await Launcher.LaunchUriAsync(new Uri($"https://github.com/Scighost/Starward/releases/tag/{release.Version}"));
                    }
                }
                NotificationBehavior.Instance.ShowWithButton(InfoBarSeverity.Error, Lang.AboutSettingPage_VerifyFailed, Lang.AboutSettingPage_ClickButtonToRepairFiles, Lang.AboutSettingPage_RepairFiles, action);
            }
            else
            {
                NotificationBehavior.Instance.Success(Lang.AboutSettingPage_VerifySuccessfully);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Verify files");
            NotificationBehavior.Instance.Error("Verify files", ex.Message);
        }
    }



    private async void StartToRepair()
    {
        try
        {
            if (AppConfig.IsPortable)
            {
                string script = $$""""
                $ProgressPreference = 'SilentlyContinue';
                $ErrorActionPreference = 'Continue';

                function CheckResource {
                    $archi = $env:PROCESSOR_ARCHITECTURE.ToLower().Replace('amd', 'x');
                    if ($archi -eq 'x64' -or 'arm64') {
                        Test-Path -Path 'Starward.exe';
                    }
                    return $false;
                }

                function DownloadResource {
                    param ([switch]$PreRelease)
                    $archi = $env:PROCESSOR_ARCHITECTURE.ToLower().Replace('amd', 'x');
                    if ($PreRelease) {
                        $url = """https://starward.scighost.com/metadata/v1/release_preview_$archi.json""";
                    }
                    else {
                        $url = """https://starward.scighost.com/metadata/v1/release_stable_$archi.json""";
                    }
                    $json = ConvertFrom-Json (Invoke-WebRequest $url -UseDefaultCredentials);
                    foreach ($file in $json.SeparateFiles) {
                        if (Test-Path $file.Path) {
                            if ((Get-FileHash -Path $file.Path -Algorithm SHA256).Hash -eq $file.Hash) {
                                continue;
                            }
                        }
                        $url = $json.SeparatePrefix + $file.Hash;
                        Write-Host """[Downloading] $($file.Path)""" -ForegroundColor DarkGray;
                        $null = New-Item $file.Path -ItemType File -Force;
                        Invoke-WebRequest -Uri $url -OutFile $file.Path -UseDefaultCredentials;
                    }
                    Write-Host """Starward has been updated to $($json.Version)""" -ForegroundColor Green;
                }


                Write-Host 'Starward - Game Launcher for miHoYo';
                Write-Host 'https://github.com/Scighost/Starward';
                Write-Host '------------------------------------';
                Start-Sleep -Seconds 1;

                if (CheckResource) {
                    DownloadResource{{(EnablePreviewRelease ? " -PreRelease" : "")}};
                    Start-Process -FilePath 'Starward.exe';
                }
                else {
                    Write-Host 'App channel does not support repair.' -ForegroundColor Red;
                }

                Write-Host 'Press enter key to exit...';
                $null = Read-Host;
                """";
                string? dir = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('/', '\\'));
                Process.Start(new ProcessStartInfo
                {
                    FileName = "PowerShell",
                    Arguments = script,
                    WorkingDirectory = dir,
                    UseShellExecute = true,
                });
                Environment.Exit(0);
            }
            else
            {
                await Launcher.LaunchUriAsync(new Uri("https://github.com/Scighost/Starward/releases"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Start PowerShell to repair app.");
            NotificationBehavior.Instance.Error("Start PowerShell to repair app.", ex.Message);
        }
    }





}
