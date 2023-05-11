// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Scighost.WinUILib.Helpers;
using Starward.Core;
using Starward.Model;
using Starward.Service;
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

namespace Starward.UI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class ScreenshotPage : Page
{


    public ScreenshotPage()
    {
        this.InitializeComponent();
    }



    [ObservableProperty]
    private int serverIndex = AppConfig.GameServerIndex;
    partial void OnServerIndexChanged(int value)
    {
        Watcher?.Dispose();
        Watcher = null;
        Initialize();
    }


    [ObservableProperty]
    private ScreenshotWatcher? watcher;


    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        await Task.Delay(16);
        Image_Large.Width = this.ActualWidth;
        Image_Large.Height = this.ActualHeight;
        Initialize();
    }


    private void Initialize()
    {
        try
        {
            if (Watcher == null)
            {
                var path = GameService.GetGameInstallPath((RegionType)ServerIndex);
                var folder = Path.Join(path, @"StarRail_Data\ScreenShots");
                if (Directory.Exists(folder))
                {
                    Watcher = new ScreenshotWatcher(folder);
                }
            }
        }
        catch (Exception ex)
        {

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
                    var file = await StorageFile.GetFileFromPathAsync(item.FullName);
                    ClipboardHelper.SetBitmap(file);
                    button.Content = new FontIcon { Glyph = "\uE10B", FontSize = 16 };
                    await Task.Delay(3000);
                    button.Content = new FontIcon { Glyph = "\uE16F", FontSize = 16 };
                }
                catch (Exception ex)
                {

                }
            }
        }
    }





    [RelayCommand]
    private async Task OpenScreenshotFolderAsync()
    {
        try
        {
            var path = GameService.GetGameInstallPath((RegionType)ServerIndex);
            var folder = Path.Join(path, @"StarRail_Data\ScreenShots");
            if (Directory.Exists(folder))
            {
                await Launcher.LaunchFolderPathAsync(folder);
            }
        }
        catch (Exception ex)
        {

        }
    }


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
        catch (Exception ex)
        {

        }
    }



    [RelayCommand]
    private async Task CloseViewAsync()
    {
        try
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
        catch (Exception ex)
        {

        }
    }



    private CancellationTokenSource _tokenSource;

    private async void Image_Large_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            _tokenSource?.Cancel();
            _tokenSource = new CancellationTokenSource();
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
            if (_tokenSource.IsCancellationRequested)
            {
                return;
            }
            Image_Large.Source = bitmap;
        }
        catch (Exception ex)
        {

        }
    }

}
