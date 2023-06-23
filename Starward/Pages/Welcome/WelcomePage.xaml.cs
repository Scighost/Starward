// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WelcomePage : Page
{

    public static WelcomePage Current { get; private set; }


    public string TextLanguage { get; set; }

    public int WindowSizeMode { get; set; }

    public int ApiCDNIndex { get; set; }

    public string UserDataFolder { get; set; }


    public WelcomePage()
    {
        Current = this;
        this.InitializeComponent();
        MainWindow.Current.ChangeAccentColor(null);
        frame.Navigate(typeof(SelectDirectoryPage));
    }


    public WelcomePage(bool first)
    {
        Current = this;
        this.InitializeComponent();
        MainWindow.Current.ChangeAccentColor(null);
        if (first)
        {
            frame.Navigate(typeof(SelectLanguagePage));
        }
        else
        {
            frame.Navigate(typeof(SelectDirectoryPage));
        }
    }


    public void NavigateTo(Type page, object parameter, NavigationTransitionInfo infoOverride)
    {
        frame.Navigate(page, parameter, infoOverride);
    }


    public void ApplySetting()
    {
        AppConfig.UserDataFolder = UserDataFolder;
        AppConfig.Language = TextLanguage;
        AppConfig.WindowSizeMode = WindowSizeMode;
        AppConfig.ApiCDNIndex = ApiCDNIndex;
    }


}
