using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core.HoYoPlay;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;


namespace Starward.Features.Screenshot;

[INotifyPropertyChanged]
public sealed partial class ScreenshotFolderManageDialog : ContentDialog
{


    private ILogger<ScreenshotFolderManageDialog> _logger = AppConfig.GetLogger<ScreenshotFolderManageDialog>();


    public GameId CurrentGameId { get; set; }


    public List<ScreenshotFolder> Folders { get; set; }





    public ScreenshotFolderManageDialog()
    {
        InitializeComponent();
        Loaded += ScreenshotFolderManageDialog_Loaded;
        Unloaded += ScreenshotFolderManageDialog_Unloaded;
    }




    public ObservableCollection<ScreenshotFolder> ScreenshotFolders { get; set; } = new();


    public bool FolderChanged { get; set; }


    public bool CanSave { get; set => SetProperty(ref field, value); }



    private void ScreenshotFolderManageDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (Folders is not null)
            {
                foreach (var item in Folders)
                {
                    ScreenshotFolders.Add(item);
                }
            }
        }
        catch { }
    }



    private void ScreenshotFolderManageDialog_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            ScreenshotFolders.Clear();
            ScreenshotFolders = null!;
        }
        catch { }
    }




    [RelayCommand]
    private async Task AddFolderAsync()
    {
        try
        {
            string? folder = await FileDialogHelper.PickFolderAsync(this.XamlRoot);
            if (Directory.Exists(folder))
            {
                if (ScreenshotFolders.FirstOrDefault(x => x.Folder == folder) is null)
                {
                    ScreenshotFolders.Add(new ScreenshotFolder(folder));
                    CanSave = true;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add screenshot folder.");
        }
    }




    private async void Button_OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { DataContext: ScreenshotFolder folder })
            {
                if (Directory.Exists(folder.Folder))
                {
                    await Launcher.LaunchFolderPathAsync(folder.Folder);
                }
            }
        }
        catch { }
    }



    private void Button_RemoveFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is FrameworkElement { DataContext: ScreenshotFolder folder })
            {
                ScreenshotFolders.Remove(folder);
                CanSave = true;
            }
        }
        catch { }
    }



    private async void Button_BackupFolder_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button)
        {
            button.IsEnabled = false;
            try
            {
                TextBlock_BackupResult.Visibility = Visibility.Collapsed;
                if (button.DataContext is ScreenshotFolder folder)
                {
                    if (Directory.Exists(folder.Folder))
                    {
                        if (ScreenshotFolders?.FirstOrDefault(x => x.Default) is ScreenshotFolder screenshotFolder)
                        {
                            string backupFolder = screenshotFolder.Folder;
                            Directory.CreateDirectory(backupFolder);
                            StackPanel_BackingUp.Visibility = Visibility.Visible;
                            int count = await Task.Run(() =>
                            {

                                int count = 0;
                                var files = Directory.GetFiles(folder.Folder);
                                foreach (var item in files)
                                {
                                    var target = Path.Combine(backupFolder, Path.GetFileName(item));
                                    if (!File.Exists(target))
                                    {
                                        File.Copy(item, target);
                                        count++;
                                    }
                                }
                                return count;
                            });
                            StackPanel_BackingUp.Visibility = Visibility.Collapsed;
                            TextBlock_BackupResult.Visibility = Visibility.Visible;
                            TextBlock_BackupResult.Text = string.Format(Lang.ScreenshotPage_BackedUpNewScreenshots, count);
                            return;
                        }
                    }
                    _logger.LogWarning("Game exe name of {GameBiz} is null, cannot backup screenshots.", CurrentGameId.GameBiz);
                    TextBlock_BackupResult.Visibility = Visibility.Visible;
                    TextBlock_BackupResult.Text = Lang.ScreenshotFolderManageDialog_FailedToBackupScreenshots;
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to backup screenshots.");
                TextBlock_BackupResult.Visibility = Visibility.Visible;
                TextBlock_BackupResult.Text = Lang.ScreenshotFolderManageDialog_FailedToBackupScreenshots;
            }
            finally
            {
                StackPanel_BackingUp.Visibility = Visibility.Collapsed;
                button.IsEnabled = true;
            }
        }
    }




    [RelayCommand]
    private void Save()
    {
        try
        {
            FolderChanged = true;
            Folders ??= new();
            Folders.Clear();
            Folders.AddRange(ScreenshotFolders.Where(x => x.CanRemove));
            this.Hide();
        }
        catch
        {
            this.Hide();
        }
    }



    [RelayCommand]
    private void Cancel()
    {
        this.Hide();
    }


}
