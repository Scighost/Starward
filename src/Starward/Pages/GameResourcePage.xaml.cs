using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Core.Launcher;
using Starward.Helpers;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class GameResourcePage : PageBase
{

    private readonly ILogger<GameResourcePage> _logger = AppConfig.GetLogger<GameResourcePage>();

    private readonly GameResourceService _gameResourceService = AppConfig.GetService<GameResourceService>();


    public GameResourcePage()
    {
        this.InitializeComponent();
    }



    public string GameServerName => $"{CurrentGameBiz.ToGameName()} - {CurrentGameBiz.ToGameServer()}";



    private GamePackagesWrapper launcherGameResource;

    private GameSDK? launcherGameSdk;

    [ObservableProperty]
    private string latestVersion;

    [ObservableProperty]
    private List<PackageGroup> latestPackageGroups;

    [ObservableProperty]
    private string preInstallVersion;

    [ObservableProperty]
    private List<PackageGroup> preInstallPackageGroups;



    protected override async void OnLoaded()
    {
        try
        {
            launcherGameResource = await _gameResourceService.GetGameResourceAsync(CurrentGameBiz);
            launcherGameSdk = await _gameResourceService.GetGameSdkAsync(CurrentGameBiz);
            LatestVersion = launcherGameResource.Main.Major.Version;
            var list = GetGameResourcePackageGroups(launcherGameResource.Main);
            if (CurrentGameBiz.IsBilibiliServer() && launcherGameSdk is not null)
            {
                list.Add(new PackageGroup
                {
                    Name = "Bilibili SDK",
                    Items = [new PackageItem
                    {
                        FileName = Path.GetFileName(launcherGameSdk.Pkg.Url),
                        Url = launcherGameSdk.Pkg.Url,
                        Md5 = launcherGameSdk.Pkg.Md5,
                        PackageSize = launcherGameSdk.Pkg.Size,
                        DecompressSize = launcherGameSdk.Pkg.DecompressedSize,
                    }],
                });
            }
            LatestPackageGroups = list;
            if (launcherGameResource.PreDownload.Major is not null )
            {
                PreInstallVersion = launcherGameResource.PreDownload.Major.Version;
                PreInstallPackageGroups = GetGameResourcePackageGroups(launcherGameResource.PreDownload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game resource failed, gameBiz: {gameBiz}", CurrentGameBiz);
        }
    }




    private List<PackageGroup> GetGameResourcePackageGroups(GameBranch gameResource)
    {
        var list = new List<PackageGroup>();
        var fullPackageGroup = new PackageGroup
        {
            Name = Lang.GameResourcePage_FullPackages,
            Items = new List<PackageItem>()
        };
        if (gameResource.Major.GamePkgs.Count > 1)
        {
            // segment
            if (gameResource.Major.GamePkgs.FirstOrDefault() is GamePkg first)
            {
                fullPackageGroup.Items.Add(new PackageItem
                {
                    FileName = $"{Path.GetFileNameWithoutExtension(first.Url)}.*",
                    PackageSize = gameResource.Major.GamePkgs.Sum(x => x.Size),
                    DecompressSize = gameResource.Major.GamePkgs.Sum(x => x.DecompressedSize),
                });
            }
            foreach (var segment in gameResource.Major.GamePkgs)
            {
                fullPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(segment.Url),
                    Url = segment.Url,
                    Md5 = segment.Md5,
                    PackageSize = segment.Size,
                });
            }
        }
        else
        {
            // no segment
            var latest = gameResource.Major.GamePkgs.First();
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(latest.Url),
                Url = latest.Url,
                Md5 = latest.Md5,
                PackageSize = latest.Size,
                DecompressSize = latest.DecompressedSize,
            });
        }
        foreach (var voice in gameResource.Major.AudioPkgs)
        {
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(voice.Url),
                Url = voice.Url,
                Md5 = voice.Md5,
                PackageSize = voice.Size,
                DecompressSize = voice.DecompressedSize,
            });
        }
        list.Add(fullPackageGroup);
        foreach (var diff in gameResource.Patches)
        {
            var diffPackageGroup = new PackageGroup
            {
                Name = $"{Lang.GameResourcePage_DiffPackages}  {diff.Version}",
                Items = new List<PackageItem>()
            };
            foreach (var pkg in diff.GamePkgs)
            {
                diffPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(pkg.Url),
                    Url = pkg.Url,
                    Md5 = pkg.Md5,
                    PackageSize = pkg.Size,
                    DecompressSize = pkg.DecompressedSize,
                });
            }
            foreach (var voice in diff.AudioPkgs)
            {
                diffPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(voice.Url),
                    Url = voice.Url,
                    Md5 = voice.Md5,
                    PackageSize = voice.Size,
                    DecompressSize = voice.DecompressedSize,
                });
            }
            list.Add(diffPackageGroup);
        }
        return list;
    }






    private async void Button_CopyUrl_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        try
        {
            if (sender is Button button)
            {
                if (button.DataContext is PackageGroup group)
                {
                    if (group.Items is not null)
                    {
                        var sb = new StringBuilder();
                        foreach (var item in group.Items)
                        {
                            if (!string.IsNullOrEmpty(item.Url))
                            {
                                sb.AppendLine(item.Url);
                            }
                        }
                        string url = sb.ToString();
                        if (!string.IsNullOrWhiteSpace(url))
                        {
                            ClipboardHelper.SetText(url);
                            await CopySuccessAsync(button);
                        }
                    }
                }
                if (button.DataContext is PackageItem package)
                {
                    if (!string.IsNullOrEmpty(package.Url))
                    {
                        ClipboardHelper.SetText(package.Url);
                        await CopySuccessAsync(button);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy url failed");
        }
    }



    private async Task CopySuccessAsync(Button button)
    {
        try
        {
            button.IsEnabled = false;
            if (button.Content is FontIcon icon)
            {
                // Accpet
                icon.Glyph = "\uF78C";
                await Task.Delay(1000);
            }
        }
        finally
        {
            button.IsEnabled = true;
            if (button.Content is FontIcon icon)
            {
                // Link
                icon.Glyph = "\uE71B";
            }
        }
    }



    [RelayCommand]
    private void Close()
    {
        MainWindow.Current.CloseOverlayPage();
    }




    public partial class PackageGroup
    {
        public string Name { get; set; }

        public List<PackageItem> Items { get; set; }
    }



    public class PackageItem
    {
        public string FileName { get; set; }

        public string Url { get; set; }

        public string Md5 { get; set; }

        public long PackageSize { get; set; }

        public long DecompressSize { get; set; }

        public string PackageSizeString => GetSizeString(PackageSize);

        public string DecompressSizeString => GetSizeString(DecompressSize);

        private string GetSizeString(long size)
        {
            const double KB = 1 << 10;
            const double MB = 1 << 20;
            const double GB = 1 << 30;
            if (size >= GB)
            {
                return $"{size / GB:F2} GB";
            }
            else if (size >= MB)
            {
                return $"{size / MB:F2} MB";
            }
            else
            {
                return $"{size / KB:F2} KB";
            }
        }
    }


}
