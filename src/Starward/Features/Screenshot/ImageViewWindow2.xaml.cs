using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Effects;
using Microsoft.Graphics.Display;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Codec.ICC;
using Starward.Features.Codec;
using Starward.Features.Setting;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics;
using Windows.Graphics.DirectX;
using Windows.Storage;
using Windows.System;
using Windows.UI;


namespace Starward.Features.Screenshot;

[INotifyPropertyChanged]
public sealed partial class ImageViewWindow2 : Window
{


    public IntPtr WindowHandle { get; private init; }

    public double UIScale => Content.XamlRoot?.RasterizationScale ?? User32.GetDpiForWindow(WindowHandle) / 96.0;

    private const float MAX_ZOOM_FACTOR = 5f;

    private readonly ILogger<ImageViewWindow2> _logger = AppConfig.GetLogger<ImageViewWindow2>();



    public ImageViewWindow2()
    {
        InitializeComponent();
        SystemBackdrop = new MicaBackdrop();
        WindowHandle = (IntPtr)AppWindow.Id.Value;
        InitializeWindow();
        InitializeResource();
        WeakReferenceMessenger.Default.Register<LanguageChangedMessage>(this, (_, _) => this.Bindings.Update());
    }



    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        AppWindow.TitleBar.IconShowOptions = IconShowOptions.ShowIconAndSystemMenu;
        AppWindow.TitleBar.PreferredHeightOption = TitleBarHeightOption.Tall;
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
        ScrollViewer_Image.SetArePointerWheelEventsIgnored(true);
    }


    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        _lastUIScale = Content.XamlRoot.RasterizationScale;
        Content.XamlRoot.Changed += XamlRoot_Changed;
    }


    private void Grid_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        try
        {
            double x = StackPanel_LeftTopCommands.ActualWidth * UIScale;
            double width = (e.NewSize.Width - 144 - StackPanel_LeftTopCommands.ActualWidth - StackPanel_RightTopCommands.ActualWidth) * UIScale;
            AppWindow.TitleBar.SetDragRectangles([new RectInt32((int)x, 0, (int)width, (int)(48 * UIScale))]);
        }
        catch { }
    }


    private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            WeakReferenceMessenger.Default.UnregisterAll(this);
            _loadImageCts?.Cancel();
            _loadImageCts?.Dispose();
            CanvasSwapChainPanel_Image.SwapChain = null;
            CanvasSwapChainPanel_Image.RemoveFromVisualTree();
            CanvasSwapChainPanel_Image = null;
            _canvasSwapChain?.Dispose();
            _canvasSwapChain = null!;
            _sourceBitmap?.Dispose();
            _sourceBitmap = null!;
            _displayInformation?.AdvancedColorInfoChanged -= DisplayInformation_AdvancedColorInfoChanged;
            _displayInformation?.Dispose();
            _displayInformation = null!;
            ScreenshotCollection = null;
            CurrentScreenshot = null;

            RootGrid.Loaded -= RootGrid_Loaded;
            RootGrid.KeyDown -= RootGrid_KeyDown;
            RootGrid.Unloaded -= RootGrid_Unloaded;
            RootGrid.SizeChanged -= Grid_SizeChanged;
            ScrollViewer_Image.DoubleTapped -= ScrollViewer_Image_DoubleTapped;
            ScrollViewer_Image.PointerPressed -= ScrollViewer_Image_PointerPressed;
            ScrollViewer_Image.PointerMoved -= ScrollViewer_Image_PointerMoved;
            ScrollViewer_Image.PointerReleased -= ScrollViewer_Image_PointerReleased;
            ScrollViewer_Image.PointerWheelChanged -= ScrollViewer_Image_PointerWheelChanged;
            ScrollViewer_Image.DragOver -= ScrollViewer_Image_DragOver;
            ScrollViewer_Image.Drop -= ScrollViewer_Image_Drop;
            Slider_ZoomFactor.ManipulationDelta -= Slider_ZoomFactor_ManipulationDelta;
            GridView_ImageCollection.SelectionChanged -= GridView_ImageCollection_SelectionChanged;

            Button_EditImage.Click -= Button_EditImage_Click;
            MenuFlyoutItem_OpenNewFile.Click -= MenuFlyoutItem_OpenNewFile_Click;
            MenuFlyoutItem_OpenInExplorer.Click -= MenuFlyoutItem_OpenInExplorer_Click;
            MenuFlyoutItem_OpenWith.Click -= MenuFlyoutItem_OpenWith_Click;
            MenuFlyoutItem_OpenWithDefault.Click -= MenuFlyoutItem_OpenWithDefault_Click;
            MenuFlyoutItem_CopyFile.Click -= MenuFlyoutItem_CopyFile_Click;
            MenuFlyoutItem_CopyPath.Click -= MenuFlyoutItem_CopyPath_Click;
            MenuFlyoutItem_CopyImage.Click -= MenuFlyoutItem_CopyImage_Click;
            Button_DeleteImage.Click -= Button_DeleteImage_Click;
            Button_OpenFullScreen.Click -= Button_OpenFullScreen_Click;
            Button_PreviousImage.Click -= Button_PreviousImage_Click;
            Button_NextImage.Click -= Button_NextImage_Click;
            Button_Minimize.Click -= Button_Minimize_Click;
            Button_CloseFullScreen.Click -= Button_CloseFullScreen_Click;
            Button_CloseWindow.Click -= Button_CloseWindow_Click;
            Button_ZoomToFitFactor.Click -= Button_ZoomToFitFactor_Click;
            Button_ZoomOut.Click -= Button_ZoomOut_Click;
            Button_ZoomIn.Click -= Button_ZoomIn_Click;
            Button_CloseEditGrid.Click -= Button_CloseEditGrid_Click;
            Button_ExportImage.Click -= Button_ExportImage_Click;
            Button_OpenFiles.Click -= MenuFlyoutItem_OpenNewFile_Click;
        }
        catch { }
    }


    private void ResetState()
    {
        try
        {
            CurrentFilePath = "";
            CurrentFileName = "";
            CurrentPixelSizeText = "-";
            CurrentFileSizeText = "-";
            CurrentScreenshot = null;
            ScreenshotCollection = null;
            IsHDRImage = false;
            MaxCLL = 0;
            ImageInformationText = "-";
            CanvasSwapChainPanel_Image.Width = 1;
            CanvasSwapChainPanel_Image.Height = 1;
            ScrollViewer_Image.ZoomToFactor(1);
            _sourceBitmap?.Dispose();
            _sourceBitmap = null!;
            _canvasSwapChain.ResizeBuffers(1, 1);
            using var ds = _canvasSwapChain.CreateDrawingSession(Colors.Transparent);
            InfoBar_Tips.IsOpen = false;
            StackPanel_NoImage.Visibility = Visibility.Visible;
            StackPanel_DisplayImageError.Visibility = Visibility.Collapsed;
            ToggleButton_ShowGallery.IsChecked = false;
            ToggleButton_ShowGallery.IsEnabled = false;
            TextBlock_ImageIndex.Visibility = Visibility.Collapsed;
        }
        catch { }
    }




    #region Info


    public string CurrentFilePath { get; set; }


    public string CurrentFileName { get; set => SetProperty(ref field, value); }


    public string CurrentPixelSizeText { get; set => SetProperty(ref field, value); }


    public string CurrentFileSizeText { get; set => SetProperty(ref field, value); }


    public ScreenshotItem? CurrentScreenshot { get; set => SetProperty(ref field, value); }


    public ObservableCollection<ScreenshotItem>? ScreenshotCollection { get; set => SetProperty(ref field, value); }


    public bool IsHDRImage { get; set => SetProperty(ref field, value); }


    public float MaxCLL { get; set => SetProperty(ref field, value); }


    public string ImageInformationText { get; set => SetProperty(ref field, value); }


    public string MonitorInformationText { get; set => SetProperty(ref field, value); }


    private ColorPrimaries MonitorColorPrimaries { get => field ?? ColorPrimaries.BT709; set; }



    private void UpdateImageInformation(CanvasBitmap bitmap)
    {
        try
        {
            if (bitmap.Format is not DirectXPixelFormat.B8G8R8A8UIntNormalized and not DirectXPixelFormat.R8G8B8A8UIntNormalized)
            {
                IsHDRImage = true;
                MaxCLL = ScreenCaptureService.GetMaxCLL(bitmap);
            }
            else
            {
                IsHDRImage = false;
                MaxCLL = 0;
            }
            ImageInformationText = $"""
                {Lang.GenshinHDRLuminanceSettingDialog_ColorSpace}: {(IsHDRImage ? "HDR" : "SDR")}
                {Lang.GenshinHDRLuminanceSettingDialog_MaxLuminance}: {(IsHDRImage ? ($"{MaxCLL:F0} nits") : "-")}
                """;
        }
        catch { }
    }


    private void UpdateMonitorInformation(DisplayInformation displayInformation)
    {
        try
        {
            var info = displayInformation.GetAdvancedColorInfo();
            string kind = info.CurrentAdvancedColorKind switch
            {
                DisplayAdvancedColorKind.StandardDynamicRange => $"SDR",
                DisplayAdvancedColorKind.WideColorGamut => $"WCG",
                DisplayAdvancedColorKind.HighDynamicRange => $"HDR",
                _ => "",
            };
            MonitorInformationText = $"""
                {Lang.GenshinHDRLuminanceSettingDialog_ColorSpace}: {kind}
                {Lang.GenshinHDRLuminanceSettingDialog_PeakLuminance}: {info.MaxLuminanceInNits} nits
                {Lang.GenshinHDRLuminanceSettingDialog_MaxFullScreenLuminance}: {info.MaxAverageFullFrameLuminanceInNits} nits
                {Lang.GenshinHDRLuminanceSettingDialog_SDRWhiteLuminance}: {info.SdrWhiteLevelInNits} nits
                """;
        }
        catch { }
    }


    private void UpdateMonitorColorPrimaries(DisplayInformation displayInformation)
    {
        try
        {
            var stream = displayInformation.GetColorProfile();
            if (stream is not null)
            {
                byte[] bytes = new byte[stream.Size];
                stream.AsStream().ReadExactly(bytes);
                MonitorColorPrimaries = ICCHelper.GetColorPrimariesFromIccData(bytes);
            }
            else
            {
                var info = displayInformation.GetAdvancedColorInfo();
                MonitorColorPrimaries = info.CurrentAdvancedColorKind switch
                {
                    DisplayAdvancedColorKind.WideColorGamut => ColorPrimaries.BT709,
                    DisplayAdvancedColorKind.HighDynamicRange => ColorPrimaries.BT709,
                    DisplayAdvancedColorKind.StandardDynamicRange => new ColorPrimaries
                    {
                        Red = Unsafe.BitCast<Point, Vector2>(info.RedPrimary),
                        Green = Unsafe.BitCast<Point, Vector2>(info.GreenPrimary),
                        Blue = Unsafe.BitCast<Point, Vector2>(info.BluePrimary),
                        White = Unsafe.BitCast<Point, Vector2>(info.WhitePoint),
                    },
                    _ => null!,
                };
            }
        }
        catch
        {
            MonitorColorPrimaries = ColorPrimaries.BT709;
        }
    }


    #endregion



    #region Zoom



    private bool _canImageMoved;

    private Point _imageMoveOldPosition;


    private void ScrollViewer_Image_PointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _canImageMoved = true;
        ScrollViewer_Image.CapturePointer(e.Pointer);
        _imageMoveOldPosition = e.GetCurrentPoint(ScrollViewer_Image).Position;
    }


    private void ScrollViewer_Image_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (_canImageMoved)
        {
            var point = e.GetCurrentPoint(ScrollViewer_Image);
            if (point.Properties.IsLeftButtonPressed)
            {
                var deltaX = point.Position.X - _imageMoveOldPosition.X;
                var deltaY = point.Position.Y - _imageMoveOldPosition.Y;
                _imageMoveOldPosition = point.Position;
                ScrollViewer_Image.ChangeView(ScrollViewer_Image.HorizontalOffset - deltaX, ScrollViewer_Image.VerticalOffset - deltaY, null, true);
            }
        }
    }


    private void ScrollViewer_Image_PointerReleased(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        _canImageMoved = false;
        ScrollViewer_Image.ReleasePointerCapture(e.Pointer);
    }


    private void ScrollViewer_Image_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        try
        {
            float oldFactor = ScrollViewer_Image.ZoomFactor;
            float? fitFactor = GetFitZoomFactor();
            float? newFactor = null;
            if (fitFactor.HasValue && fitFactor < 1)
            {
                newFactor = oldFactor switch
                {
                    < 0.4f => oldFactor * 2,
                    < 0.9999f => 1,
                    _ => fitFactor,
                };
            }
            else if (fitFactor.HasValue && fitFactor >= 1)
            {
                newFactor = oldFactor switch
                {
                    > 0.9999f and < 1.0001f => fitFactor,
                    _ => 1,
                };
            }
            if (newFactor.HasValue)
            {
                Zoom(newFactor.Value, e.GetPosition(ScrollViewer_Image));
            }
        }
        catch { }
    }


    private void ScrollViewer_Image_PointerWheelChanged(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        try
        {
            int delta = e.GetCurrentPoint(ScrollViewer_Image).Properties.MouseWheelDelta;
            if (delta > 0)
            {
                LoadPreviewImage();
            }
            else if (delta < 0)
            {
                LoadNextImage();
            }
        }
        catch { }
    }


    private void Slider_ZoomFactor_ManipulationDelta(object sender, Microsoft.UI.Xaml.Input.ManipulationDeltaRoutedEventArgs e)
    {
        Zoom(Slider_ZoomFactor.Value, null);
    }


    private void Button_ZoomToFitFactor_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            float? fitFactor = GetFitZoomFactor();
            if (fitFactor.HasValue)
            {
                if (MathF.Abs(fitFactor.Value - ScrollViewer_Image.ZoomFactor) < 0.0001f)
                {
                    Zoom(1);
                }
                else
                {
                    Zoom(fitFactor.Value);
                }
            }
        }
        catch { }
    }


    private void Button_ZoomOut_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            float oldFactor = ScrollViewer_Image.ZoomFactor;
            float newFactor = oldFactor switch
            {
                > 2.0001f => oldFactor - 0.2501f,
                _ => oldFactor - 0.1001f,
            };
            newFactor = newFactor switch
            {
                > 2.0001f => MathF.Ceiling(newFactor * 4) / 4,
                _ => MathF.Ceiling(newFactor * 10) / 10,
            };
            Zoom(newFactor, null);
        }
        catch { }
    }


    private void Button_ZoomIn_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            float oldFactor = ScrollViewer_Image.ZoomFactor;
            float newFactor = oldFactor switch
            {
                < 1.9999f => oldFactor + 0.1001f,
                _ => oldFactor + 0.2501f,
            };
            newFactor = newFactor switch
            {
                < 2f => MathF.Floor(newFactor * 10) / 10,
                _ => MathF.Floor(newFactor * 4) / 4,
            };
            Zoom(newFactor, null);
        }
        catch { }
    }


    private float? GetFitZoomFactor()
    {
        double scrollWidth = ScrollViewer_Image.ActualWidth;
        double scrollHeight = ScrollViewer_Image.ActualHeight;
        double imageWidth = CanvasSwapChainPanel_Image.Width;
        double imageHeight = CanvasSwapChainPanel_Image.Height;
        if (scrollWidth == 0 || scrollHeight == 0 || imageWidth == 0 || imageHeight == 0)
        {
            return null;
        }
        return (float)Math.Min(scrollWidth / imageWidth, scrollHeight / imageHeight);
    }


    private void ResetZoomFactor()
    {
        try
        {
            ScrollViewer_Image.UpdateLayout();
            float? fitFactor = GetFitZoomFactor();
            if (fitFactor.HasValue)
            {
                ScrollViewer_Image.ZoomToFactor(MathF.Min(fitFactor.Value, 1));
            }
        }
        catch { }
    }


    private void Zoom(double factor, Point? centerPoint = null)
    {
        try
        {
            double new_factor = Math.Clamp(factor, 0.1, MAX_ZOOM_FACTOR);
            double old_factor = ScrollViewer_Image.ZoomFactor;
            if (new_factor == old_factor)
            {
                return;
            }
            double offset_x = ScrollViewer_Image.HorizontalOffset;
            double offset_y = ScrollViewer_Image.VerticalOffset;
            double viewport_width = ScrollViewer_Image.ViewportWidth;
            double viewport_height = ScrollViewer_Image.ViewportHeight;
            double extent_width = ScrollViewer_Image.ExtentWidth;
            double extent_height = ScrollViewer_Image.ExtentHeight;

            double fictor_scale = new_factor / old_factor;
            double fit_factor = GetFitZoomFactor() ?? 1;
            if (new_factor <= fit_factor)
            {
                ScrollViewer_Image.ChangeView(0, 0, (float)new_factor);
                return;
            }

            Rect image_rect = new Rect(extent_width < viewport_width ? ((viewport_width - extent_width) / 2) : -offset_x,
                                       extent_height < viewport_height ? ((viewport_height - extent_height) / 2) : -offset_y,
                                       extent_width, extent_height);

            if (!centerPoint.HasValue || !image_rect.Contains(centerPoint.Value))
            {
                centerPoint = new Point(viewport_width / 2, viewport_height / 2);
            }

            Rect image_rect_new = new Rect();
            image_rect_new.X = (image_rect.X - centerPoint.Value.X) * fictor_scale + centerPoint.Value.X;
            image_rect_new.Y = (image_rect.Y - centerPoint.Value.Y) * fictor_scale + centerPoint.Value.Y;
            image_rect_new.Width = image_rect.Width * fictor_scale;
            image_rect_new.Height = image_rect.Height * fictor_scale;

            double offset_x_new = -image_rect_new.X;
            double offset_y_new = -image_rect_new.Y;
            ScrollViewer_Image.ChangeView(offset_x_new, offset_y_new, (float)new_factor);
        }
        catch { }
    }


    #endregion



    #region Image


    private DisplayInformation _displayInformation;

    private CanvasSwapChain _canvasSwapChain;

    private CanvasBitmap _sourceBitmap;

    private CancellationTokenSource _loadImageCts;

    private double _lastUIScale;

    private ColorPrimaries ImageColorPrimaries { get => field ?? ColorPrimaries.BT709; set; }


    private void InitializeResource()
    {
        _displayInformation = DisplayInformation.CreateForWindowId(AppWindow.Id);
        _displayInformation.AdvancedColorInfoChanged += DisplayInformation_AdvancedColorInfoChanged;
        UpdateMonitorInformation(_displayInformation);
        UpdateMonitorColorPrimaries(_displayInformation);
        _canvasSwapChain = new CanvasSwapChain(CanvasDevice.GetSharedDevice(), 1, 1, 96, DirectXPixelFormat.R16G16B16A16Float, 6, CanvasAlphaMode.Premultiplied);
        CanvasSwapChainPanel_Image.SwapChain = _canvasSwapChain;
    }


    private void XamlRoot_Changed(XamlRoot sender, XamlRootChangedEventArgs args)
    {
        if (_lastUIScale != sender.RasterizationScale)
        {
            DrawImage();
            _lastUIScale = sender.RasterizationScale;
        }
    }


    private void DisplayInformation_AdvancedColorInfoChanged(DisplayInformation sender, object args)
    {
        UpdateMonitorInformation(sender);
        UpdateMonitorColorPrimaries(sender);
        DrawImage();
    }


    private void GridView_ImageCollection_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.AddedItems.FirstOrDefault() is ScreenshotItem item && item != CurrentScreenshot)
        {
            CurrentScreenshot = item;
            _ = LoadImageAsync(item.FilePath);
        }
    }


    private void Button_PreviousImage_Click(object sender, RoutedEventArgs e)
    {
        LoadPreviewImage();
    }


    private void Button_NextImage_Click(object sender, RoutedEventArgs e)
    {
        LoadNextImage();
    }


    private async Task LoadImageAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                ProgressBar_ImageLoading.Visibility = Visibility.Visible;
                _loadImageCts?.Cancel();
                _loadImageCts = new();
                CancellationToken token = _loadImageCts.Token;
                CanvasBitmap? bitmap = null;
                CurrentFilePath = filePath;
                CurrentFileName = Path.GetFileName(filePath);
                var imageInfo = await ImageLoader.LoadImageAsync(filePath, token);
                ImageColorPrimaries = imageInfo.ColorPrimaries ?? ColorPrimaries.BT709;
                bitmap = imageInfo.CanvasBitmap;
                if (token.IsCancellationRequested)
                {
                    bitmap.Dispose();
                    return;
                }
                _sourceBitmap?.Dispose();
                _sourceBitmap = bitmap;
                CurrentFileSizeText = GetSizeText(new FileInfo(filePath).Length);
                CurrentPixelSizeText = $"{_sourceBitmap.SizeInPixels.Width} x {_sourceBitmap.SizeInPixels.Height}";
                UpdateImageInformation(_sourceBitmap);
                DrawImage(true);
                ResetZoomFactor();
                CanvasSwapChainPanel_Image.Visibility = Visibility.Visible;
                StackPanel_NoImage.Visibility = Visibility.Collapsed;
                StackPanel_DisplayImageError.Visibility = Visibility.Collapsed;
                ProgressBar_ImageLoading.Visibility = Visibility.Collapsed;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            CurrentFileSizeText = "-";
            CurrentPixelSizeText = "-";
            CanvasSwapChainPanel_Image.Visibility = Visibility.Collapsed;
            StackPanel_NoImage.Visibility = Visibility.Collapsed;
            StackPanel_DisplayImageError.Visibility = Visibility.Visible;
            ProgressBar_ImageLoading.Visibility = Visibility.Collapsed;
            TextBlock_DisplayImageError.Text = ex.Message;
            _logger.LogError(ex, "Failed to load image: {FilePath}", filePath);
        }
    }


    private async Task LoadImageAsync(IStorageFile file)
    {
        try
        {
            ProgressBar_ImageLoading.Visibility = Visibility.Visible;
            _loadImageCts?.Cancel();
            _loadImageCts = new();
            CancellationToken token = _loadImageCts.Token;
            CanvasBitmap? bitmap = null;
            CurrentFilePath = file.Path;
            CurrentFileName = file.Name;
            var imageInfo = await ImageLoader.LoadImageAsync(file.Path, token);
            ImageColorPrimaries = imageInfo.ColorPrimaries ?? ColorPrimaries.BT709;
            bitmap = imageInfo.CanvasBitmap;
            if (token.IsCancellationRequested)
            {
                bitmap.Dispose();
                return;
            }
            _sourceBitmap?.Dispose();
            _sourceBitmap = bitmap;
            CurrentFileSizeText = GetSizeText((long)(await file.GetBasicPropertiesAsync()).Size);
            CurrentPixelSizeText = $"{_sourceBitmap.SizeInPixels.Width} x {_sourceBitmap.SizeInPixels.Height}";
            UpdateImageInformation(_sourceBitmap);
            DrawImage(true);
            ResetZoomFactor();
            CanvasSwapChainPanel_Image.Visibility = Visibility.Visible;
            StackPanel_NoImage.Visibility = Visibility.Collapsed;
            StackPanel_DisplayImageError.Visibility = Visibility.Collapsed;
            ProgressBar_ImageLoading.Visibility = Visibility.Collapsed;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            CurrentFileSizeText = "-";
            CurrentPixelSizeText = "-";
            CanvasSwapChainPanel_Image.Visibility = Visibility.Collapsed;
            StackPanel_NoImage.Visibility = Visibility.Collapsed;
            StackPanel_DisplayImageError.Visibility = Visibility.Visible;
            ProgressBar_ImageLoading.Visibility = Visibility.Collapsed;
            TextBlock_DisplayImageError.Text = ex.Message;
            _logger.LogError(ex, "Failed to load image");
        }
    }


    private static string GetSizeText(long size)
    {
        const double MB = 1 << 20;
        if (size < (1 << 20))
        {
            return $"{size / 1024.0:F2} KB";
        }
        else
        {
            return $"{size / MB:F2} MB";
        }
    }


    private void DrawImage(bool throwError = false)
    {
        try
        {
            if (_sourceBitmap is null || _canvasSwapChain is null)
            {
                return;
            }
            double uiScale = UIScale;
            double width = _sourceBitmap.SizeInPixels.Width / uiScale;
            double height = _sourceBitmap.SizeInPixels.Height / uiScale;
            uint dpi = User32.GetDpiForWindow(WindowHandle);
            if (width != _canvasSwapChain.Size.Width || height != _canvasSwapChain.Size.Height || dpi != _canvasSwapChain.Dpi)
            {
                _canvasSwapChain.ResizeBuffers((float)width, (float)height, User32.GetDpiForWindow(WindowHandle));
                CanvasSwapChainPanel_Image.Width = width;
                CanvasSwapChainPanel_Image.Height = height;
            }
            using (var ds = _canvasSwapChain.CreateDrawingSession(Colors.Transparent))
            {
                ds.Units = CanvasUnits.Pixels;
                ICanvasImage output = GetDrawOutput(out _);
                ds.DrawImage(output);
            }
            _canvasSwapChain.Present();
        }
        catch
        {
            if (throwError)
            {
                throw;
            }
        }
    }


    private void LoadPreviewImage()
    {
        try
        {
            if (CurrentScreenshot is null || ScreenshotCollection is null || ScreenshotCollection.Count < 2)
            {
                return;
            }
            int index = ScreenshotCollection.IndexOf(CurrentScreenshot);
            if (index > 0)
            {
                CurrentScreenshot = ScreenshotCollection[index - 1];
                _ = LoadImageAsync(CurrentScreenshot.FilePath);
            }
        }
        catch { }
    }


    private void LoadNextImage()
    {
        try
        {
            if (CurrentScreenshot is null || ScreenshotCollection is null || ScreenshotCollection.Count < 2)
            {
                return;
            }
            int index = ScreenshotCollection.IndexOf(CurrentScreenshot);
            if (index < ScreenshotCollection.Count - 1)
            {
                CurrentScreenshot = ScreenshotCollection[index + 1];
                _ = LoadImageAsync(CurrentScreenshot.FilePath);
            }
        }
        catch { }
    }


    #endregion



    #region Effect


    /// <summary>
    /// Auto, SDR, HDR
    /// </summary>
    public int DisplayMode
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                DrawImage();
            }
        }
    }


    public float SDRLuminance
    {
        get; set
        {
            if (SetProperty(ref field, value))
            {
                DrawImage();
            }
        }
    } = 300;



    public ICanvasImage GetDrawOutput(out int displayMode)
    {
        displayMode = 0;
        ICanvasImage output = _sourceBitmap;
        var colorInfo = _displayInformation.GetAdvancedColorInfo();
        bool monitorIsHDR = colorInfo.CurrentAdvancedColorKind is DisplayAdvancedColorKind.HighDynamicRange;
        float sdrWhiteLevel = (float)colorInfo.SdrWhiteLevelInNits;
        if (_sourceBitmap.Format is DirectXPixelFormat.B8G8R8A8UIntNormalized or DirectXPixelFormat.R8G8B8A8UIntNormalized)
        {
            // SDR 图像
            displayMode = 1;
            var gammaEffect = new SrgbGammaEffect
            {
                Source = _sourceBitmap,
                GammaMode = SrgbGammaMode.EOTF,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            output = new WhiteLevelAdjustmentEffect
            {
                Source = gammaEffect,
                InputWhiteLevel = sdrWhiteLevel,
                OutputWhiteLevel = 80,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
        }
        else if (DisplayMode is 1 || (DisplayMode is 0 && !monitorIsHDR))
        {
            // SDR 显示模式
            displayMode = 1;
            var toneMapEffect = new HdrToneMapEffect
            {
                Source = _sourceBitmap,
                DisplayMode = HdrToneMapEffectDisplayMode.Hdr,
                InputMaxLuminance = MaxCLL,
                OutputMaxLuminance = SDRLuminance,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
            output = new WhiteLevelAdjustmentEffect
            {
                Source = toneMapEffect,
                InputWhiteLevel = sdrWhiteLevel,
                OutputWhiteLevel = SDRLuminance,
                BufferPrecision = CanvasBufferPrecision.Precision16Float,
            };
        }
        else
        {
            displayMode = 2;
        }
        output = new ColorMatrixEffect
        {
            Source = output,
            ColorMatrix = ToMatrix5x4(ColorPrimaries.GetColorTransferMatrix(ImageColorPrimaries, MonitorColorPrimaries)),
            ClampOutput = false,
            BufferPrecision = CanvasBufferPrecision.Precision16Float,
        };
        return output;
    }



    #endregion



    #region Operation


    public async Task ShowWindowAsync(Microsoft.UI.WindowId windowId, ScreenshotItem screenshotItem, ObservableCollection<ScreenshotItem>? collection)
    {
        try
        {
            var parentWindow = AppWindow.GetFromWindowId(windowId);
            PointInt32 point = parentWindow.Position;
            SizeInt32 size = parentWindow.Size;
            CurrentScreenshot = screenshotItem;
            ScreenshotCollection = collection;
            AppWindow.MoveAndResize(new RectInt32(point.X, point.Y, size.Width, size.Height));
            User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_SHOWMAXIMIZED);
            await Task.Delay(1);
            await LoadImageAsync(screenshotItem.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show image view window");
        }
    }


    public async Task ShowWindowAsync(Microsoft.UI.WindowId windowId, string file, bool showGallary)
    {
        try
        {
            if (showGallary)
            {
                string? folder = Path.GetDirectoryName(file);
                if (Directory.Exists(folder))
                {
                    var list = Directory.GetFiles(folder)
                                        .Where(ScreenshotHelper.IsSupportedExtension)
                                        .Select(x => new ScreenshotItem(x))
                                        .OrderByDescending(x => x.CreationTime)
                                        .ToList();
                    ScreenshotCollection = new(list);
                }
            }
            if (ScreenshotCollection?.FirstOrDefault(x => x.FilePath == file) is ScreenshotItem item)
            {
                CurrentScreenshot = item;
            }
            else
            {
                CurrentScreenshot = new ScreenshotItem(file);
                ScreenshotCollection ??= new();
                ScreenshotCollection.Insert(0, CurrentScreenshot);
            }
            DisplayArea area = DisplayArea.GetFromWindowId(windowId, DisplayAreaFallback.Nearest);
            CenterInScreen(area, 1200, 676);
            User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_SHOWMAXIMIZED);
            await Task.Delay(1);
            await LoadImageAsync(CurrentScreenshot.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show image view window");
        }
    }


    public void ShowWindow(Microsoft.UI.WindowId windowId)
    {
        try
        {
            StackPanel_NoImage.Visibility = Visibility.Visible;
            var parentWindow = AppWindow.GetFromWindowId(windowId);
            PointInt32 point = parentWindow.Position;
            SizeInt32 size = parentWindow.Size;
            AppWindow.MoveAndResize(new RectInt32(point.X, point.Y, size.Width, size.Height));
            User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_SHOWMAXIMIZED);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to show image view window");
        }
    }


    private void Button_EditImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Grid_EditImage.Visibility = Grid_EditImage.Visibility is Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
            ResetZoomFactor();
        }
        catch { }
    }


    private async void MenuFlyoutItem_OpenNewFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = await FileDialogHelper.PickMultipleFilesAsync(Content.XamlRoot);
            var items = files.Where(ScreenshotHelper.IsSupportedExtension).Select(x => new ScreenshotItem(x)).ToList();
            if (items.Count == 0)
            {
                return;
            }
            ScreenshotCollection = new(items);
            CurrentScreenshot = items.First();
            await LoadImageAsync(CurrentScreenshot.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open new files");
        }
    }


    private async void MenuFlyoutItem_OpenInExplorer_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(CurrentFilePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(CurrentFilePath);
                var options = new FolderLauncherOptions();
                options.ItemsToSelect.Add(file);
                await Launcher.LaunchFolderAsync(await file.GetParentAsync(), options);
            }
            else
            {
                ShowInfo(InfoBarSeverity.Warning, Lang.ImageViewWindow2_FileDoesNotExist, "", 5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file in explorer");
        }
    }


    private async void MenuFlyoutItem_OpenWithDefault_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(CurrentFilePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(CurrentFilePath);
                await Launcher.LaunchFileAsync(file);
            }
            else
            {
                ShowInfo(InfoBarSeverity.Warning, Lang.ImageViewWindow2_FileDoesNotExist, "", 5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file with default application");
        }
    }


    private async void MenuFlyoutItem_OpenWith_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(CurrentFilePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(CurrentFilePath);
                var options = new LauncherOptions { DisplayApplicationPicker = true };
                await Launcher.LaunchFileAsync(file, options);
            }
            else
            {
                ShowInfo(InfoBarSeverity.Warning, Lang.ImageViewWindow2_FileDoesNotExist, "", 5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to open file with application picker");
        }
    }


    private async void MenuFlyoutItem_CopyFile_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(CurrentFilePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(CurrentFilePath);
                ClipboardHelper.SetStorageItems(DataPackageOperation.Copy, file);
                ShowInfo(InfoBarSeverity.Success, Lang.ImageViewWindow2_CopiedToClipboard, "", 2000);
            }
            else
            {
                ShowInfo(InfoBarSeverity.Warning, Lang.ImageViewWindow2_FileDoesNotExist, "", 5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file to clipboard");
        }
    }


    private void MenuFlyoutItem_CopyPath_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(CurrentFilePath))
            {
                ClipboardHelper.SetText(CurrentFilePath);
                ShowInfo(InfoBarSeverity.Success, Lang.ImageViewWindow2_CopiedToClipboard, "", 2000);
            }
            else
            {
                ShowInfo(InfoBarSeverity.Warning, Lang.ImageViewWindow2_FileDoesNotExist, "", 5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy file path to clipboard");
        }
    }


    private async void MenuFlyoutItem_CopyImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(CurrentFilePath))
            {
                var file = await StorageFile.GetFileFromPathAsync(CurrentFilePath);
                ClipboardHelper.SetBitmap(file);
                ShowInfo(InfoBarSeverity.Success, Lang.ImageViewWindow2_CopiedToClipboard, "", 2000);
            }
            else
            {
                ShowInfo(InfoBarSeverity.Warning, Lang.ImageViewWindow2_FileDoesNotExist, "", 5000);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy image to clipboard");
        }
    }



    private async void Button_DeleteImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (File.Exists(CurrentFilePath))
            {
                ScreenshotItem? next = null;
                if (CurrentScreenshot is not null && ScreenshotCollection is not null)
                {
                    int index = ScreenshotCollection.IndexOf(CurrentScreenshot);
                    if (index + 1 < ScreenshotCollection.Count)
                    {
                        next = ScreenshotCollection[index + 1];
                        ScreenshotCollection.Remove(CurrentScreenshot);
                    }
                    else
                    {
                        ScreenshotCollection.Remove(CurrentScreenshot);
                        next = ScreenshotCollection.LastOrDefault();
                    }
                }
                var file = await StorageFile.GetFileFromPathAsync(CurrentFilePath);
                await file.DeleteAsync();
                if (next is not null)
                {
                    CurrentScreenshot = next;
                    await LoadImageAsync(CurrentScreenshot.FilePath);
                }
                else
                {
                    ResetState();
                }
                Flyout_DeleteImage.Hide();
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            // TODO 使用 RPC 删除
            ShowInfo(InfoBarSeverity.Warning, Lang.ImageViewWindow2_UnableToDeleteTheFile, Lang.ImageViewWindow2_InsufficientPermissionsOrTheFileIsInUse, 5000);
            _logger.LogError(ex, "Failed to delete image file");
        }
        catch (Exception ex)
        {
            ShowInfo(InfoBarSeverity.Error, Lang.ImageViewWindow2_FailedToDeleteImageFile, ex.Message, 0);
            _logger.LogError(ex, "Failed to delete image file");
        }
    }


    private void Button_OpenFullScreen_Click(object sender, RoutedEventArgs e)
    {
        FullScreen();
    }


    private void Button_Minimize_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            User32.ShowWindow(WindowHandle, ShowWindowCommand.SW_MINIMIZE);
        }
        catch { }
    }


    private void Button_CloseFullScreen_Click(object sender, RoutedEventArgs e)
    {
        FullScreen();
    }


    private void Button_CloseWindow_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Close();
        }
        catch { }
    }


    private void FullScreen()
    {
        try
        {
            if (AppWindow.Presenter is OverlappedPresenter)
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.FullScreen);
                RowDefinition_0.Height = new GridLength(0);
                RowDefinition_2.Height = new GridLength(0);
                RowDefinition_3.Height = new GridLength(0);
                ColumnDefinition_1.Width = new GridLength(0);
                StackPanel_FullScreenWindowCaption.Visibility = Visibility.Visible;
            }
            else
            {
                AppWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
                RowDefinition_0.Height = new GridLength(48);
                RowDefinition_2.Height = new GridLength(0, GridUnitType.Auto);
                RowDefinition_3.Height = new GridLength(48);
                ColumnDefinition_1.Width = new GridLength(0, GridUnitType.Auto);
                StackPanel_FullScreenWindowCaption.Visibility = Visibility.Collapsed;
            }
        }
        catch { }
    }


    private void ScrollViewer_Image_DragOver(object sender, DragEventArgs e)
    {
        try
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
        catch { }
    }


    private async void ScrollViewer_Image_Drop(object sender, DragEventArgs e)
    {
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();
            if (items.Count == 1 && items[0] is StorageFile { Path: "" } image && ScreenshotHelper.IsSupportedExtension(image.FileType))
            {
                // 从浏览器或其他应用拖入的非本地文件图片
                await LoadImageAsync(image);
                CurrentScreenshot = null;
                ScreenshotCollection = null;
                ToggleButton_ShowGallery.IsChecked = false;
                ToggleButton_ShowGallery.IsEnabled = false;
                TextBlock_ImageIndex.Visibility = Visibility.Collapsed;
                return;
            }

            var list = new List<string>();
            foreach (var item in items)
            {
                if (item is StorageFile { Path: not "" } file && ScreenshotHelper.IsSupportedExtension(file.FileType))
                {
                    list.Add(file.Path);
                }
                else if (item is StorageFolder folder)
                {
                    var files = await folder.GetFilesAsync();
                    list.AddRange(files.Where(x => ScreenshotHelper.IsSupportedExtension(x.FileType)).Select(x => x.Path));
                }
            }
            var screenshotItems = list.Select(x => new ScreenshotItem(x)).ToList();
            if (screenshotItems.Count == 0)
            {
                return;
            }
            ScreenshotCollection = new(screenshotItems);
            CurrentScreenshot = screenshotItems.First();
            await LoadImageAsync(CurrentScreenshot.FilePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drop images");
        }
    }


    private void Button_CloseEditGrid_Click(object sender, RoutedEventArgs e)
    {
        Grid_EditImage.Visibility = Visibility.Collapsed;
        ResetZoomFactor();
    }


    private async void Button_ExportImage_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (_sourceBitmap is null)
            {
                return;
            }
            var bitmap = _sourceBitmap;
            float maxCLL = MaxCLL, outputNits = SDRLuminance;
            ColorPrimaries colorPrimaries = ImageColorPrimaries;
            string name = Path.GetFileNameWithoutExtension(CurrentFileName);
            ICanvasImage output = GetDrawOutput(out int displayMode);
            bool imageHdr = bitmap.Format is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float;
            bool displayHdr = displayMode is 2;

            string? path;
            if (displayHdr)
            {
                // hdr image & hdr display
                path = await FileDialogHelper.OpenSaveFileDialogAsync(Content.XamlRoot, name, ("AVIF", ".avif"), ("JPEG XL", ".jxl"));
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }
            }
            else if (!imageHdr)
            {
                // sdr image & sdr display
                path = await FileDialogHelper.OpenSaveFileDialogAsync(Content.XamlRoot, name, ("PNG", ".png"), ("AVIF", ".avif"), ("JPEG XL", ".jxl"));
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }
            }
            else
            {
                // hdr image & sdr display
                path = await FileDialogHelper.OpenSaveFileDialogAsync(Content.XamlRoot, name, ("JPEG", ".jpg"));
                if (string.IsNullOrWhiteSpace(path))
                {
                    return;
                }
            }

            using var ms = new MemoryStream();
            string extension = Path.GetExtension(path).ToLowerInvariant();
            Task task = Path.GetExtension(path).ToLowerInvariant() switch
            {
                ".png" => ImageSaver.SaveAsPngAsync(bitmap, ms, colorPrimaries),
                ".avif" => ImageSaver.SaveAsAvifAsync(bitmap, ms, colorPrimaries, 90),
                ".jxl" => ImageSaver.SaveAsJxlAsync(bitmap, ms, colorPrimaries, 1f),
                ".jpg" => ImageSaver.SaveAsUhdrAsync(bitmap, ms, maxCLL, outputNits),
                _ => throw new ArgumentOutOfRangeException($"File extension '{extension}' is not supported."),
            };
            await task;

            ms.Position = 0;
            using var fs = File.Create(path);
            await ms.CopyToAsync(fs);
            var file = await StorageFile.GetFileFromPathAsync(path);
            var folder = await file.GetParentAsync();
            var folderOptions = new FolderLauncherOptions();
            folderOptions.ItemsToSelect.Add(file);
            await Launcher.LaunchFolderAsync(folder, folderOptions);
        }
        catch (Exception ex)
        {
            ShowInfo(InfoBarSeverity.Error, Lang.ImageViewWindow2_FailedToSaveImage, ex.Message, 0);
            _logger.LogError(ex, "Failed to export image");
        }
    }

    #endregion



    #region Shortcut


    private void RootGrid_KeyDown(object sender, Microsoft.UI.Xaml.Input.KeyRoutedEventArgs e)
    {
        if (e.Key is VirtualKey.Escape)
        {
            Close();
        }
        else if (e.Key is VirtualKey.Left or VirtualKey.Up)
        {
            LoadPreviewImage();
        }
        else if (e.Key is VirtualKey.Right or VirtualKey.Down)
        {
            LoadNextImage();
        }
        else if (e.Key is VirtualKey.F11)
        {
            FullScreen();
        }
    }


    #endregion



    #region InfoBar


    private CancellationTokenSource _infoBarCts;


    private async void ShowInfo(InfoBarSeverity severity, string title, string content, int hideDelay = 0)
    {
        try
        {
            _infoBarCts?.Cancel();
            _infoBarCts = new();
            CancellationToken token = _infoBarCts.Token;
            InfoBar_Tips.Severity = severity;
            InfoBar_Tips.Title = title;
            InfoBar_Tips.Content = content;
            InfoBar_Tips.IsOpen = true;
            if (hideDelay <= 0)
            {
                return;
            }
            await Task.Delay(hideDelay, CancellationToken.None);
            if (token.IsCancellationRequested)
            {
                return;
            }
            InfoBar_Tips.IsOpen = false;
        }
        catch { }
    }



    #endregion



    #region Others


    public void AdaptTitleBarButtonColorToActuallTheme()
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppWindow.TitleBar.ExtendsContentIntoTitleBar == true)
        {
            var titleBar = AppWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            if (Content is FrameworkElement element)
            {
                switch (element.ActualTheme)
                {
                    case ElementTheme.Default:
                        break;
                    case ElementTheme.Light:
                        titleBar.ButtonForegroundColor = Colors.Black;
                        titleBar.ButtonHoverForegroundColor = Colors.Black;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0x00, 0x00, 0x00);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    case ElementTheme.Dark:
                        titleBar.ButtonForegroundColor = Colors.White;
                        titleBar.ButtonHoverForegroundColor = Colors.White;
                        titleBar.ButtonHoverBackgroundColor = Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
                        titleBar.ButtonInactiveForegroundColor = Color.FromArgb(0xFF, 0x99, 0x99, 0x99);
                        break;
                    default:
                        break;
                }
            }
        }
    }


    public void SetIcon(string? iconPath = null)
    {
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            nint hInstance = Kernel32.GetModuleHandle(null).DangerousGetHandle();
            nint hIcon = User32.LoadIcon(hInstance, "#32512").DangerousGetHandle();
            AppWindow.SetIcon(Win32Interop.GetIconIdFromIcon(hIcon));
        }
        else
        {
            AppWindow.SetIcon(iconPath);
        }
    }


    public static Visibility ObjectToVisibility(object obj)
    {
        return obj is null ? Visibility.Collapsed : Visibility.Visible;
    }


    public static Visibility ObjectToVisibilityReversed(bool? value)
    {
        return value is true ? Visibility.Collapsed : Visibility.Visible;
    }


    public static bool ObjectToBool(object obj)
    {
        return obj is not null;
    }


    public static string AddOne(int value)
    {
        return (value + 1).ToString();
    }


    public void CenterInScreen(DisplayArea displayArea, int width, int height)
    {
        nint monitor = Win32Interop.GetMonitorFromDisplayId(displayArea.DisplayId);
        GetDpiForMonitor(monitor, 0, out uint dpiX, out uint dpiY);
        double scale = dpiX / 96.0;
        int w = (int)(width * scale);
        int h = (int)(height * scale);
        int x = displayArea.WorkArea.X + (displayArea.WorkArea.Width - w) / 2;
        int y = displayArea.WorkArea.Y + (displayArea.WorkArea.Height - h) / 2;
        AppWindow.MoveAndResize(new RectInt32(x, y, w, h));
    }


    [LibraryImport("Shcore.dll")]
    private static partial int GetDpiForMonitor(nint hmonitor, int dpiType, out uint dpiX, out uint dpiY);


    #endregion



    private static Matrix5x4 ToMatrix5x4(Matrix4x4 matrix4x4)
    {
        return new Matrix5x4(matrix4x4.M11, matrix4x4.M12, matrix4x4.M13, matrix4x4.M14,
                             matrix4x4.M21, matrix4x4.M22, matrix4x4.M23, matrix4x4.M24,
                             matrix4x4.M31, matrix4x4.M32, matrix4x4.M33, matrix4x4.M34,
                             matrix4x4.M41, matrix4x4.M42, matrix4x4.M43, matrix4x4.M44,
                             0, 0, 0, 0);
    }



}
