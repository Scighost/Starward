using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
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

    public GameBiz GameBiz { get; set; }


    public AudioLanguage LanguageType { get; set; }


    public DownloadGameResource GameResource { get; set; }


    public bool PreDownloadMode { get; set; }


    public bool RepairMode { get; set; }



    public DownloadGameDialog()
    {
        this.InitializeComponent();
    }


    [ObservableProperty]
    private bool showAllInfo;


    [ObservableProperty]
    private bool showRepairSwitcher;


    [ObservableProperty]
    private bool showVoiceBoxes;


    [ObservableProperty]
    private bool canCheckVoiceBoxes;


    [ObservableProperty]
    private string packageSizeText;


    [ObservableProperty]
    private string downloadSizeText;


    [ObservableProperty]
    private string decompressedSizeText;


    [ObservableProperty]
    private string freeSpaceText;


    [ObservableProperty]
    private string freeSpaceCautionText;

    [ObservableProperty]
    private bool enableRepairMode;
    partial void OnEnableRepairModeChanged(bool value)
    {
        UpdateSizeText();
    }


    public bool IsChineseChecked
    {
        get => LanguageType.HasFlag(AudioLanguage.Chinese);
        set
        {
            if (value)
            {
                LanguageType |= AudioLanguage.Chinese;
            }
            else
            {
                LanguageType &= ~AudioLanguage.Chinese;
            }
            UpdateSizeText();
        }
    }


    public bool IsEnglishChecked
    {
        get => LanguageType.HasFlag(AudioLanguage.English);
        set
        {
            if (value)
            {
                LanguageType |= AudioLanguage.English;
            }
            else
            {
                LanguageType &= ~AudioLanguage.English;
            }
            UpdateSizeText();
        }
    }


    public bool IsJapaneseChecked
    {
        get => LanguageType.HasFlag(AudioLanguage.Japanese);
        set
        {
            if (value)
            {
                LanguageType |= AudioLanguage.Japanese;
            }
            else
            {
                LanguageType &= ~AudioLanguage.Japanese;
            }
            UpdateSizeText();
        }
    }


    public bool IsKoreanChecked
    {
        get => LanguageType.HasFlag(AudioLanguage.Korean);
        set
        {
            if (value)
            {
                LanguageType |= AudioLanguage.Korean;
            }
            else
            {
                LanguageType &= ~AudioLanguage.Korean;
            }
            UpdateSizeText();
        }
    }




    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (RepairMode)
        {
            ShowAllInfo = false;
            ShowRepairSwitcher = false;
            if (GameBiz.ToGame() == GameBiz.hk4e)
            {
                ShowVoiceBoxes = true;
                CanCheckVoiceBoxes = true;
            }
        }
        else if (PreDownloadMode)
        {
            ShowAllInfo = true;
            ShowRepairSwitcher = false;
            if (GameBiz.ToGame().Value is GameBiz.hk4e or GameBiz.hkrpg)
            {
                ShowVoiceBoxes = true;
            }
        }
        else
        {
            ShowAllInfo = true;
            if (GameBiz.ToGame().Value is GameBiz.hk4e or GameBiz.hkrpg)
            {
                ShowRepairSwitcher = true;
                ShowVoiceBoxes = true;
                CanCheckVoiceBoxes = true;
            }
        }
        UpdateSizeText();
    }




    private void UpdateSizeText()
    {
        try
        {
            if (GameResource is null)
            {
                return;
            }
            long package = 0, decompress = 0, download = 0;
            package += GameResource.Game.PackageSize;
            decompress += GameResource.Game.DecompressedSize;
            download += GameResource.Game.DownloadedSize;

            foreach (var lang in Enum.GetValues<AudioLanguage>())
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

            FreeSpaceText = $"{GameResource.FreeSpace / GB:F2} GB";
            long delta = 0;
            if (EnableRepairMode)
            {
                PackageSizeText = $"{(decompress - package) / GB:F2} GB";
                DownloadSizeText = $"-";
                DecompressedSizeText = $"{(decompress - package) / GB:F2} GB";
                delta = GameResource.FreeSpace + package - decompress;
            }
            else
            {
                PackageSizeText = $"{package / GB:F2} GB";
                DownloadSizeText = $"{download / GB:F2} GB";
                DecompressedSizeText = $"{decompress / GB:F2} GB";
                delta = GameResource.FreeSpace + download - decompress;
            }
            if (delta < 0)
            {
                // 剩余空间可能不足
                FreeSpaceCautionText = Lang.DownloadGameDialog_FreeSpaceMayBeNotEnough;
                TextBlock_FreeSpaceCaution.Foreground = Application.Current.Resources["SystemFillColorCautionBrush"] as Brush;
            }
            else
            {
                FreeSpaceCautionText = "";
                TextBlock_FreeSpaceCaution.Foreground = Application.Current.Resources["TextFillColorPrimaryBrush"] as Brush;
            }
        }
        catch { }
    }




}
