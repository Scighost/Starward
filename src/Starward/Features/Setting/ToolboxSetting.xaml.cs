using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Starward.Features.Gacha.UIGF;
using Starward.Features.Gacha.ZZZGachaToolbox;
using Starward.Frameworks;
using System.Collections.Generic;


namespace Starward.Features.Setting;

public sealed partial class ToolboxSetting : PageBase
{


    private readonly ILogger<ToolboxSetting> _logger = AppConfig.GetLogger<ToolboxSetting>();


    public ToolboxSetting()
    {
        this.InitializeComponent();
    }



    protected override void OnLoaded()
    {
        ToolboxItems =
         [
             new ToolboxItem("\xE794",
                            null,
                            nameof(UIGF4GachaWindow),
                            nameof(Lang.ToolboxSetting_GachaRecordsImportExport),
                            ""){ Description="UIGF v4.0" },
            new ToolboxItem(null,
                            "ms-appx:///Assets/Image/GachaTicket2Big.png",
                            nameof(ZZZGachaInfoWindow),
                            nameof(Lang.ToolboxSetting_ZZZGachaItemImages),
                            nameof(Lang.ToolboxSetting_DisplayItemImagesInZZZGachaRecordPage)),
        ];
    }



    protected override void OnUnloaded()
    {
        ToolboxItems = null!;
    }



    public List<ToolboxItem> ToolboxItems { get; set => SetProperty(ref field, value); }



    private void OnLanguageChanged(object _, LanguageChangedMessage __)
    {
        foreach (var item in ToolboxItems)
        {
            item.UpdateLanguage();
        }
    }




    private void Button_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: ToolboxItem item })
        {
            if (item.Tag is nameof(ZZZGachaInfoWindow))
            {
                new ZZZGachaInfoWindow().Activate();
            }
            if (item.Tag is nameof(UIGF4GachaWindow))
            {
                new UIGF4GachaWindow().Activate();
            }
        }
    }



}
