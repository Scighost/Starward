using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.Web.WebView2.Core;
using Starward.Features.Database;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.System;


namespace Starward.Features.ViewHost;

[ObservableObject]
public sealed partial class WelcomeWindow : WindowEx
{


    private TaskCompletionSource<bool> _taskCompletionSource;



    public WelcomeWindow()
    {
        InitializeComponent();
        InitializeWindow();
        _taskCompletionSource = new();
    }



    private void InitializeWindow()
    {
        this.Closed += NoPermissionWindow_Closed;
        ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        CenterInScreen(1200, 676);
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = true;
        }
    }




    private void NoPermissionWindow_Closed(object sender, WindowEventArgs args)
    {
        _taskCompletionSource.TrySetResult(false);
    }



    public async Task<bool> WaitAsync()
    {
        this.Activate();
        return await _taskCompletionSource.Task;
    }



    public string? UserDataFolder { get; set => SetProperty(ref field, value); }


    public string? UserDataFolderErrorMessage { get; set => SetProperty(ref field, value); }


    public string? WebView2Version { get; set => SetProperty(ref field, value); }


    public bool WebpDecoderSupport { get; set => SetProperty(ref field, value); }


    public bool CanStartStarward { get; set => SetProperty(ref field, value); }


    public bool IsWin11 { get; set => SetProperty(ref field, value); }



    private async void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        IsWin11 = Environment.OSVersion.Version >= new Version(10, 0, 22000);
        InitializeDefaultUserDataFolder();
        CheckWritePermission();
        CheckWebView2Support();
        await CheckWebpDecoderSupportAsync();
    }





    private void InitializeDefaultUserDataFolder()
    {
        try
        {
            string? parentFolder = new DirectoryInfo(AppContext.BaseDirectory).Parent?.FullName;
            if (AppConfig.IsPortable)
            {
                UserDataFolder = parentFolder;
            }
            else if (AppConfig.IsAppInRemovableStorage)
            {
                UserDataFolder = Path.Combine(Path.GetPathRoot(AppContext.BaseDirectory)!, ".StarwardData");
            }
            else
            {
#if DEBUG
                UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Starward");
#else
                UserDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Starward");
#endif
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    private void CheckWritePermission()
    {
        try
        {
            UserDataFolderErrorMessage = null;
            CanStartStarward = false;
            if (string.IsNullOrWhiteSpace(UserDataFolder) || !Path.IsPathFullyQualified(UserDataFolder))
            {
                UserDataFolderErrorMessage = Lang.DownloadGamePage_TheFolderDoesNotExist;
                return;
            }
            string folder = Path.GetFullPath(UserDataFolder);
            Directory.CreateDirectory(folder);
            if (folder == Path.GetPathRoot(folder))
            {
                UserDataFolderErrorMessage = Lang.LauncherPage_PleaseDoNotSelectTheRootDirectoryOfADrive;
                return;
            }
            string baseDir = AppContext.BaseDirectory.TrimEnd('/', '\\');
            if (folder.StartsWith(baseDir))
            {
                UserDataFolderErrorMessage = Lang.SelectDirectoryPage_AutoDeleteAfterUpdate;
                return;
            }
            var file = Path.Combine(folder, Guid.CreateVersion7().ToString());
            File.WriteAllBytes(file, "Write permission test."u8);
            File.Delete(file);
            CanStartStarward = true;
        }
        catch (UnauthorizedAccessException ex)
        {
            // 没有写入权限
            UserDataFolderErrorMessage = Lang.SelectDirectoryPage_NoWritePermission;
            Debug.WriteLine(ex);
        }
        catch (Exception ex)
        {
            UserDataFolderErrorMessage = ex.Message;
            Debug.WriteLine(ex);
        }

    }



    [RelayCommand]
    private async Task ChangeUserDataFolderAsync()
    {
        try
        {
            string? folder = await FileDialogHelper.PickFolderAsync(Content.XamlRoot);
            if (Directory.Exists(folder))
            {
                UserDataFolder = folder;
                CheckWritePermission();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    private void CheckWebView2Support()
    {
        try
        {
            WebView2Version = CoreWebView2Environment.GetAvailableBrowserVersionString();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }




    private async Task CheckWebpDecoderSupportAsync()
    {
        try
        {
            // 一个webp图片
            byte[] bytes = Convert.FromBase64String("UklGRiQAAABXRUJQVlA4IBgAAAAwAQCdASoBAAEAAgA0JaQAA3AA/vv9UAA=");
            using MemoryStream ms = new MemoryStream(bytes);
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(BitmapDecoder.WebpDecoderId, ms.AsRandomAccessStream());
            WebpDecoderSupport = true;
        }
        catch (Exception ex)
        {
            // 0x88982F8B
            Debug.WriteLine(ex);
        }
    }



    private async void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        try
        {
            if (sender.NavigateUri.Scheme is "http" or "https")
            {
                return;
            }
            await Launcher.LaunchUriAsync(sender.NavigateUri);
        }
        catch { }
    }



    [RelayCommand]
    private void Start()
    {
        try
        {
            if (!Directory.Exists(UserDataFolder))
            {
                UserDataFolderErrorMessage = Lang.DownloadGamePage_TheFolderDoesNotExist;
                CanStartStarward = false;
                return;
            }
            AppConfig.UserDataFolder = UserDataFolder;
            DatabaseService.SetDatabase(UserDataFolder);
            AppConfig.SaveConfiguration();
            _taskCompletionSource.SetResult(true);
            this.Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



}
