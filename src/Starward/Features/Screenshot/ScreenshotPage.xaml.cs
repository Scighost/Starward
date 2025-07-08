using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Starward.Controls;
using Starward.Core;
using Starward.Features.GameLauncher;
using Starward.Features.HoYoPlay;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Screenshot;

public sealed partial class ScreenshotPage : PageBase
{


    private readonly ILogger<ScreenshotPage> _logger = AppConfig.GetLogger<ScreenshotPage>();


    private readonly HoYoPlayService _hoyoplayService = AppConfig.GetService<HoYoPlayService>();




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
        await InitializeAsync();
    }



    protected override void OnUnloaded()
    {
        try
        {
            foreach (var item in _watchers)
            {
                item.Dispose();
            }
            _watchers.Clear();
            _folders.Clear();
            _folders = null!;
            _screenshotDict.Clear();
            _screenshotDict = null!;
            Screenshots = null!;
            _defaultScrollController = null!;
            _detailLabelToolTip?.Content = null;
            _detailLabelToolTip?.IsOpen = false;
            _detailLabelToolTip = null;
            AnnotatedScrollBar.Labels.Clear();
            ItemsView_Images.Loaded -= ItemsView_Images_Loaded;
            AnnotatedScrollBar.Loaded -= AnnotatedScrollBar_Loaded;
            AnnotatedScrollBar.DetailLabelRequested -= AnnotatedScrollBar_DetailLabelRequested;
            AnnotatedScrollBar.PointerEntered -= AnnotatedScrollBar_PointerEntered;
            AnnotatedScrollBar.PointerExited -= AnnotatedScrollBar_PointerExited;
            AnnotatedScrollBar.PointerPressed -= AnnotatedScrollBar_PointerPressed;
            AnnotatedScrollBar.PointerReleased -= AnnotatedScrollBar_PointerReleased;
            AnnotatedScrollBar = null;
        }
        catch { }

    }



    private List<FileSystemWatcher> _watchers = new();

    private List<ScreenshotFolder> _folders = new();

    private Dictionary<string, ScreenshotItem> _screenshotDict = new();

    public ObservableCollection<ScreenshotItem> Screenshots { get; set => SetProperty(ref field, value); }


    private IScrollController _defaultScrollController;



    private async Task InitializeAsync()
    {
        try
        {
            foreach (var item in _watchers)
            {
                item.Dispose();
            }
            _folders.Clear();
            _screenshotDict.Clear();
            Screenshots = null!;

            string? backupFolder = AppConfig.ScreenshotFolder;
            if (Directory.Exists(backupFolder))
            {
                string folder = Path.GetFullPath(Path.Join(backupFolder, CurrentGameBiz.Game));
                if (_folders.FirstOrDefault(x => x.Folder == folder) is null)
                {
                    var watcher = CreateFileSystemWatcher(folder);
                    _watchers.Add(watcher);
                    _folders.Add(new(folder) { Backup = true });
                }
            }
            else
            {
                backupFolder = Path.Join(AppConfig.UserDataFolder, "Screenshots", CurrentGameBiz.Game);
                if (Directory.Exists(backupFolder))
                {
                    string folder = Path.GetFullPath(backupFolder);
                    if (_folders.FirstOrDefault(x => x.Folder == folder) is null)
                    {
                        var watcher = CreateFileSystemWatcher(folder);
                        _watchers.Add(watcher);
                        _folders.Add(new(folder) { Backup = true });
                    }
                }
            }

            string? inGameFolder = await GetGameScreenshotPathAsync();
            if (Directory.Exists(inGameFolder))
            {
                string folder = Path.GetFullPath(inGameFolder);
                if (_folders.FirstOrDefault(x => x.Folder == folder) is null)
                {
                    var watcher = CreateFileSystemWatcher(folder);
                    _watchers.Add(watcher);
                    _folders.Add(new(folder) { InGame = true });
                }
            }

            string? externalFolder = AppConfig.GetExternalScreenshotFolder(CurrentGameBiz);
            if (!string.IsNullOrWhiteSpace(externalFolder))
            {
                foreach (var item in externalFolder.Split(';'))
                {
                    string folder = item.Trim();
                    if (Directory.Exists(folder))
                    {
                        folder = Path.GetFullPath(folder);
                        if (_folders.FirstOrDefault(x => x.Folder == folder) is null)
                        {
                            var watcher = CreateFileSystemWatcher(folder);
                            _watchers.Add(watcher);
                            _folders.Add(new(folder));
                        }
                    }
                }
            }

            List<ScreenshotItem> screenshots = new();
            foreach (var folderItem in _folders)
            {
                var files = Directory.GetFiles(folderItem.Folder);
                foreach (var file in files)
                {
                    string name = Path.GetFileName(file);
                    if (_screenshotDict.ContainsKey(name))
                    {
                        continue;
                    }
                    if (IsSupportedExtension(file) && !File.GetAttributes(file).HasFlag((System.IO.FileAttributes)0x440000))
                    {
                        var item = new ScreenshotItem(file);
                        screenshots.Add(item);
                        _screenshotDict[name] = item;
                    }
                }
            }

            var list = screenshots.OrderByDescending(x => x.CreationTime).ToList();
            Screenshots = new(list);
            await Task.Delay(100);
            UpdateLabels();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize");
        }
    }



    private FileSystemWatcher CreateFileSystemWatcher(string folder)
    {
        var watcher = new FileSystemWatcher(folder);
        watcher.NotifyFilter = NotifyFilters.FileName;
        watcher.Filters.Add("*.jpg");
        watcher.Filters.Add("*.png");
        watcher.Filters.Add("*.jxr");
        watcher.Created += FileSystemWatcher_Created;
        watcher.Deleted += FileSystemWatcher_Deleted;
        watcher.EnableRaisingEvents = true;
        return watcher;
    }



    public async Task<string?> GetGameScreenshotPathAsync()
    {
        try
        {
            string? folder = GameLauncherService.GetGameInstallPath(CurrentGameId);
            var relativePath = CurrentGameBiz.Game switch
            {
                GameBiz.hk4e => "ScreenShot",
                GameBiz.hkrpg => @"StarRail_Data\ScreenShots",
                GameBiz.bh3 => @"ScreenShot",
                GameBiz.nap => @"ScreenShot",
                _ => (await _hoyoplayService.GetGameConfigAsync(CurrentGameId))?.GameScreenshotDir,
            };
            folder = Path.Join(folder, relativePath);
            if (Directory.Exists(folder))
            {
                return Path.GetFullPath(folder);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Get game screenshot path");
        }
        return null;
    }



    public static bool IsSupportedExtension(string file)
    {
        return Path.GetExtension(file) is ".jpg" or ".png" or ".jxr";
    }



    private void ItemsView_Images_Loaded(object sender, RoutedEventArgs e)
    {
        _defaultScrollController ??= ItemsView_Images.VerticalScrollController;
    }



    private void AnnotatedScrollBar_DetailLabelRequested(AnnotatedScrollBar sender, AnnotatedScrollBarDetailLabelRequestedEventArgs args)
    {
        try
        {
            if (Screenshots is not null)
            {
                double offset = 0;
                double viewportHeight = ItemsView_Images.ScrollView.ViewportHeight;
                double extentHeight = ItemsView_Images.ScrollView.ExtentHeight;
                if (args.ScrollOffset < viewportHeight / 2 || args.ScrollOffset > extentHeight - viewportHeight / 2)
                {
                    offset = args.ScrollOffset / ItemsView_Images.ScrollView.ExtentHeight;
                }
                else
                {
                    offset = (args.ScrollOffset + viewportHeight / 2) / ItemsView_Images.ScrollView.ExtentHeight;
                }
                if (offset < 0 || offset > 1)
                {
                    args.Content = null;
                    _detailLabelToolTip?.IsOpen = false;
                }
                else
                {
                    int index = (int)Math.Ceiling(offset * (Screenshots.Count - 1));
                    index = Math.Clamp(index, 0, Screenshots.Count - 1);
                    args.Content = Screenshots[index].CreationTime.ToString("yyyy-MM-dd");
                }
            }
        }
        catch { }
    }



    private void UpdateLabels()
    {
        try
        {
            if (Screenshots?.Count > 0)
            {
                double count = Screenshots.Count;
                if (ItemsView_Images.ScrollView.ExtentHeight / ItemsView_Images.ScrollView.ViewportHeight > 3)
                {
                    AnnotatedScrollBar.Labels.Clear();
                    double extentHeight = ItemsView_Images.ScrollView.ExtentHeight;

                    int lastYear = int.MinValue;
                    int lastYearMonth = int.MinValue;
                    var labels = new List<AnnotatedScrollBarLabel>();
                    for (int i = Screenshots.Count - 1; i >= 0; i--)
                    {
                        int year = Screenshots[i].CreationTime.Year;
                        int yearMonth = year * 100 + Screenshots[i].CreationTime.Month;
                        if (year > lastYear)
                        {
                            double offset = extentHeight * i / count;
                            labels.Add(new AnnotatedScrollBarLabel(year.ToString(), offset));
                            lastYear = year;
                            lastYearMonth = yearMonth;
                        }
                        else if (yearMonth > lastYearMonth)
                        {
                            double offset = extentHeight * i / count;
                            lastYearMonth = yearMonth;
                            labels.Add(new AnnotatedScrollBarLabel("•", offset));
                        }
                    }

                    double lastOffset = double.MaxValue;
                    bool lastIsYear = false;
                    foreach (var item in labels)
                    {
                        if (item.Content is not "•")
                        {
                            AnnotatedScrollBar.Labels.Insert(0, item);
                            lastOffset = item.ScrollOffset;
                            lastIsYear = true;
                        }
                        else if (lastIsYear)
                        {
                            if (Math.Abs(item.ScrollOffset - lastOffset) / extentHeight >= 0.04)
                            {
                                AnnotatedScrollBar.Labels.Insert(0, item);
                                lastOffset = item.ScrollOffset;
                                lastIsYear = false;
                            }
                        }
                        else
                        {
                            AnnotatedScrollBar.Labels.Insert(0, item);
                            lastOffset = item.ScrollOffset;
                        }
                    }

                    AnnotatedScrollBar.Opacity = 1;
                    AnnotatedScrollBar.IsHitTestVisible = true;
                    AnnotatedScrollBar.Visibility = Visibility.Visible;
                    ItemsView_Images.VerticalScrollController = AnnotatedScrollBar.ScrollController;
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Update labels");
        }
        if (_defaultScrollController is not null)
        {
            AnnotatedScrollBar.Visibility = Visibility.Collapsed;
            ItemsView_Images.VerticalScrollController = _defaultScrollController;
        }
    }



    private async void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                string name = Path.GetFileName(e.FullPath);
                if (_screenshotDict.ContainsKey(name))
                {
                    return;
                }
                if (IsSupportedExtension(e.FullPath) && File.Exists(e.FullPath))
                {
                    await WaitForFileReleaseAsync(e.FullPath, CancellationToken.None);
                    var item = new ScreenshotItem(e.FullPath);
                    _screenshotDict[name] = item;
                    DispatcherQueue.TryEnqueue(() =>
                    {
                        Screenshots ??= new();
                        Screenshots.Insert(0, item);
                        UpdateLabels();
                    });
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File system watcher created event");
        }
    }



    private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (Screenshots?.FirstOrDefault(x => x.FullName == e.FullPath) is ScreenshotItem item)
            {
                _screenshotDict.Remove(Path.GetFileName(e.FullPath));
                DispatcherQueue.TryEnqueue(() => Screenshots?.Remove(item));
                UpdateLabels();
            }
        }
        catch { }
    }



    private static async Task WaitForFileReleaseAsync(string filePath, CancellationToken cancellation = default)
    {
        int count = 0;
        while (count < 30)
        {
            using var handle = Kernel32.CreateFile2(filePath, Kernel32.FileAccess.GENERIC_READ, 0, Kernel32.CreationOption.OPEN_EXISTING);
            if (handle.IsNull || handle.IsInvalid)
            {
                await Task.Delay(100, cancellation);
                count++;
                continue;
            }
            break;
        }
    }




    #region Action


    [RelayCommand]
    private async Task ManageScreenshotFolderAsync()
    {
        try
        {
            var dialog = new ScreenshotFolderManageDialog
            {
                Folders = _folders,
                CurrentGameId = this.CurrentGameId,
                XamlRoot = this.XamlRoot,
            };
            await dialog.ShowAsync();
            if (dialog.FolderChanged)
            {
                string folder = string.Join(';', dialog.Folders.Where(x => x.CanRemove).Select(x => x.Folder));
                AppConfig.SetExternalScreenshotFolder(CurrentGameBiz, folder);
                await InitializeAsync();
            }
        }
        catch { }
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



    private void Grid_ImageItem_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement grid && grid.DataContext is ScreenshotItem item)
            {
                _ = new ImageViewWindow2().ShowWindowAsync(this.XamlRoot.ContentIslandEnvironment.AppWindowId, item, Screenshots);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open ImageViewWindow");
        }
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
                var bitmap = CachedImage.GetCachedThumbnail(item.FullName);
                if (bitmap is not null)
                {
                    bitmap.DecodePixelHeight = (int)(grid.ActualHeight * this.XamlRoot.GetUIScaleFactor());
                    args.DragUI.SetContentFromBitmapImage(bitmap);
                }
                deferral.Complete();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drag image starting");
        }
    }






    #endregion




    #region AnnotatedScrollBar.DetailLabelToolTip

    // https://github.com/microsoft/microsoft-ui-xaml/issues/9726


    private ToolTip? _detailLabelToolTip;

    private bool _isPointerHovered;

    private bool _isPointerPressed;


    private void AnnotatedScrollBar_Loaded(object sender, RoutedEventArgs e)
    {
        if (AnnotatedScrollBar.FindDescendant("PART_ToolTipRail") is DependencyObject border)
        {
            if (ToolTipService.GetToolTip(border) is ToolTip toolTip)
            {
                _detailLabelToolTip = toolTip;
            }
        }
    }



    private void AnnotatedScrollBar_PointerEntered(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isPointerHovered = true;
    }


    private void AnnotatedScrollBar_PointerExited(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isPointerPressed)
        {
            _detailLabelToolTip?.IsOpen = false;
        }
        _isPointerHovered = false;
    }


    private void AnnotatedScrollBar_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _isPointerPressed = true;
    }

    private void AnnotatedScrollBar_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (!_isPointerHovered)
        {
            _detailLabelToolTip?.IsOpen = false;
        }
        _isPointerPressed = false;
    }






    #endregion




}
