// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Services;
using System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WelcomePage : Page
{


    private readonly WelcomeService _welcomeService = AppConfig.GetService<WelcomeService>();


    public WelcomePage()
    {
        this.InitializeComponent();
        _welcomeService.Reset();
        _welcomeService.OnNavigateTo += _welcomeService_OnNavigateTo;
        int length = (int)(48 * MainWindow.Current.UIScale);
        MainWindow.Current.SetDragRectangles(new Windows.Graphics.RectInt32(0, 0, 10000, length));
        frame.Navigate(typeof(SelectLanguagePage));
    }


    private void Page_Unloaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _welcomeService.OnNavigateTo -= _welcomeService_OnNavigateTo;
    }



    private void _welcomeService_OnNavigateTo(object? sender, (Type Page, object Parameter, NavigationTransitionInfo InfoOverride) e)
    {
        frame.Navigate(e.Page, e.Parameter, e.InfoOverride);
    }




}
