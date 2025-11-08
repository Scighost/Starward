using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Gacha.UIGF;

[INotifyPropertyChanged]
public sealed partial class UIGF4GachaWindow : WindowEx
{

    private readonly ILogger<UIGF4GachaWindow> _logger = AppConfig.GetLogger<UIGF4GachaWindow>();

    private readonly UIGFGachaService _uigfGachaService = AppConfig.GetService<UIGFGachaService>();



    public UIGF4GachaWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
    }




    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        Title = Lang.ToolboxSetting_GachaRecordsImportExport;
        RootGrid.RequestedTheme = ShouldAppsUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        SystemBackdrop = new DesktopAcrylicBackdrop();
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
    }



    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var list = _uigfGachaService.GetLocalGachaArchives();
            foreach (var item in list)
            {
                GachaExportArchives.Add(item);
            }
        }
        catch (Exception ex)
        {
            ExportError = ex.Message;
        }
    }



    private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        GachaExportArchives.Clear();
        GachaImportArchives.Clear();
        Pivot_ExportImport.Items.Clear();
    }




    #region Export


    public ObservableCollection<GachaUidArchiveDisplay> GachaExportArchives { get; } = new();


    public string? ExportError { get; set => SetProperty(ref field, value); }


    [RelayCommand]
    private async Task ExportAsync()
    {
        try
        {
            ExportError = null;
            if (ListView_Export.SelectedItems.Count > 0)
            {
                string name = $"Starward_UIGF_{DateTimeOffset.Now:yyyyMMdd_HHmmss}.json";
                string? path = await FileDialogHelper.OpenSaveFileDialogAsync(Content.XamlRoot, name, ("JSON", ".json"));
                if (!string.IsNullOrWhiteSpace(path))
                {
                    await _uigfGachaService.ExportUIGF4Async(path, ListView_Export.SelectedItems.Cast<GachaUidArchiveDisplay>());
                    var file = await StorageFile.GetFileFromPathAsync(path);
                    var folder = Path.GetDirectoryName(path)!;
                    FolderLauncherOptions options = new();
                    options.ItemsToSelect.Add(file);
                    await Launcher.LaunchFolderAsync(await file.GetParentAsync(), options);
                }
            }
        }
        catch (Exception ex)
        {
            ExportError = ex.Message;
            _logger.LogError(ex, "Export uigf4");
        }
    }


    #endregion




    #region Import



    public ObservableCollection<GachaUidArchiveDisplay> GachaImportArchives { get; } = new();



    public string? ImportError { get; set => SetProperty(ref field, value); }



    [RelayCommand]
    private async Task SelectFileAsync()
    {
        try
        {
            ImportError = null;
            string? path = await FileDialogHelper.PickSingleFileAsync(Content.XamlRoot, ("JSON", ".json"));
            if (File.Exists(path))
            {
                GachaImportArchives.Clear();
                var list = await _uigfGachaService.ImportFileAsync(path);
                foreach (var item in list)
                {
                    GachaImportArchives.Add(item);
                }
            }
        }
        catch (Exception ex)
        {
            ImportError = ex.Message;
            _logger.LogError(ex, "Select uigf4 file");
        }
    }



    [RelayCommand]
    private async Task ImportAsync()
    {
        try
        {
            ImportError = null;
            if (ListView_Import.SelectedItems.Count > 0)
            {
                await _uigfGachaService.ImportAsync(ListView_Import.SelectedItems.Cast<GachaUidArchiveDisplay>());
            }
        }
        catch (Exception ex)
        {
            ImportError = ex.Message;
            _logger.LogError(ex, "Import uigf4 gacha");
        }
    }



    #endregion


}
