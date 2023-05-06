// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Scighost.WinUILib.Helpers;
using Starward.Core.Gacha;
using Starward.Models;
using System;
using System.IO;
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

    private FileSystemWatcher _watcher;


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


    private void Page_Loaded(object sender, RoutedEventArgs e)
    {
        Initialize();
    }


    private void Initialize()
    {
        try
        {
            if (Watcher == null)
            {
                var path = GachaLogClient.GetGameInstallPathFromRegistry(ServerIndex);
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



    private void GridView_Images_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (e.ClickedItem is ScreenshotItem item)
            {
                //var list = ImageFolderSource.ImageList.Select(x => WallpaperInfoEx.FromUri(x.FullName)).ToList();
                //var current = list.FirstOrDefault(x => x.Url == item.FullName);
                //if (current != null)
                //{
                //    MainWindow.Current.SetFullWindowContent(new ImageViewer { CurrentImage = current, ImageCollection = list });
                //}
            }
        }
        catch (Exception ex)
        {

        }
    }



    [RelayCommand]
    private async Task OpenScreenshotFolderAsync()
    {
        try
        {
            var path = GachaLogClient.GetGameInstallPathFromRegistry(ServerIndex);
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


}
