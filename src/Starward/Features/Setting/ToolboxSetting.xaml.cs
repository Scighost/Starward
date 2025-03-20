using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Features.Gacha.ZZZGachaToolbox;
using System.Collections.Generic;


namespace Starward.Features.Setting;

[INotifyPropertyChanged]
public sealed partial class ToolboxSetting : UserControl
{


    private readonly ILogger<ToolboxSetting> _logger = AppConfig.GetLogger<ToolboxSetting>();


    public ToolboxSetting()
    {
        this.InitializeComponent();
        this.Loaded += ToolboxSetting_Loaded;
        this.Unloaded += ToolboxSetting_Unloaded;
    }



    private void ToolboxSetting_Loaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, OnLanguageChanged);
        ToolboxItems =
        [
            new ToolboxItem(null,
                            "ms-appx:///Assets/Image/GachaTicket2Big.png",
                            nameof(ZZZGachaInfoWindow),
                            nameof(Lang.ToolboxSetting_ZZZGachaItemImages),
                            nameof(Lang.ToolboxSetting_DisplayItemImagesInZZZGachaRecordPage)),
        ];
    }



    private void ToolboxSetting_Unloaded(object sender, RoutedEventArgs e)
    {
        WeakReferenceMessenger.Default.UnregisterAll(this);
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
        }
    }



}
