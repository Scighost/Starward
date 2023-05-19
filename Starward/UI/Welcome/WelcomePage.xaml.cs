// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WelcomePage : Page
{

    public static WelcomePage Current { get; private set; }

    public string ConfigDirectory { get; set; }


    public WelcomePage()
    {
        Current = this;
        this.InitializeComponent();
        frame.Content = new SelectDirectoryPage();
    }


    public WelcomePage(bool first)
    {
        Current = this;
        this.InitializeComponent();
        if (first)
        {
            frame.Content = new SelectLanguagePage();
        }
        else
        {
            frame.Content = new SelectDirectoryPage();
        }
    }


    public void NavigateTo(Type page, object parameter, NavigationTransitionInfo infoOverride)
    {
        frame.Navigate(page, parameter, infoOverride);
    }


}
