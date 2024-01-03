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
        InitializeAfterStartGameAction();
    }



    protected override void OnLoaded()
    {
        ComboBox_CloseWindowOption.SelectionChanged += ComboBox_CloseWindowOption_SelectionChanged;
        ComboBox_AfterStartGameAction.SelectionChanged += ComboBox_AfterStartGameAction_SelectionChanged;
    }



    protected override void OnUnloaded()
    {
        ComboBox_CloseWindowOption.SelectionChanged -= ComboBox_CloseWindowOption_SelectionChanged;
        ComboBox_AfterStartGameAction.SelectionChanged -= ComboBox_AfterStartGameAction_SelectionChanged;
    }



    #region Close Window Option



    private void InitializeCloseWindowOption()
    {
        try
        {
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
                    CloseWindowOption option => option,
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





    #region After Start Game Action



    private void InitializeAfterStartGameAction()
    {
        try
        {
            var option = AppConfig.AfterStartGameAction;
            if (option is AfterStartGameAction.Hide)
            {
                ComboBox_AfterStartGameAction.SelectedIndex = 0;
            }
            else if (option is AfterStartGameAction.Minimize)
            {
                ComboBox_AfterStartGameAction.SelectedIndex = 1;
            }
            else if (option is AfterStartGameAction.DoNothing)
            {
                ComboBox_AfterStartGameAction.SelectedIndex = 2;
            }
        }
        catch { }
    }



    private void ComboBox_AfterStartGameAction_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_AfterStartGameAction.SelectedItem is ComboBoxItem item)
            {
                AppConfig.AfterStartGameAction = item.Tag switch
                {
                    AfterStartGameAction action => action,
                    _ => AfterStartGameAction.Hide,
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change After Start Game Action");
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
