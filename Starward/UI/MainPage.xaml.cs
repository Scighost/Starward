// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media.Imaging;
using Starward.Core;
using Starward.Service;
using Starward.Service.Gacha;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Imaging;
using Windows.UI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Starward.UI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
[INotifyPropertyChanged]
public sealed partial class MainPage : Page
{

    public static MainPage Current { get; private set; }


    private readonly ILogger<MainPage> _logger = AppConfig.GetLogger<MainPage>();


    private readonly LauncherService _launcherService = AppConfig.GetService<LauncherService>();



    // todo game and region 切换
    public MainPage()
    {
        Current = this;
        this.InitializeComponent();

        InitializeSelectGameBiz();
        InitializeBackgroundImage();
        NavigateTo(typeof(LauncherPage));
    }




    public bool IsPaneToggleButtonVisible
    {
        get => MainPage_NavigationView.IsPaneToggleButtonVisible;
        set => MainPage_NavigationView.IsPaneToggleButtonVisible = value;
    }




    private async void Page_Loaded(object sender, RoutedEventArgs e)
    {
        if (BackgroundImage is null)
        {
            await UpdateBackgroundImageAsync();
        }
    }






    #region Select Game

    [ObservableProperty]
    private GameBiz currentGameBiz;
    partial void OnCurrentGameBizChanged(GameBiz value)
    {
        NavigationViewItem_GachaLog.Content = GachaLogService.GetGachaLogText(value);
    }


    private GameBiz selectGameBiz = AppConfig.SelectGameBiz;


    private void InitializeSelectGameBiz()
    {
        CurrentGameBiz = selectGameBiz;
        var index = CurrentGameBiz switch
        {
            GameBiz.hk4e_cn => 0,
            GameBiz.hk4e_global => 1,
            GameBiz.hk4e_cloud => 2,
            GameBiz.hkrpg_cn => 3,
            GameBiz.hkrpg_global => 4,
            _ => -1,
        };
        if (index >= 0)
        {
            ComboBox_GameBiz.SelectedIndex = index;
        }
    }


    private void ComboBox_GameBiz_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        try
        {
            Button_ChangeGameBiz.IsEnabled = false;
            if (ComboBox_GameBiz.SelectedItem is FrameworkElement ele)
            {
                if (Enum.TryParse(ele.Tag as string, out selectGameBiz))
                {
                    if (selectGameBiz != CurrentGameBiz)
                    {
                        Button_ChangeGameBiz.IsEnabled = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {

        }
    }


    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task ChangeGameBizAsync()
    {
        CurrentGameBiz = selectGameBiz;
        AppConfig.SelectGameBiz = CurrentGameBiz;
        Button_ChangeGameBiz.IsEnabled = false;
        NavigateTo(MainPage_Frame.SourcePageType);
        await UpdateBackgroundImageAsync();
    }



    private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }


    private void StackPanel_SelectGame_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        UpdateDragRectangles();
    }



    private void UpdateDragRectangles()
    {
        try
        {
            var scale = MainWindow.Current.UIScale;
            var point = StackPanel_SelectGame.TransformToVisual(this).TransformPoint(new Windows.Foundation.Point());
            var width = StackPanel_SelectGame.ActualWidth;
            var height = StackPanel_SelectGame.ActualHeight;
            int len = (int)(48 * scale);
            var rect1 = new RectInt32(len, 0, (int)((point.X - 48) * scale), len);
            var rect2 = new RectInt32((int)((point.X + width) * scale), 0, 100000, len);
            MainWindow.Current.SetDragRectangles(rect1, rect2);
        }
        catch (Exception ex)
        {

        }
    }



    #endregion



    #region Background Image




    [ObservableProperty]
    private BitmapSource backgroundImage;





    private void InitializeBackgroundImage()
    {
        try
        {
            var file = _launcherService.GetCachedBackgroundImage(CurrentGameBiz);
            if (file != null)
            {
                BackgroundImage = new BitmapImage(new Uri(file));
                Color? color = null;
                if (AppConfig.EnableDynamicAccentColor)
                {
                    var hex = AppConfig.AccentColor;
                    if (!string.IsNullOrWhiteSpace(hex))
                    {
                        try
                        {
                            color = ColorHelper.ToColor(hex);
                        }
                        catch { }
                    }
                }
                MainWindow.Current.ChangeAccentColor(color);
            }
        }
        catch (Exception ex)
        {

        }
    }


    private CancellationTokenSource? source;


    private async Task UpdateBackgroundImageAsync()
    {
        try
        {
            source?.Cancel();
            source = new();
            var file = await _launcherService.GetBackgroundImageAsync(CurrentGameBiz);
            if (file != null)
            {
                using var fs = File.OpenRead(file);
                var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
                var bitmap = new WriteableBitmap((int)decoder.PixelWidth, (int)decoder.PixelHeight);
                fs.Position = 0;
                await bitmap.SetSourceAsync(fs.AsRandomAccessStream());
                var bytes = new byte[bitmap.PixelBuffer.Length];
                var ms = new MemoryStream(bytes);
                await bitmap.PixelBuffer.AsStream().CopyToAsync(ms);
                var sw = Stopwatch.StartNew();
                var color = GetPrimaryColor(bytes);
                sw.Stop();
                _logger.LogInformation(sw.ElapsedMilliseconds.ToString());
                if (source.IsCancellationRequested)
                {
                    return;
                }
                if (AppConfig.EnableDynamicAccentColor)
                {
                    MainWindow.Current.ChangeAccentColor(color);
                }
                BackgroundImage = bitmap;
            }
        }
        catch (Exception ex)
        {

        }
    }



    private Color? GetPrimaryColor(byte[] bytes)
    {
        if (bytes.Length % 4 == 0)
        {
            long b = 0, g = 0, r = 0, a = 0;
            for (int i = 0; i < bytes.Length; i += 4)
            {
                b += bytes[i];
                g += bytes[i + 1];
                r += bytes[i + 2];
                a += bytes[i + 3];
            }
            return Color.FromArgb((byte)(a * 4 / bytes.Length), (byte)(r * 4 / bytes.Length), (byte)(g * 4 / bytes.Length), (byte)(b * 4 / bytes.Length));
        }
        return null;
    }



    #endregion



    #region Navigate



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
                    nameof(GachaLogPage) => typeof(GachaLogPage),
                    nameof(SettingPage) => typeof(SettingPage),
                    _ => null,
                };
                if (type != null)
                {
                    NavigateTo(type);
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



    public void NavigateTo(Type page)
    {
        if (page != null)
        {
            MainPage_Frame.Navigate(page, CurrentGameBiz, new DrillInNavigationTransitionInfo());
        }
    }



    #endregion



}
