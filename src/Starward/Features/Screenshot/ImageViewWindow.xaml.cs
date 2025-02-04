using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Scighost.WinUI.ImageEx;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Storage;
using Windows.System;
using Windows.UI.Core;


namespace Starward.Features.Screenshot;

[INotifyPropertyChanged]
public sealed partial class ImageViewWindow : WindowEx
{

    private readonly ILogger<ImageViewWindow> _logger = AppService.GetLogger<ImageViewWindow>();


    public ImageViewWindow()
    {
        this.InitializeComponent();
        InitializeWindow();
        this.Closed += ImageViewWindow_Closed;
    }



    private void InitializeWindow()
    {
        Title = "Starward Screenshots Viewer";
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        CenterInScreen(1200, 676);
        AdaptTitleBarButtonColorToActuallTheme();
        SetDragRectangles(new RectInt32(0, 0, 100000, (int)(48 * UIScale)));
        SetIcon();
        _ScrollViewer_Image.MaxZoomFactor = (float)(2 / UIScale);
    }



    public new void Activate()
    {
        User32.ShowWindow((nint)AppWindow.Id.Value, ShowWindowCommand.SW_SHOWMAXIMIZED);
        base.Activate();
    }


    // ImageViewWindow 内存泄漏的问题还无法解决
    private void ImageViewWindow_Closed(object sender, WindowEventArgs args)
    {
        RootGrid.KeyDown -= RootGrid_KeyDown;
        _Image.SizeChanged -= _Image_SizeChanged;
        _Image.ImageExOpened -= _Image_ImageOpened;
        _ScrollViewer_Image.DoubleTapped -= _ScrollViewer_Image_DoubleTapped;
        _ScrollViewer_Image.PointerMoved -= _ScrollViewer_Image_PointerMoved;
        _ScrollViewer_Image.PointerPressed -= _ScrollViewer_Image_PointerPressed;
        _ScrollViewer_Image.PointerReleased -= _ScrollViewer_Image_PointerReleased;
        _ScrollViewer_Image.PointerWheelChanged -= _ScrollViewer_Image_PointerWheelChanged;
        _ScrollViewer_Image.Tapped -= _ScrollViewer_Image_Tapped;
        _ScrollViewer_Image.ViewChanged -= _ScrollViewer_Image_ViewChanged;
        Button_Close.Command = null;
        Button_Copy.Command = null;
        Button_FullScreen.Command = null;
        Button_OpenFile.Command = null;
        Button_Rotate.Command = null;
        Button_ZoomOut.Command = null;
        Button_ZoomIn.Command = null;
        CurrentImage = null!;
        ImageCollection = null;
        this.Bindings.StopTracking();
    }



