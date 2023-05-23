using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Data;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace Starward.Models;

public class ScreenshotWatcher : ObservableObject, IDisposable
{

    private readonly ILogger<ScreenshotWatcher> _logger = AppConfig.GetLogger<ScreenshotWatcher>();


    private FileSystemWatcher _watcher;



    public ScreenshotWatcher(string folder)
    {
        _watcher = new FileSystemWatcher(folder);
        _watcher.Filter = "*.png";
        _watcher.Created += FileSystemWatcher_Created;
        _watcher.Deleted += FileSystemWatcher_Deleted;
        _watcher.EnableRaisingEvents = true;
        ImageList = new(new DirectoryInfo(folder).GetFiles("*.png").Select(x => new ScreenshotItem(x)).OrderByDescending(x => x.CreationTime));
        var queryGroup = ImageList.GroupBy(x => x.CreationTime.ToString("yyyy-MM")).Select(x => new ScreenshotItemGroup(x.Key, x)).OrderByDescending(x => x.Header);
        ImageGroupList = new(queryGroup);
        ImageViewSource = new CollectionViewSource { Source = ImageGroupList, IsSourceGrouped = true };
    }





    private ObservableCollection<ScreenshotItem> _ImageList;
    public ObservableCollection<ScreenshotItem> ImageList
    {
        get => _ImageList;
        set => SetProperty(ref _ImageList, value);
    }




    private ObservableCollection<ScreenshotItemGroup> _ImageGroupList;
    public ObservableCollection<ScreenshotItemGroup> ImageGroupList
    {
        get => _ImageGroupList;
        set => SetProperty(ref _ImageGroupList, value);
    }



    private CollectionViewSource _ImageViewSource;
    public CollectionViewSource ImageViewSource
    {
        get => _ImageViewSource;
        set => SetProperty(ref _ImageViewSource, value);
    }


    private ScreenshotItemGroup newImages;




    private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                _logger.LogInformation("File created: {file}", e.FullPath);
                var fileInfo = new FileInfo(e.FullPath);
                if (fileInfo.Exists)
                {
                    var item = new ScreenshotItem(fileInfo);
                    var dq = MainWindow.Current.DispatcherQueue;
                    if (newImages is null)
                    {
                        newImages = new("New", new[] { item });
                        dq.TryEnqueue(() => ImageGroupList?.Insert(0, newImages));
                    }
                    else
                    {
                        dq.TryEnqueue(() => newImages.Insert(0, item));
                    }
                    dq.TryEnqueue(() => ImageList.Insert(newImages.Count - 1, item));
                }
            }
        }
        catch { }
    }



    private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e)
    {
        try
        {
            if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                _logger.LogInformation("File deleted: {file}", e.FullPath);
                var path = Path.GetFullPath(e.FullPath);
                var dq = MainWindow.Current.DispatcherQueue;
                dq.TryEnqueue(() =>
                {
                    var file1 = newImages?.FirstOrDefault(x => x.FullName == path);
                    if (file1 is not null)
                    {
                        newImages?.Remove(file1);
                    }
                    var file2 = ImageList?.FirstOrDefault(x => x.FullName == path);
                    if (file2 is not null)
                    {
                        ImageList?.Remove(file2);
                    }
                    foreach (var item in ImageGroupList)
                    {
                        var file3 = item.FirstOrDefault(x => x.FullName == path);
                        if (file3 is not null)
                        {
                            item.Remove(file3);
                            break;
                        }
                    }
                });
            }
        }
        catch { }
    }




    public void Dispose()
    {
        ((IDisposable)_watcher).Dispose();
    }

}
