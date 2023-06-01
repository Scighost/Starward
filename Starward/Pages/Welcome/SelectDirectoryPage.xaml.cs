// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Helpers;
using System;
using System.IO;
using System.Threading.Tasks;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.Pages.Welcome;

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
            var parentFolder = Path.GetDirectoryName(AppContext.BaseDirectory.TrimEnd('/', '\\'));
            var exe = Path.Join(parentFolder, "Starward.exe");
            string? selectFolder = null;

            if (File.Exists(exe))
            {
                var dialog = new ContentDialog
                {
                    Title = "选择文件夹",
                    Content = $"""
                    应用数据默认保存在软件安装的文件夹中：

                    {parentFolder}

                    是否选择此文件夹？
                    """,
                    DefaultButton = ContentDialogButton.Primary,
                    PrimaryButtonText = "好的",
                    SecondaryButtonText = "自己选",
                    CloseButtonText = "不想选",
                    XamlRoot = this.XamlRoot
                };
                var result = await dialog.ShowAsync();
                if (result is ContentDialogResult.Primary)
                {
                    selectFolder = parentFolder;
                }
                if (result is ContentDialogResult.Secondary)
                {
                    selectFolder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.HWND);
                }
                if (result is ContentDialogResult.None)
                {
                    return;
                }
            }
            else
            {
                selectFolder = await FileDialogHelper.PickFolderAsync(MainWindow.Current.HWND);
            }

            if (Directory.Exists(selectFolder))
            {
                _logger.LogInformation("Select directory is '{Path}'", selectFolder);
                if (Path.GetFullPath(selectFolder.TrimEnd('/', '\\')) == Path.GetFullPath(AppContext.BaseDirectory.TrimEnd('/', '\\')))
                {
                    TargetDictionary = "此文件夹将在软件更新后被自动删除";
                    Button_Next.IsEnabled = false;
                }
                else
                {
                    var file = Path.Combine(selectFolder, Random.Shared.Next(int.MaxValue).ToString());
                    await File.WriteAllTextAsync(file, "");
                    File.Delete(file);
                    WelcomePage.Current.ConfigDirectory = selectFolder;
                    TargetDictionary = selectFolder;
                    Button_Next.IsEnabled = true;
                }
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
