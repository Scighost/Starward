using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Starward.Core;
using Starward.Models;
using System;
using System.Linq;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Controls;

[INotifyPropertyChanged]
public sealed partial class DownloadGameDialog : UserControl
{


    public VoiceLanguage LanguageType { get; set; }


    public DownloadGameResource GameResource { get; set; }


    public DownloadGameDialog()
    {
        this.InitializeComponent();
    }


    [ObservableProperty]
    private string packageSizeText;

    [ObservableProperty]
    private string downloadSizeText;


    [ObservableProperty]
    private string decompressedSizeText;


    [ObservableProperty]
    private string freeSpaceText;


    public bool IsChineseChecked
    {
        get => LanguageType.HasFlag(VoiceLanguage.Chinese);
        set
        {
            if (value)
            {
                LanguageType |= VoiceLanguage.Chinese;
            }
            else
            {
                LanguageType &= ~VoiceLanguage.Chinese;
            }
            UpdateSizeText();
        }
    }


    public bool IsEnglishChecked
    {
        get => LanguageType.HasFlag(VoiceLanguage.English);
        set
        {
            if (value)
            {
                LanguageType |= VoiceLanguage.English;
            }
            else
            {
                LanguageType &= ~VoiceLanguage.English;
            }
            UpdateSizeText();
        }
    }


    public bool IsJapaneseChecked
    {
        get => LanguageType.HasFlag(VoiceLanguage.Japanese);
        set
        {
            if (value)
            {
                LanguageType |= VoiceLanguage.Japanese;
            }
            else
            {
                LanguageType &= ~VoiceLanguage.Japanese;
            }
            UpdateSizeText();
        }
    }


    public bool IsKoreanChecked
    {
        get => LanguageType.HasFlag(VoiceLanguage.Korean);
        set
        {
            if (value)
            {
                LanguageType |= VoiceLanguage.Korean;
            }
            else
            {
                LanguageType &= ~VoiceLanguage.Korean;
            }
            UpdateSizeText();
        }
    }




    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (GameResource.Voices.Any())
        {
            StackPanel_Voice.Visibility = Visibility.Visible;
        }
        UpdateSizeText();
    }




    private void UpdateSizeText()
    {
        try
        {
            long package = 0, decompress = 0, download = 0;
            package += GameResource.Game.PackageSize;
            decompress += GameResource.Game.DecompressedSize;
            download += GameResource.Game.DownloadedSize;

            foreach (var lang in Enum.GetValues<VoiceLanguage>())
            {
                if (LanguageType.HasFlag(lang))
                {
                    if (GameResource.Voices.FirstOrDefault(x => x.Name == lang.ToDescription()) is DownloadPackageState pack)
                    {
                        package += pack.PackageSize;
                        decompress += pack.DecompressedSize;
                        download += pack.DownloadedSize;
                    }
                }
            }

            const double GB = 1 << 30;
            PackageSizeText = $"{package / GB:F2} GB";
            DownloadSizeText = $"{download / GB:F2} GB";
            DecompressedSizeText = $"{decompress / GB:F2} GB";
            FreeSpaceText = $"{GameResource.FreeSpace / GB:F2} GB";
        }
        catch { }
    }




}
