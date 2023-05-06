// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.IO;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class MainPage : Page
{

    public static MainPage Current { get; private set; }


    public MainPage()
    {
        Current = this;
        this.InitializeComponent();
        InitializeBackgroundImage();
        MainPage_Frame.Navigate(typeof(LauncherPage));
    }


    [ObservableProperty]
    private BitmapImage backgroundImage;

    [ObservableProperty]
    private Uri backgroundImageUri;
    partial void OnBackgroundImageUriChanged(Uri value)
    {
        BackgroundImage = new BitmapImage(value);
    }



    public bool IsPaneToggleButtonVisible
    {
        get => MainPage_NavigationView.IsPaneToggleButtonVisible;
        set => MainPage_NavigationView.IsPaneToggleButtonVisible = value;
    }


    private void InitializeBackgroundImage()
    {
        try
        {
            var file = Path.Join(AppConfig.ConfigDirectory, "bg", AppConfig.BackgroundImage);
            if (File.Exists(file))
            {
                BackgroundImageUri = new Uri(file);
            }
        }
        catch (Exception ex)
        {

        }
    }


    private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
    {
        if (args.InvokedItemContainer.IsSelected)
        {
            return;
        }
        if (args.IsSettingsInvoked)
        {
        }
        else
        {
            var item = args.InvokedItemContainer as NavigationViewItem;
            if (item != null)
            {
                var type = item.Tag switch
                {
                    nameof(LauncherPage) => typeof(LauncherPage),
                    nameof(ScreenshotPage) => typeof(ScreenshotPage),
                    _ => null,
                };
                if (type != null)
                {
                    MainPage_Frame.Navigate(type, null, new Microsoft.UI.Xaml.Media.Animation.DrillInNavigationTransitionInfo());
                    if (type.Name is "LauncherPage")
                    {
                        Border_ContentBackground.Opacity = 0;
                    }
                    else
                    {
                        Border_ContentBackground.Opacity = 1;
                    }
                }
            }
        }
    }



}
