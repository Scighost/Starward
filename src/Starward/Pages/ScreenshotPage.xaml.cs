// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Helpers;
using Starward.Models;
using Starward.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class ScreenshotPage : PageBase
{


    private readonly ILogger<ScreenshotPage> _logger = AppConfig.GetLogger<ScreenshotPage>();


    private readonly GameService _gameService = AppConfig.GetService<GameService>();


    private GameBiz gameBiz;



    public ScreenshotPage()
    {
        this.InitializeComponent();
    }


    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is GameBiz biz)
        {
            gameBiz = biz;
            Image_Emoji.Source = gameBiz.ToGame() switch
            {
                GameBiz.GenshinImpact => new BitmapImage(AppConfig.EmojiPaimon),
                GameBiz.StarRail => new BitmapImage(AppConfig.EmojiPom),
                GameBiz.Honkai3rd => new BitmapImage(AppConfig.EmojiAI),
                _ => null,
            };
        }
    }



    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(16);
        Initialize();
    }



    private void Page_Unloaded(object sender, RoutedEventArgs e)
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
                var folder = _gameService.GetGameScreenshotPath(gameBiz);
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
            var folder = _gameService.GetGameScreenshotPath(gameBiz);
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
            var folder = _gameService.GetGameScreenshotPath(gameBiz);
            if (folder != null)
            {
                int count = 0;
                var files = Directory.GetFiles(folder);
                var targetDir = Path.Combine(AppConfig.UserDataFolder, "Screenshots", gameBiz.ToString());
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
                NotificationBehavior.Instance.Success(null, string.Format(Lang.ScreenshotPage_BackedUpNewScreenshots, count));
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
            var folder = Path.Combine(AppConfig.UserDataFolder, "Screenshots", gameBiz.ToString());
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
                MainWindow.Current.OverlayFrameNavigateTo(typeof(ImageViewPage), (item, list));
            }
        }
        catch { }
    }



}
