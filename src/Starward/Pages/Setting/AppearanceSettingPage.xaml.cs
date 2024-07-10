using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Starward.Helpers;
using Starward.Messages;
using Starward.Services;
using System;
using System.Globalization;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Setting;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class AppearanceSettingPage : PageBase
{


    private readonly ILogger<AppearanceSettingPage> _logger = AppConfig.GetLogger<AppearanceSettingPage>();

    private readonly HoYoPlayService _hoYoPlayService = AppConfig.GetService<HoYoPlayService>();


    public AppearanceSettingPage()
    {
        this.InitializeComponent();
        InitializeLanguage();
        UpdateExperienceDesc();
    }



    #region Language


    private bool languageInitialized;

    private void InitializeLanguage()
    {
        try
        {
            var lang = AppConfig.Language;
            ComboBox_Language.Items.Clear();
            ComboBox_Language.Items.Add(new ComboBoxItem
            {
                Content = Lang.ResourceManager.GetString(nameof(Lang.SettingPage_FollowSystem), CultureInfo.InstalledUICulture),
                Tag = "",
            });
            ComboBox_Language.SelectedIndex = 0;
            foreach (var (Title, LangCode) in Localization.LanguageList)
            {
                var box = new ComboBoxItem
                {
                    Content = Title,
                    Tag = LangCode,
                };
                ComboBox_Language.Items.Add(box);
                if (LangCode == lang)
                {
                    ComboBox_Language.SelectedItem = box;
                }
            }
        }
        finally
        {
            languageInitialized = true;
        }
    }



    private void ComboBox_Language_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            if (ComboBox_Language.SelectedItem is ComboBoxItem item)
            {
                if (languageInitialized)
                {
                    var lang = item.Tag as string;
                    _logger.LogInformation("Language change to {lang}", lang);
                    AppConfig.Language = lang;
                    if (string.IsNullOrWhiteSpace(lang))
                    {
                        CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
                    }
                    else
                    {
                        CultureInfo.CurrentUICulture = new CultureInfo(lang);
                    }
                    this.Bindings.Update();
                    UpdateExperienceDesc();
                    WeakReferenceMessenger.Default.Send(new LanguageChangedMessage(lang!, CultureInfo.CurrentUICulture));
                    AppConfig.SaveConfiguration();
                    _ = _hoYoPlayService.PrepareDataAsync();
                }
            }
        }
        catch (CultureNotFoundException)
        {
            CultureInfo.CurrentUICulture = CultureInfo.InstalledUICulture;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Change Language");
        }
    }




    #endregion





    #region Theme Color



    [ObservableProperty]
    private bool useSystemThemeColor = AppConfig.UseSystemThemeColor;
    partial void OnUseSystemThemeColorChanged(bool value)
    {
        AppConfig.UseSystemThemeColor = value;
        WeakReferenceMessenger.Default.Send(new UpdateBackgroundImageMessage(true));
    }



    #endregion





    #region Experience



    private void UpdateExperienceDesc()
    {
        try
        {
            TextBlock_BetterExperience.Inlines.Clear();
            TextBlockHelper.Inlines(TextBlock_BetterExperience.Inlines,
                                    string.Format(Lang.AppearanceSettingPage_BetterExperience,
                                                  $"{{{Lang.AppearanceSettingPage_TransparencyEffects}}}",
                                                  $"{{{Lang.AppearanceSettingPage_AnimationEffects}}}"),
                                    ($"{{{Lang.AppearanceSettingPage_TransparencyEffects}}}", "ms-settings:easeofaccess-visualeffects"),
                                    ($"{{{Lang.AppearanceSettingPage_AnimationEffects}}}", "ms-settings:easeofaccess-visualeffects"));
        }
        catch { }
    }




    #endregion


}
