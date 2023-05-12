// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class WelcomePage : Page
{

    public static WelcomePage Current { get; private set; }

    public string ConfigDirecory { get; set; }


    public WelcomePage()
    {
        Current = this;
        this.InitializeComponent();
        frame.Content = new SelectDirectoryPage();
        // todo 多语言页面
    }


    public void NavigateTo(Type page, object parameter, NavigationTransitionInfo infoOverride)
    {
        frame.Navigate(page, parameter, infoOverride);
    }


}
