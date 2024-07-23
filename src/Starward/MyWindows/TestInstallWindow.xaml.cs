using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Starward.Core;
using Starward.Services.Download;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.MyWindows;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestInstallWindow : WindowEx
{


    private readonly InstallGameService _installGameService = AppConfig.GetService<InstallGameService>();

    private readonly DispatcherQueueTimer _timer;

    public GameBiz GameBiz { get; set; }



    public TestInstallWindow()
    {
        this.InitializeComponent();
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AdaptTitleBarButtonColorToActuallTheme();
        CenterInScreen(400, 400);
        _timer = DispatcherQueue.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(0.1);
        _timer.IsRepeating = true;
        _timer.Tick += _timer_Tick;
    }

    private void _timer_Tick(DispatcherQueueTimer sender, object args)
    {
        TextBlock_State.Text = _installGameService.State.ToString();
        TextBlock_Progress.Text = $"{_installGameService.FinishCount} / {_installGameService.TotalCount}   -   {_installGameService.FinishBytes:N0} / {_installGameService.TotalBytes:N0}   -   {_installGameService.ConcurrentExecuteThreadCount}";
    }

    private void Grid_Loaded(object sender, RoutedEventArgs e)
    {
        _installGameService.Initialize(GameBiz, $@"D:\test\{GameBiz}");
        _ = _installGameService.StartRepairGameAsync();
        _timer.Start();
    }

    private void Button_Action_Click(object sender, RoutedEventArgs e)
    {
        if (_installGameService.State is InstallGameState.None)
        {
            _installGameService.Continue();
        }
        else
        {
            _installGameService.Pause();
        }
    }
}
