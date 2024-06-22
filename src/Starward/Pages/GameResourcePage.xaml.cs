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



    private LauncherGameResource launcherGameResource;

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
            LatestVersion = launcherGameResource.Game.Latest.Version;
            var list = GetGameResourcePackageGroups(launcherGameResource.Game);
            if (CurrentGameBiz.IsBilibiliServer() && launcherGameResource.Sdk is not null)
            {
                list.Add(new PackageGroup
                {
                    Name = "Bilibili SDK",
                    Items = [new PackageItem
                    {
                        FileName = Path.GetFileName(launcherGameResource.Sdk.Path),
                        Url = launcherGameResource.Sdk.Path,
                        Md5 = launcherGameResource.Sdk.Md5,
                        PackageSize = launcherGameResource.Sdk.PackageSize,
                        DecompressSize = launcherGameResource.Sdk.Size,
                    }],
                });
            }
            LatestPackageGroups = list;
            if (launcherGameResource.PreDownloadGame is not null)
            {
                PreInstallVersion = launcherGameResource.PreDownloadGame.Latest.Version;
                PreInstallPackageGroups = GetGameResourcePackageGroups(launcherGameResource.PreDownloadGame);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game resource failed, gameBiz: {gameBiz}", CurrentGameBiz);
        }
    }




    private List<PackageGroup> GetGameResourcePackageGroups(GameResource gameResource)
    {
        var list = new List<PackageGroup>();
        var fullPackageGroup = new PackageGroup
        {
            Name = Lang.GameResourcePage_FullPackages,
            Items = new List<PackageItem>()
        };
        if (string.IsNullOrWhiteSpace(gameResource.Latest.Path))
        {
            // segment
            if (gameResource.Latest.Segments.FirstOrDefault() is Segment first)
            {
                fullPackageGroup.Items.Add(new PackageItem
                {
                    FileName = $"{Path.GetFileNameWithoutExtension(first.Path)}.*",
                    Md5 = gameResource.Latest.Md5,
                    PackageSize = gameResource.Latest.PackageSize,
                    DecompressSize = gameResource.Latest.Size,
                });
            }
            foreach (var segment in gameResource.Latest.Segments)
            {
                fullPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(segment.Path),
                    Url = segment.Path,
                    Md5 = segment.Md5,
                    PackageSize = segment.PackageSize,
                });
            }
        }
        else
        {
            // no segment
            var latest = gameResource.Latest;
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(latest.Path),
                Url = latest.Path,
                Md5 = latest.Md5,
                PackageSize = latest.PackageSize,
                DecompressSize = latest.Size,
            });
        }
        foreach (var voice in gameResource.Latest.VoicePacks)
        {
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(voice.Path),
                Url = voice.Path,
                Md5 = voice.Md5,
                PackageSize = voice.PackageSize,
                DecompressSize = voice.Size,
            });
        }
        list.Add(fullPackageGroup);
        foreach (var diff in gameResource.Diffs)
        {
            var diffPackageGroup = new PackageGroup
            {
                Name = $"{Lang.GameResourcePage_DiffPackages}  {diff.Version}",
                Items = new List<PackageItem>()
            };
            diffPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(diff.Path),
                Url = diff.Path,
                Md5 = diff.Md5,
                PackageSize = diff.PackageSize,
                DecompressSize = diff.Size,
            });
            foreach (var voice in diff.VoicePacks)
            {
                diffPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(voice.Path),
                    Url = voice.Path,
                    Md5 = voice.Md5,
                    PackageSize = voice.PackageSize,
                    DecompressSize = voice.Size,
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
