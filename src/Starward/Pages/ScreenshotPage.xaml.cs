// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Core;
using Starward.Helpers;
using Starward.Models;
using Starward.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class ScreenshotPage : Page
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
        Image_Large.Width = this.ActualWidth;
        Image_Large.Height = this.ActualHeight;
        Initialize();
    }



    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        Image_Large.Width = this.ActualWidth;
        Image_Large.Height = this.ActualHeight;
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
                    ClipboardHelper.SetBitmap(file);
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




    #region Large Image




    private ScreenshotItem _selectItem;

    [ObservableProperty]
    private List<ScreenshotItem> _screenshotItems;

    [ObservableProperty]
    private int _selectIndex;


    private void GridView_Images_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is ScreenshotItem item && Watcher is not null)
            {
                _selectItem = item;
                ScreenshotItems = Watcher.ImageList.ToList();
                SelectIndex = ScreenshotItems.IndexOf(_selectItem) + 1;
                Image_Large.Source = new BitmapImage(new Uri(_selectItem.FullName));
                var ani = GridView_Images.PrepareConnectedAnimation("forwardAnimation", item, "Image_Thumb");
                ani.Configuration = new BasicConnectedAnimationConfiguration();
                ani.Completed += (_, _) => Grid_ImageView.IsHitTestVisible = true;
                Grid_ImageView.Opacity = 1;
                MainPage.Current.IsPaneToggleButtonVisible = false;
                ani.TryStart(Image_Large);
            }
        }
        catch { }
    }



    [RelayCommand]
    private async Task CloseViewAsync()
    {
        try
        {
            if (Watcher?.ImageList?.Contains(_selectItem) ?? false)
            {
                GridView_Images.ScrollIntoView(_selectItem, ScrollIntoViewAlignment.Default);
                GridView_Images.UpdateLayout();
                var ani = ConnectedAnimationService.GetForCurrentView().PrepareToAnimate("backwardAnimation", Image_Large);
                ani.Configuration = new BasicConnectedAnimationConfiguration();
                ani.Completed += (_, _) =>
                {
                    MainPage.Current.IsPaneToggleButtonVisible = true;
                    Grid_ImageView.IsHitTestVisible = false;
                };
                Grid_ImageView.Opacity = 0;
                await GridView_Images.TryStartConnectedAnimationAsync(ani, _selectItem, "Image_Thumb");
            }
            else
            {
                MainPage.Current.IsPaneToggleButtonVisible = true;
                Grid_ImageView.IsHitTestVisible = false;
                Grid_ImageView.Opacity = 0;
            }
        }
        catch { }
    }



    private CancellationTokenSource _tokenSource;

    private async void Image_Large_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            _tokenSource?.Cancel();
            _tokenSource = new CancellationTokenSource();
            var source = _tokenSource;
            int index = 0;
            if (e.GetCurrentPoint(this).Properties.MouseWheelDelta < 0)
            {
                // next
                index = (SelectIndex) % ScreenshotItems.Count;
            }
            else
            {
                // preview
                index = (SelectIndex - 2 + ScreenshotItems.Count) % ScreenshotItems.Count;
            }
            _selectItem = ScreenshotItems[index];
            SelectIndex = index + 1;
            var bitmap = new BitmapImage();
            using var fs = File.OpenRead(_selectItem.FullName);
            await bitmap.SetSourceAsync(fs.AsRandomAccessStream());
            if (source.IsCancellationRequested)
            {
                return;
            }
            Image_Large.Source = bitmap;
        }
        catch { }
    }





    #endregion




}
