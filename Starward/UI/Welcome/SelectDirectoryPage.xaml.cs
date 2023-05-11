// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Navigation;
using Starward.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Devices.Display.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage.Pickers;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SelectDirectoryPage : Page
{



    private readonly ILogger<SelectDirectoryPage> _logger;



    public SelectDirectoryPage()
    {
        this.InitializeComponent();
        _logger = ServiceProvider.GetLogger<SelectDirectoryPage>();
    }



    [ObservableProperty]
    private string targetDictionary;



    [RelayCommand]
    private async Task SelectDirectoryAsync()
    {
        try
        {
            var picker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            };
            InitializeWithWindow.Initialize(picker, MainWindow.Current.HWND);
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null)
            {
                var file = Path.Combine(folder.Path, Random.Shared.Next(int.MaxValue).ToString());
                await File.WriteAllTextAsync(file, "");
                File.Delete(file);
                AppConfig.SetConfigDirectory(folder.Path);
                TargetDictionary = folder.Path;
                Button_Next.IsEnabled = true;
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "No write permission.");
            TargetDictionary = "选择的文件夹没有写入权限";
            Button_Next.IsEnabled = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, null);
            Button_Next.IsEnabled = false;
        }
    }




    [RelayCommand]
    private void Preview()
    {
        WelcomePage.Current?.NavigateTo(typeof(SelectGamePage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
    }



    [RelayCommand]
    private void Next()
    {
        WelcomePage.Current?.NavigateTo(typeof(SelectGamePage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }





}
