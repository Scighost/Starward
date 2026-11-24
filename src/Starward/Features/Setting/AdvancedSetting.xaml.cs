using Microsoft.Extensions.Logging;


namespace Starward.Features.Setting;

public sealed partial class AdvancedSetting : PageBase
{

    private readonly ILogger<AdvancedSetting> _logger = AppConfig.GetLogger<AdvancedSetting>();



    public AdvancedSetting()
    {
        this.InitializeComponent();
    }

}
