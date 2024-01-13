using Microsoft.UI.Xaml.Navigation;
using Starward.Core.GameRecord;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.HoyolabToolbox;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class HyperionWebBridgePage : PageBase
{


    public HyperionWebBridgePage()
    {
        this.InitializeComponent();
    }





    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        if (e.Parameter is PageParameter parameter)
        {
            webBridge.GameRecordRole = parameter.GameRole;
            webBridge.TargetUrl = parameter.TargetUrl;
        }
        if (e.Parameter is GameRecordRole role)
        {
            webBridge.GameRecordRole = role;
        }
    }





    public class PageParameter(GameRecordRole gameRole, string targetUrl)
    {
        public GameRecordRole GameRole { get; set; } = gameRole;

        public string TargetUrl { get; set; } = targetUrl;
    }









}
