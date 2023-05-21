// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Helper;
using System;
using System.IO;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI.Welcome;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class SelectDirectoryPage : Page
{



    private readonly ILogger<SelectDirectoryPage> _logger = AppConfig.GetLogger<SelectDirectoryPage>();



    public SelectDirectoryPage()
    {
        this.InitializeComponent();
    }



    [ObservableProperty]
    private string targetDictionary;



    [RelayCommand]
    private async Task SelectDirectoryAsync()
    {
        try
        {
            var folder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.HWND);
            if (Directory.Exists(folder))
            {
                _logger.LogInformation("Select directory is '{Path}'", folder);
                var file = Path.Combine(folder, Random.Shared.Next(int.MaxValue).ToString());
                await File.WriteAllTextAsync(file, "");
                File.Delete(file);
                WelcomePage.Current.ConfigDirectory = folder;
                TargetDictionary = folder;
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
            _logger.LogError(ex, "Select config directory");
            Button_Next.IsEnabled = false;
            TargetDictionary = ex.Message;
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
