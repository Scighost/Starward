using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Starward.Frameworks;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.System;


namespace Starward.Features.ViewHost;

[ObservableObject]
public sealed partial class NoPermissionWindow : WindowEx
{


    private TaskCompletionSource _taskCompletionSource;


    public string Folder { get; set; }



    public NoPermissionWindow(string folder)
    {
        InitializeComponent();
        InitializeWindow();
        _taskCompletionSource = new();
        Folder = folder;
    }



    private void InitializeWindow()
    {
        this.Closed += NoPermissionWindow_Closed;
        ExtendsContentIntoTitleBar = true;
        RootGrid.RequestedTheme = ShouldSystemUseDarkMode() ? ElementTheme.Dark : ElementTheme.Light;
        if (AppWindow.Presenter is OverlappedPresenter presenter)
        {
            presenter.IsMaximizable = false;
            presenter.IsResizable = false;
        }
        CenterInScreen(460, 280);
    }




    private void NoPermissionWindow_Closed(object sender, WindowEventArgs args)
    {
        _taskCompletionSource.SetResult();
    }



    public async Task WaitAsync()
    {
        this.Activate();
        await _taskCompletionSource.Task;
    }



    [RelayCommand]
    private void RestartAsAdmin()
    {
        try
        {
            ProcessStartInfo info = new ProcessStartInfo
            {
                FileName = Environment.ProcessPath,
                UseShellExecute = true,
                Verb = "runas",
            };
            foreach (string? item in Environment.GetCommandLineArgs().Skip(1))
            {
                info.ArgumentList.Add(item);
            }
            Process.Start(info);
            Close();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }



    [RelayCommand]
    private new void Close()
    {
        base.Close();
    }


    private async void Hyperlink_Click(Microsoft.UI.Xaml.Documents.Hyperlink sender, Microsoft.UI.Xaml.Documents.HyperlinkClickEventArgs args)
    {
        if (Directory.Exists(Folder))
        {
            await Launcher.LaunchUriAsync(new Uri(Folder));
        }
    }




}