    private void RootGrid_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        try
        {
            if (e.Handled)
            {
                return;
            }
            if (e.Key == VirtualKey.Escape)
            {
                Close();
                e.Handled = true;
            }
            if (e.Key == VirtualKey.F11)
            {
                FullScreen();
                e.Handled = true;
            }
        }
        catch { }
    }





    public bool DecodeFromStream { get; set; }


    public bool EnableLoadingRing { get; set; }




    public ScreenshotItem CurrentImage
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                _ScrollViewer_Image.HorizontalScrollMode = ScrollMode.Disabled;
                _ScrollViewer_Image.VerticalScrollMode = ScrollMode.Disabled;
            }
        }
    }


    public List<ScreenshotItem>? ImageCollection
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                if (value?.Any() ?? false)
                {
                    _GridView_ImageCollection.Visibility = Visibility.Visible;
                }
                else
                {
                    _GridView_ImageCollection.Visibility = Visibility.Collapsed;
                }
            }
        }
    }





    /// <summary>
    /// 缩放
    /// </summary>
    /// <param name="factor">缩放因子</param>
    private void Zoom(double factor)
    {
        var newFactor = Math.Clamp(factor, 0.1, 2 / UIScale);
        var oldFactor = _ScrollViewer_Image.ZoomFactor;
        var offset_Width = _ScrollViewer_Image.HorizontalOffset;
        var offset_Height = _ScrollViewer_Image.VerticalOffset;
        var viewport_Width = _ScrollViewer_Image.ViewportWidth;
        var viewport_Height = _ScrollViewer_Image.ViewportHeight;
        var extent_Width = _ScrollViewer_Image.ExtentWidth;
        var extent_Height = _ScrollViewer_Image.ExtentHeight;
        //var extent_Width_New = extent_Width * newFactor / oldFactor;
        //var extent_Height_New = extent_Height * newFactor / oldFactor;

        // 保证图片以画面中心为基点进行缩放
        double offset_Width_New = 0, offfset_Height_New = 0;
        if (extent_Width < viewport_Width)
        {
            // 如果缩放前图片宽度 < 视图宽度，则以图片半宽为基准缩放
            offset_Width_New = extent_Width / 2 * newFactor / oldFactor - viewport_Width / 2;
        }
        else
        {
            // 如果缩放前图片宽度 > 视图宽度，则以视图中心点到图片边缘为基准缩放
            offset_Width_New = (viewport_Width / 2 + offset_Width) * newFactor / oldFactor - viewport_Width / 2;
        }
        if (extent_Height < viewport_Height)
        {
            offfset_Height_New = extent_Height / 2 * newFactor / oldFactor - viewport_Height / 2;
        }
        else
        {
            offfset_Height_New = (viewport_Height / 2 + offset_Height) * newFactor / oldFactor - viewport_Height / 2;
        }

        // 缩放后若填不满视图，则图片居中（这部分没用）
        //if (extent_Width_New < viewport_Width)
        //{
        //    offset_Width_New = (extent_Width_New - viewport_Width) / 2;
        //}
        //if (extent_Height_New < viewport_Height)
        //{
        //    offfset_Height_New = (extent_Height_New - viewport_Height) / 2;
        //}

        _ScrollViewer_Image.ChangeView(offset_Width_New, offfset_Height_New, (float)newFactor);
    }



    /// <summary>
    /// 缩小
    /// </summary>
    [RelayCommand]
    private void ZoomOut()
    {
        // 缩放率调整为 0.1 的倍数
        var factor = Math.Ceiling(_ScrollViewer_Image.ZoomFactor * 10 * UIScale - 1.1f) / 10 / UIScale;
        Zoom(factor);
    }


    /// <summary>
    /// 放大
    /// </summary>
    [RelayCommand]
    private void ZoomIn()
    {
        var factor = Math.Floor(_ScrollViewer_Image.ZoomFactor * 10 * UIScale + 1.1f) / 10 / UIScale;
        Zoom(factor);
    }




    /// <summary>
    /// 顺时针旋转90°
    /// </summary>
    [RelayCommand]
    private void Rotate()
    {
        _Image.Rotation = _Image.Rotation + 90;
    }


    /// <summary>
    /// 复制图片
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task CopyImageAsync()
    {
        try
        {
            StorageFile? file = null;
            var uri = new Uri(CurrentImage.FullName);
            if (uri.Scheme is "ms-appx")
            {
                file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            }
            else if (uri.Scheme is "file")
            {
                file = await StorageFile.GetFileFromPathAsync(uri.ToString());
            }
            if (file is null)
            {
                _logger.LogWarning("Cannot find file: {file}", CurrentImage.FullName);
            }
            else
            {
                ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
                Button_Copy.Content = "\xE8FB";
                await Task.Delay(3000);
                Button_Copy.Content = "\xE8C8";
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Copy image");
        }
    }


    /// <summary>
    /// 打开图片
    /// </summary>
    /// <returns></returns>
    [RelayCommand]
    private async Task OpenFileAsync()
    {
        try
        {
            if (File.Exists(CurrentImage.FullName))
            {
                await Launcher.LaunchUriAsync(new Uri(CurrentImage.FullName));
            }
            else
            {
                _logger.LogWarning("Cannot find file: {file}", CurrentImage.FullName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Open file");
        }
    }



    /// <summary>
    /// 关闭页面
    /// </summary>
    [RelayCommand]
    private new void Close()
    {
        base.Close();
    }



    /// <summary>
    /// 缩放率文本变更
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _ScrollViewer_Image_ViewChanged(object? sender, ScrollViewerViewChangedEventArgs e)
    {
        _TextBlock_Factor.Text = (_ScrollViewer_Image.ZoomFactor * UIScale).ToString("P0");
    }



    /// <summary>
    /// 图片加载后计算合适的缩放率
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="_"></param>
    private void _Image_ImageOpened(object sender, ImageExOpenedEventArgs _)
    {
        _ScrollViewer_Image.HorizontalScrollMode = ScrollMode.Enabled;
        _ScrollViewer_Image.VerticalScrollMode = ScrollMode.Enabled;
        var width = _Image.ActualWidth;
        var height = _Image.ActualHeight;
        if (width * height == 0)
        {
            return;
        }
        _Image.CenterPoint = new System.Numerics.Vector3((float)(width / 2), (float)(height / 2), 0);
        var factor = GetFitZoomFactor();
        _TextBlock_Factor.Text = (factor * UIScale).ToString("P0");
        _ScrollViewer_Image.ZoomToFactor((float)factor);
    }


    /// <summary>
    /// 图片加载后计算合适的缩放率（部分无法触发 <see cref="MenuImage.ImageOpened"/> 的情况）
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="_"></param>
    private void _Image_SizeChanged(object sender, SizeChangedEventArgs _)
    {
        _ScrollViewer_Image.HorizontalScrollMode = ScrollMode.Enabled;
        _ScrollViewer_Image.VerticalScrollMode = ScrollMode.Enabled;
        var width = _Image.ActualWidth;
        var height = _Image.ActualHeight;
        if (width * height == 0)
        {
            return;
        }
        _Image.CenterPoint = new System.Numerics.Vector3((float)(width / 2), (float)(height / 2), 0);
        var factor = GetFitZoomFactor();
        _TextBlock_Factor.Text = (factor * UIScale).ToString("P0");
        _ScrollViewer_Image.ZoomToFactor((float)factor);
    }

    /// <summary>
    /// 计算适合窗口的图片缩放率
    /// </summary>
    /// <returns></returns>
    private double GetFitZoomFactor()
    {
        double factor;
        if (_Image.Rotation % 180 == 0)
        {
            var widthFactor = _ScrollViewer_Image.ViewportWidth / _Image.ActualWidth;
            var heightFactor = _ScrollViewer_Image.ViewportHeight / _Image.ActualHeight;
            factor = Math.Min(widthFactor, heightFactor);
        }
        else
        {
            var widthFactor = _ScrollViewer_Image.ViewportHeight / _Image.ActualWidth;
            var heightFactor = _ScrollViewer_Image.ViewportWidth / _Image.ActualHeight;
            factor = Math.Min(widthFactor, heightFactor);
        }
        return Math.Min(factor, 1 / UIScale);
    }


    /// <summary>
    /// 鼠标单击，显示或隐藏图片缩率图栏
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _ScrollViewer_Image_Tapped(object sender, TappedRoutedEventArgs e)
    {
        ChangeToolBarVisibility();
        //this.Focus(FocusState.Programmatic);
    }


    /// <summary>
    /// 更改工具栏的可见性
    /// </summary>
    private void ChangeToolBarVisibility()
    {
        if (_Border_ToolBar.IsHitTestVisible)
        {
            _Border_ToolBar.Opacity = 0;
            _Border_ToolBar.IsHitTestVisible = false;
            if (ImageCollection?.Count > 0)
            {
                _GridView_ImageCollection.Opacity = 0;
                _GridView_ImageCollection.IsHitTestVisible = false;
            }
        }
        else
        {
            _Border_ToolBar.Opacity = 1;
            _Border_ToolBar.IsHitTestVisible = true;
            if (ImageCollection?.Count > 0)
            {
                _GridView_ImageCollection.Opacity = 1;
                _GridView_ImageCollection.IsHitTestVisible = true;
            }
        }
    }


    /// <summary>
    /// 鼠标双击，放大或缩小
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _ScrollViewer_Image_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
    {
        ChangeToolBarVisibility();
        var oldFactor = _ScrollViewer_Image.ZoomFactor;
        double newFactor;
        if (oldFactor < 0.4 / UIScale)
        {
            newFactor = oldFactor * 2;
        }
        else if (oldFactor < 0.999999 / UIScale)
        {
            newFactor = 1 / UIScale;
        }
        else
        {
            newFactor = GetFitZoomFactor();
        }
        Zoom(newFactor);
    }


    /// <summary>
    /// 鼠标滚动，切换图片
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _ScrollViewer_Image_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
    {
        var stats = Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(VirtualKey.Control);
        if (stats is CoreVirtualKeyStates.None or CoreVirtualKeyStates.Locked)
        {
            if (ImageCollection?.Count > 0)
            {
                _GridView_ImageCollection.Focus(FocusState.Programmatic);
                var index = _GridView_ImageCollection.SelectedIndex;
                var count = ImageCollection.Count;
                var pointer = e.GetCurrentPoint(_ScrollViewer_Image);
                if (pointer.Properties.MouseWheelDelta < 0)
                {
                    if (index < count - 1)
                    {
                        _GridView_ImageCollection.SelectedIndex = index + 1;
                    }
                }
                else
                {
                    if (index > 0)
                    {
                        _GridView_ImageCollection.SelectedIndex = index - 1;
                    }
                }
            }
            // 阻止事件传递到滚动条
            e.Handled = true;
        }
    }



    private bool canImageMoved;

    private Point oldPosition;


    private void _ScrollViewer_Image_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        canImageMoved = true;
        oldPosition = e.GetCurrentPoint(_ScrollViewer_Image).Position;
    }

    /// <summary>
    /// 鼠标拖动图片
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void _ScrollViewer_Image_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (canImageMoved)
        {
            var pointer = e.GetCurrentPoint(_ScrollViewer_Image);
            if (pointer.Properties.IsLeftButtonPressed)
            {
                var deltaX = pointer.Position.X - oldPosition.X;
                var deltaY = pointer.Position.Y - oldPosition.Y;
                oldPosition = pointer.Position;
                // offset 的方向应与鼠标移动的方向相反
                // 不要使用 ChangeView，会出现图片无法跟随鼠标的情况
                _ScrollViewer_Image.ScrollToHorizontalOffset(_ScrollViewer_Image.HorizontalOffset - deltaX);
                _ScrollViewer_Image.ScrollToVerticalOffset(_ScrollViewer_Image.VerticalOffset - deltaY);
            }
        }
    }

    private void _ScrollViewer_Image_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        canImageMoved = false;
    }



    [RelayCommand]
    private void FullScreen()
    {
        if (AppWindow.Presenter.Kind is AppWindowPresenterKind.Overlapped)
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
            Button_FullScreen.Content = "\uE73F";
        }
        else
        {
            AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            Button_FullScreen.Content = "\uE740";
        }
    }


}
