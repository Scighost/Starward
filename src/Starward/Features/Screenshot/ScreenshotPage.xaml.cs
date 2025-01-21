using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Controls;
using Starward.Core;
using Starward.Features.GameLauncher;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Screenshot;

public sealed partial class ScreenshotPage : PageBase
{


    private readonly ILogger<ScreenshotPage> _logger = AppService.GetLogger<ScreenshotPage>();


    private readonly GameLauncherService _gameLauncherService = AppService.GetService<GameLauncherService>();




    public ScreenshotPage()
    {
        this.InitializeComponent();
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Image_Emoji.Source = CurrentGameBiz.ToGame().Value switch
        {
            GameBiz.hk4e => new BitmapImage(AppConfig.EmojiPaimon),
            GameBiz.hkrpg => new BitmapImage(AppConfig.EmojiPom),
            GameBiz.bh3 => new BitmapImage(AppConfig.EmojiAI),
            GameBiz.nap => new BitmapImage(AppConfig.EmojiBangboo),
            _ => null,
        };
    }



    protected override async void OnLoaded()
    {
        await Task.Delay(16);
        Initialize();
    }



    protected override void OnUnloaded()
    {
        Watcher?.Dispose();
    }





    [ObservableProperty]
    private ScreenshotWatcher? watcher;


    private void Initialize()
    {
        try
        {
            if (Watcher == null)
            {
                var folder = GetGameScreenshotPath();
                if (folder != null)
                {
                    _logger.LogInformation("Screenshot folder is {folder}", folder);
                    Watcher = new ScreenshotWatcher(folder);
                }
                else
                {
                    _logger.LogWarning("Cannot find screenshot folder");
                    StackPanel_Emoji.Visibility = Visibility.Visible;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize");
        }
    }



    private async void Button_CopyImage_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            if (button.DataContext is ScreenshotItem item)
            {
                try
                {
                    _logger.LogInformation("Copy image: {file}", item.FullName);
                    var file = await StorageFile.GetFileFromPathAsync(item.FullName);
                    ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
                    button.Content = new FontIcon { Glyph = "\uE8FB", FontSize = 16 };
                    await Task.Delay(3000);
                    button.Content = new FontIcon { Glyph = "\uE8C8", FontSize = 16 };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Copy image");
                }
            }
        }
    }





    [RelayCommand]
    private async Task OpenScreenshotFolderAsync()
    {
        try
        {
            var folder = GetGameScreenshotPath();
            if (folder != null)
            {
                _logger.LogInformation("Open folder: {folder}", folder);
                await Launcher.LaunchFolderPathAsync(folder);
            }
        }
        catch { }
    }



    [RelayCommand]
    private void BackupScreenshots()
    {
        try
        {
            var folder = GetGameScreenshotPath();
            if (folder != null)
            {
                int count = 0;
                var files = Directory.GetFiles(folder);
                var targetDir = Path.Combine(AppSetting.UserDataFolder!, "Screenshots", CurrentGameBiz);
                Directory.CreateDirectory(targetDir);
                foreach (var item in files)
                {
                    var target = Path.Combine(targetDir, Path.GetFileName(item));
                    if (!File.Exists(target))
                    {
                        File.Copy(item, target);
                        count++;
                    }
                }
                InAppToast.MainWindow?.Success(null, string.Format(Lang.ScreenshotPage_BackedUpNewScreenshots, count));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Backup screenshots");
        }
    }



    [RelayCommand]
    private async Task OpenBackupFolderAsync()
    {
        try
        {
            var folder = Path.Combine(AppSetting.UserDataFolder!, "Screenshots", CurrentGameBiz.ToString());
            Directory.CreateDirectory(folder);
            await Launcher.LaunchFolderPathAsync(folder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open backup folder");
        }
    }



    private void GridView_Images_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is ScreenshotItem item && Watcher is not null)
            {
                var list = Watcher.ImageList.ToList();
                new ImageViewWindow { CurrentImage = item, ImageCollection = list, }.Activate();
            }
        }
        catch { }
    }



    private void Grid_ImageItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement grid && grid.DataContext is ScreenshotItem item && Watcher is not null)
            {
                var list = Watcher.ImageList.ToList();
                new ImageViewWindow { CurrentImage = item, ImageCollection = list, }.Activate();
            }
        }
        catch { }
    }



    private async void Grid_ImageItem_DragStarting(UIElement sender, DragStartingEventArgs args)
    {
        try
        {
            if (sender is FrameworkElement grid && grid.DataContext is ScreenshotItem item)
            {
                var deferral = args.GetDeferral();
                args.AllowedOperations = DataPackageOperation.Copy;
                var file = await StorageFile.GetFileFromPathAsync(item.FullName);
                args.Data.SetStorageItems([file], true);
                string thumbnail = await CachedImage.GetImageThumbnailAsync(item.FullName);
                var bitmap = new BitmapImage(new Uri(thumbnail));
                bitmap.DecodePixelHeight = (int)(grid.ActualHeight * this.XamlRoot.GetUIScaleFactor());
                args.DragUI.SetContentFromBitmapImage(bitmap);
                deferral.Complete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drag image starting");
        }
    }





    public string? GetGameScreenshotPath()
    {
        string? folder = null;

        folder = _gameLauncherService.GetGameInstallPath(CurrentGameId);
        var relativePath = CurrentGameBiz.Game switch
        {
            GameBiz.hk4e => "ScreenShot",
            GameBiz.hkrpg => @"StarRail_Data\ScreenShots",
            GameBiz.bh3 => @"ScreenShot",
            GameBiz.nap => @"ScreenShot",
            _ => throw new ArgumentOutOfRangeException($"Unknown region {CurrentGameBiz}"),
        };
        folder = Path.Join(folder, relativePath);
        if (Directory.Exists(folder))
        {
            return Path.GetFullPath(folder);
        }
        else
        {
            return null;
        }
    }





}
