// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Starward.Helpers;
using Starward.Services;
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

    private readonly WelcomeService _welcomeService = AppConfig.GetService<WelcomeService>();



    public SelectDirectoryPage()
    {
        this.InitializeComponent();
        InitializeFirstUse();
    }



    private void InitializeFirstUse()
    {
        TextBlockHelper.Inlines(TextBlock_FirstUse.Inlines, Lang.SelectDirectoryPage_FirstUse, ("{Starward}", null));
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

            if (!AppConfig.MsixPackaged && File.Exists(exe))
            {
                var dialog = new ContentDialog
                {
                    // 选择文件夹
                    Title = Lang.SelectDirectoryPage_SelectFolder,
                    // 推荐您将数据保存在软件所在的文件夹中：
                    // 是否选择此文件夹？
                    Content = $"""
                    {Lang.SelectDirectoryPage_RecommendFolder}
                    
                    {parentFolder}

                    {Lang.SelectDirectoryPage_WouldYouLikeToChooseThisFolder}
                    """,
                    DefaultButton = ContentDialogButton.Primary,
                    // 好的
                    PrimaryButtonText = Lang.SelectDirectoryPage_OK,
                    // 自己选
                    SecondaryButtonText = Lang.SelectDirectoryPage_ChooseAnother,
                    // 不想选
                    CloseButtonText = Lang.SelectDirectoryPage_Cancel,
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
                if (Path.GetFullPath(selectFolder) == Path.GetFullPath(AppContext.BaseDirectory.TrimEnd('/', '\\')))
                {
                    // 此文件夹将在软件更新后被自动删除
                    TargetDictionary = Lang.SelectDirectoryPage_AutoDeleteAfterUpdate;
                    Button_Next.IsEnabled = false;
                }
                else
                {
                    var file = Path.Combine(selectFolder, Random.Shared.Next(int.MaxValue).ToString());
                    await File.WriteAllTextAsync(file, "");
                    File.Delete(file);
                    _welcomeService.UserDataFolder = selectFolder;
                    TargetDictionary = selectFolder;
                    Button_Next.IsEnabled = true;
                }
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "No write permission.");
            // 没有写入权限
            TargetDictionary = Lang.SelectDirectoryPage_NoWritePermission;
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
        _welcomeService.NavigateTo(typeof(SelectGamePage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
    }



    [RelayCommand]
    private void Next()
    {
        _welcomeService.NavigateTo(typeof(SelectGamePage), null!, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
    }





}
