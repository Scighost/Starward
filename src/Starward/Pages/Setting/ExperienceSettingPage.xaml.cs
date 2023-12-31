using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Messages;
using Starward.Models;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class ExperienceSettingPage : PageBase
{


    private readonly ILogger<ExperienceSettingPage> _logger = AppConfig.GetLogger<ExperienceSettingPage>();



    public ExperienceSettingPage()
    {
        this.InitializeComponent();
        InitializeCloseWindowOption();
    }





    #region Close Window Option



    private void InitializeCloseWindowOption()
    {
        try
        {
            var lang = AppConfig.Language;
            ComboBox_CloseWindowOption.Items.Clear();
            ComboBox_CloseWindowOption.Items.Add(new ComboBoxItem
            {
                Content = Lang.ExperienceSettingPage_MinimizeToSystemTray,
                Tag = "Hide",
            });
            ComboBox_CloseWindowOption.Items.Add(new ComboBoxItem
            {
                Content = Lang.ExperienceSettingPage_ExitCompletely,
                Tag = "Exit",
            });
            ComboBox_CloseWindowOption.Items.Add(new ComboBoxItem
            {
                Content = Lang.ExperienceSettingPage_CloseWindowButKeepSystemTray,
                Tag = "Close",
            });
            var option = AppConfig.CloseWindowOption;
            if (option is CloseWindowOption.Hide)
            {
                ComboBox_CloseWindowOption.SelectedIndex = 0;
            }
            else if (option is CloseWindowOption.Exit)
            {
                ComboBox_CloseWindowOption.SelectedIndex = 1;
            }
            else if (option is CloseWindowOption.Close)
            {
                ComboBox_CloseWindowOption.SelectedIndex = 2;
            }
        }
        catch { }
    }



    private void ComboBox_CloseWindowOption_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_CloseWindowOption.SelectedItem is ComboBoxItem item)
            {
                AppConfig.CloseWindowOption = item.Tag switch
                {
                    "Hide" => CloseWindowOption.Hide,
                    "Exit" => CloseWindowOption.Exit,
                    "Close" => CloseWindowOption.Close,
                    _ => CloseWindowOption.Undefined,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change Close Window Option");
        }
    }




    #endregion




    #region Features



    [ObservableProperty]
    private bool disableGameAccountSwitcher = AppConfig.DisableGameAccountSwitcher;
    partial void OnDisableGameAccountSwitcherChanged(bool value)
    {
        AppConfig.DisableGameAccountSwitcher = value;
        WeakReferenceMessenger.Default.Send(new GameAccountSwitcherDisabledChanged(value));
    }


    [ObservableProperty]
    private bool disableGameNoticeRedHot = AppConfig.DisableGameNoticeRedHot;
    partial void OnDisableGameNoticeRedHotChanged(bool value)
    {
        AppConfig.DisableGameNoticeRedHot = value;
        WeakReferenceMessenger.Default.Send(new GameNoticeRedHotDisabledChanged(value));
    }




    #endregion



}
