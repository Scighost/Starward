using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Background;

public sealed partial class BackgroundViewPage : PageBase
{


    private readonly ILogger<BackgroundViewPage> _logger = AppConfig.GetLogger<BackgroundViewPage>();


    public BackgroundViewPage()
    {
        this.InitializeComponent();
    }


    public ObservableCollection<BackgroundFileItem> BackgroundImages { get; set => SetProperty(ref field, value); }


    public string SelectCountText { get; set => SetProperty(ref field, value); }


    public string DeleteInfoText { get; set => SetProperty(ref field, value); }


    public bool MutliSelect
    {
        get; set
        {
            field = value;
            GridView_Images.SelectionMode = value ? ListViewSelectionMode.Multiple : ListViewSelectionMode.None;
        }
    }




    protected override void OnLoaded()
    {
        LoadBackgroundItems();
    }


    protected override void OnUnloaded()
    {
        try
        {
            BackgroundImages?.Clear();
            BackgroundImages = null!;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }


    private void LoadBackgroundItems()
    {
        try
        {
            string folder = Path.Join(AppConfig.UserDataFolder, "bg");
            if (Directory.Exists(folder))
            {
                string[] files = Directory.GetFiles(folder);
                var result = files.Select(x => new BackgroundFileItem(x));
                if (RadioMenuFlyoutItem_Filter_Image.IsChecked)
                {
                    result = result.Where(x => !x.IsVideo);
                }
                if (RadioMenuFlyoutItem_Filter_Video.IsChecked)
                {
                    result = result.Where(x => x.IsVideo);
                }
                if (RadioMenuFlyoutItem_Sort_Time.IsChecked)
                {
                    result = result.OrderByDescending(x => x.CreationTime);
                }
                if (RadioMenuFlyoutItem_Sort_FileSize.IsChecked)
                {
                    result = result.OrderByDescending(x => x.FileSize);
                }
                BackgroundImages = new(result);
                UpdateSelectCountText();
            }
            else
            {
                StackPanel_NoFolder.Visibility = Visibility.Visible;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Load background items.");
        }
    }



    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        try
        {
            string folder = Path.Join(AppConfig.UserDataFolder, "bg");
            if (Directory.Exists(folder))
            {
                await Launcher.LaunchFolderPathAsync(folder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open background folder.");
        }
    }


    [RelayCommand]
    private async Task DeleteDuplicateBgAsync()
    {
        try
        {
            string folder = Path.Join(AppConfig.UserDataFolder, "bg");
            if (Directory.Exists(folder))
            {
                int count = 0;
                string[] files = Directory.GetFiles(folder);
                ConcurrentDictionary<string, bool> dict = new();
                await Parallel.ForEachAsync(files, async (file, _) =>
                {
                    using FileStream fs = File.Open(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
                    string hash = Convert.ToHexString(await MD5.HashDataAsync(fs));
                    if (dict.TryAdd(hash, true))
                    {
                        return;
                    }
                    fs.Dispose();
                    File.Delete(file);
                    Interlocked.Increment(ref count);
                });
                DeleteInfoText = string.Format(Lang.BackgroundViewPage_0DuplicateFileSHasBeenDeleted, count);
                LoadBackgroundItems();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Delete duplicate background files.");
        }
    }


    private void RadioMenuFlyoutItem_FilterSort_Click(object sender, RoutedEventArgs e)
    {
        LoadBackgroundItems();
    }



    private async void GridView_Images_ItemClick(object sender, ItemClickEventArgs e)
    {
        try
        {
            if (GridView_Images.SelectionMode is ListViewSelectionMode.None)
            {
                if (e.ClickedItem is BackgroundFileItem item)
                {
                    await Launcher.LaunchUriAsync(new(item.FilePath));
                }
            }
        }
        catch { }
    }


    private async void GridView_Images_DragItemsStarting(object sender, DragItemsStartingEventArgs e)
    {
        try
        {
            var list = new List<StorageFile>();
            foreach (var dragItem in e.Items)
            {
                if (dragItem is BackgroundFileItem item)
                {
                    if (File.Exists(item.FilePath))
                    {
                        var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        list.Add(file);
                    }
                }
            }
            if (list.Count > 0)
            {
                e.Data.RequestedOperation = DataPackageOperation.Copy;
                e.Data.SetStorageItems(list, true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drag image starting");
        }
    }



    private void GridView_Images_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        UpdateSelectCountText();
    }



    private void UpdateSelectCountText()
    {
        try
        {
            SelectCountText = $"{GridView_Images.SelectedItems.Count}/{BackgroundImages?.Count ?? 0}";
        }
        catch { }
    }



    private async void MenuFlyoutItem_Open_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { DataContext: BackgroundFileItem item })
            {
                await Launcher.LaunchUriAsync(new(item.FilePath));
            }
        }
        catch { }
    }


    private async void MenuFlyoutItem_CopyFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (GridView_Images.SelectionMode is ListViewSelectionMode.Multiple && GridView_Images.SelectedItems.Count > 0)
            {
                var list = new List<StorageFile>();
                foreach (BackgroundFileItem item in GridView_Images.SelectedItems.Cast<BackgroundFileItem>())
                {
                    if (File.Exists(item.FilePath))
                    {
                        var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        list.Add(file);
                    }
                }
                if (list.Count > 0)
                {
                    ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, list.ToArray());
                }
            }
            else if (sender is FrameworkElement fe && fe.DataContext is BackgroundFileItem item)
            {
                if (File.Exists(item.FilePath))
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                    ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file to clipboard");
        }
    }


    private async void MenuFlyoutItem_OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is BackgroundFileItem item)
            {
                if (File.Exists(item.FilePath))
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                    var options = new FolderLauncherOptions();
                    options.ItemsToSelect.Add(file);
                    await Launcher.LaunchFolderAsync(await file.GetParentAsync(), options);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in explorer");
        }
    }


    private async void MenuFlyoutItem_OpenWithDefault_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is BackgroundFileItem item)
            {
                if (File.Exists(item.FilePath))
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                    await Launcher.LaunchFileAsync(file);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file with default application");
        }
    }


    private async void MenuFlyoutItem_OpenWith_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement fe && fe.DataContext is BackgroundFileItem item)
            {
                if (File.Exists(item.FilePath))
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                    var options = new LauncherOptions { DisplayApplicationPicker = true };
                    await Launcher.LaunchFileAsync(file, options);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file with application picker");
        }
    }


    private async void MenuFlyoutItem_Delete_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (GridView_Images.SelectionMode is ListViewSelectionMode.Multiple && GridView_Images.SelectedItems.Count > 0)
            {
                var list = GridView_Images.SelectedItems.Cast<BackgroundFileItem>().ToList();
                foreach (BackgroundFileItem item in list)
                {
                    if (File.Exists(item.FilePath))
                    {
                        var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                        await file.DeleteAsync();
                    }
                    BackgroundImages?.Remove(item);
                }
            }
            else if (sender is FrameworkElement fe && fe.DataContext is BackgroundFileItem item)
            {
                if (File.Exists(item.FilePath))
                {
                    var file = await StorageFile.GetFileFromPathAsync(item.FilePath);
                    await file.DeleteAsync();
                }
                BackgroundImages?.Remove(item);
            }
            UpdateSelectCountText();
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogError(ex, "Failed to delete image file");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete image file");
        }
    }


}
