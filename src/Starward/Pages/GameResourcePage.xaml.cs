using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Core.HoYoPlay;
using Starward.Helpers;
using Starward.Services.Launcher;
using System;
using System.Collections.Generic;
using System.IO;
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

    private readonly GamePackageService _gamePackageService = AppConfig.GetService<GamePackageService>();

    private readonly HoYoPlayClient _hoyoPlayClient = AppConfig.GetService<HoYoPlayClient>();


    public GameResourcePage()
    {
        this.InitializeComponent();
    }



    public string GameServerName => $"{CurrentGameBiz.ToGameName()} - {CurrentGameBiz.ToGameServerName()}";



    private GamePackage gamePackage;

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
            gamePackage = await _gamePackageService.GetGamePackageAsync(CurrentGameBiz);
            LatestVersion = gamePackage.Main.Major!.Version;
            var list = GetGameResourcePackageGroups(gamePackage.Main);
            if (CurrentGameBiz.IsBilibili())
            {
                var sdk = await _hoyoPlayClient.GetGameChannelSDKAsync(LauncherId.FromGameBiz(CurrentGameBiz)!, "", GameId.FromGameBiz(CurrentGameBiz)!);
                if (sdk is not null)
                {
                    list.Add(new PackageGroup
                    {
                        Name = "Bilibili SDK",
                        Items = [new PackageItem
                        {
                            FileName = Path.GetFileName(sdk.ChannelSDKPackage.Url),
                            Url = sdk.ChannelSDKPackage.Url,
                            Md5 = sdk.ChannelSDKPackage.MD5,
                            PackageSize = sdk.ChannelSDKPackage.Size,
                            DecompressSize = sdk.ChannelSDKPackage.DecompressedSize,
                        }],
                    });
                }
            }
            LatestPackageGroups = list;
            if (!string.IsNullOrWhiteSpace(gamePackage.PreDownload?.Major?.Version))
            {
                PreInstallVersion = gamePackage.PreDownload.Major.Version;
                PreInstallPackageGroups = GetGameResourcePackageGroups(gamePackage.PreDownload);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game resource failed, gameBiz: {gameBiz}", CurrentGameBiz);
        }
    }




    private List<PackageGroup> GetGameResourcePackageGroups(GamePackageVersion gameResource)
    {
        var list = new List<PackageGroup>();
        var fullPackageGroup = new PackageGroup
        {
            Name = Lang.GameResourcePage_FullPackages,
            Items = new List<PackageItem>()
        };
        foreach (var item in gameResource.Major?.GamePackages ?? [])
        {
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(item.Url),
                Url = item.Url,
                Md5 = item.MD5,
                PackageSize = item.Size,
                DecompressSize = item.DecompressedSize,
            });
        }
        foreach (var item in gameResource.Major?.AudioPackages ?? [])
        {
            fullPackageGroup.Items.Add(new PackageItem
            {
                FileName = Path.GetFileName(item.Url),
                Url = item.Url,
                Md5 = item.MD5,
                PackageSize = item.Size,
                DecompressSize = item.DecompressedSize,
            });
        }
        list.Add(fullPackageGroup);

        foreach (var patch in gameResource.Patches ?? [])
        {
            var diffPackageGroup = new PackageGroup
            {
                Name = $"{Lang.GameResourcePage_DiffPackages}  {patch.Version}",
                Items = new List<PackageItem>()
            };
            foreach (var item in patch.GamePackages ?? [])
            {
                diffPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(item.Url),
                    Url = item.Url,
                    Md5 = item.MD5,
                    PackageSize = item.Size,
                    DecompressSize = item.DecompressedSize,
                });
            }
            foreach (var item in patch.AudioPackages ?? [])
            {
                diffPackageGroup.Items.Add(new PackageItem
                {
                    FileName = Path.GetFileName(item.Url),
                    Url = item.Url,
                    Md5 = item.MD5,
                    PackageSize = item.Size,
                    DecompressSize = item.DecompressedSize,
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
                        string url = sb.ToString().TrimEnd();
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




    public class PackageGroup
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
