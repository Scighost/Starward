using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Starward.Codec.AVIF;
using Starward.Codec.JpegXL;
using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.CodeStream;
using Starward.Codec.JpegXL.Encode;
using Starward.Features.Codec;
using Starward.Frameworks;
using Starward.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Graphics.DirectX;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.System;


namespace Starward.Features.Screenshot;

[INotifyPropertyChanged]
public sealed partial class ImageBatchConvertWindow : WindowEx
{

    private readonly ILogger<ImageBatchConvertWindow> _logger = AppConfig.GetLogger<ImageBatchConvertWindow>();


    private readonly Brush _deltaAddBrush = (App.Current.Resources["SystemFillColorCriticalBrush"] as Brush)!;

    private readonly Brush _deltaDecreseBrush = (App.Current.Resources["SystemFillColorSuccessBrush"] as Brush)!;

    private readonly Brush _deltaZeroBrush = (App.Current.Resources["TextFillColorSecondaryBrush"] as Brush)!;


    public ImageBatchConvertWindow()
    {
        InitializeComponent();
        InitializeWindow();
    }




    private void InitializeWindow()
    {
        AppWindow.TitleBar.ExtendsContentIntoTitleBar = true;
        SystemBackdrop = new MicaBackdrop();
        AdaptTitleBarButtonColorToActuallTheme();
        SetIcon();
    }



    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {

    }



    private void RootGrid_Unloaded(object sender, RoutedEventArgs e)
    {
        try
        {
            RootGrid.Loaded -= RootGrid_Loaded;
            RootGrid.Unloaded -= RootGrid_Unloaded;
            Button_Import.Click -= Button_Import_Click;
            Button_OutputFolder.Click -= Button_OutputFolder_Click;
            Button_StartConvert.Click -= Button_StartConvert_Click;
            Button_Stop.Click -= Button_Stop_Click;
            Button_Clear.Click -= Button_Clear_Click;
            ListView_ImageConvertItems.DragOver -= ListView_ImageConvertItems_DragOver;
            ListView_ImageConvertItems.Drop -= ListView_ImageConvertItems_Drop;
            _cancellationTokenSource?.Cancel();
            _itemsDict.Clear();
            _itemsDict = null!;
            ImageConvertItems.Clear();
            ImageConvertItems = null!;
        }
        catch { }
    }



    public void Activate(List<ScreenshotItem> items)
    {
        base.Activate();
        try
        {
            foreach (var item in items)
            {
                if (ScreenshotHelper.IsSupportedExtension(Path.GetExtension(item.FilePath)))
                {
                    if (!_itemsDict.ContainsKey(item.FilePath))
                    {
                        var convertItem = new ImageConvertItem(item.FilePath);
                        ImageConvertItems.Add(convertItem);
                        _itemsDict[item.FilePath] = convertItem;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import image files failed");
        }
    }



    private CancellationTokenSource? _cancellationTokenSource;

    private Dictionary<string, ImageConvertItem> _itemsDict = new();

    public ObservableCollection<ImageConvertItem> ImageConvertItems { get; set; } = new();

    public string OutputFolder { get; set => SetProperty(ref field, value); } = Lang.ImageBatchConvertWindow_OriginalFileFolder;

    public int TotalCount { get; set => SetProperty(ref field, value); }

    public int SuccessCount { get; set => SetProperty(ref field, value); }

    public int ErrorCount { get; set => SetProperty(ref field, value); }

    public bool DisplayInfo { get; set => SetProperty(ref field, value); }

    public bool IsConverting { get; set => SetProperty(ref field, value); }

    public long TotalSourceFileSize { get; set => SetProperty(ref field, value); }

    public long TotalOutputFileSize { get; set => SetProperty(ref field, value); }



    private async void Button_Import_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var files = await FileDialogHelper.PickMultipleFilesAsync(Content.XamlRoot);
            foreach (var item in files)
            {
                if (ScreenshotHelper.IsSupportedExtension(Path.GetExtension(item)))
                {
                    if (!_itemsDict.ContainsKey(item))
                    {
                        var convertItem = new ImageConvertItem(item);
                        ImageConvertItems.Add(convertItem);
                        _itemsDict[item] = convertItem;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import image files failed");
        }
    }


    private async void Button_OutputFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = await FileDialogHelper.PickFolderAsync(Content.XamlRoot);
            if (Directory.Exists(folder))
            {
                OutputFolder = folder;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Pick output folder failed");
        }
    }



    private async void Button_StartConvert_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            DisableControls();
            if (_cancellationTokenSource is null)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                await ConvertInternalAsync(_cancellationTokenSource.Token);
            }
            else
            {
                _cancellationTokenSource.Cancel();
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Convert images canceled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Convert images failed");
        }
        finally
        {
            _cancellationTokenSource = null;
            RestoreControls();
        }
    }


    private void Button_Stop_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource = null;
            RestoreControls();
            DisplayInfo = false;
            foreach (var item in ImageConvertItems)
            {
                item.Converting = false;
                item.ConvertError = false;
                item.ConvertSuccess = false;
                item.ErrorMessage = null!;
                item.OutputFileName = null!;
                item.OutputFilePath = null!;
                item.OutputFileSize = 0;
                item.OutputFileSizeText = null!;
                item.FileDeltaPercent = null!;
                item.FileDeltaTextBrush = null!;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stop converting images failed");
        }
    }


    private void Button_Clear_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _itemsDict.Clear();
            ImageConvertItems.Clear();
            TotalCount = 0;
            SuccessCount = 0;
            ErrorCount = 0;
            DisplayInfo = false;
            IsConverting = false;
            TotalSourceFileSize = 0;
            TotalOutputFileSize = 0;
        }
        catch { }
    }


    private void ListView_ImageConvertItems_DragOver(object sender, DragEventArgs e)
    {
        try
        {
            e.AcceptedOperation = DataPackageOperation.Copy;
        }
        catch { }
    }


    private async void ListView_ImageConvertItems_Drop(object sender, DragEventArgs e)
    {
        try
        {
            var items = await e.DataView.GetStorageItemsAsync();

            var list = new List<string>();
            foreach (IStorageItem? item in items)
            {
                if (item is StorageFile { Path: not "" } file && ScreenshotHelper.IsSupportedExtension(file.FileType))
                {
                    if (!_itemsDict.ContainsKey(file.Path))
                    {
                        var convertItem = new ImageConvertItem(file.Path);
                        ImageConvertItems.Add(convertItem);
                        _itemsDict[file.Path] = convertItem;
                    }
                }
                else if (item is StorageFolder folder)
                {
                    var files = await folder.GetFilesAsync();
                    foreach (var file1 in files)
                    {
                        if (ScreenshotHelper.IsSupportedExtension(file1.FileType) && !_itemsDict.ContainsKey(file1.Path))
                        {
                            var convertItem = new ImageConvertItem(file1.Path);
                            ImageConvertItems.Add(convertItem);
                            _itemsDict[file1.Path] = convertItem;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Drop images");
        }
    }


    private async void HyperlinkButton_SourceFileName_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is HyperlinkButton { DataContext: ImageConvertItem item })
            {
                string path = item.SourceFilePath;
                var file = await StorageFile.GetFileFromPathAsync(path);
                var folder = await file.GetParentAsync();
                var options = new FolderLauncherOptions { ItemsToSelect = { file } };
                await Launcher.LaunchFolderAsync(folder, options);
            }
        }
        catch { }
    }


    private async void HyperlinkButton_OutputFileName_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (sender is HyperlinkButton { DataContext: ImageConvertItem item })
            {
                string path = item.OutputFilePath;
                var file = await StorageFile.GetFileFromPathAsync(path);
                var folder = await file.GetParentAsync();
                var options = new FolderLauncherOptions { ItemsToSelect = { file } };
                await Launcher.LaunchFolderAsync(folder, options);
            }
        }
        catch { }
    }



    private void DisableControls()
    {
        Button_Import.IsEnabled = false;
        Button_OutputFolder.IsEnabled = false;
        Button_Clear.IsEnabled = false;
        ComboBox_OutputFormat.IsEnabled = false;
        ComboBox_OutputFileExists.IsEnabled = false;
        Slider_Quality.IsEnabled = false;
        Button_StartConvert.Content = Lang.DownloadGamePage_Pause;
    }



    private void RestoreControls()
    {
        Button_Import.IsEnabled = true;
        Button_OutputFolder.IsEnabled = true;
        Button_Clear.IsEnabled = true;
        ComboBox_OutputFormat.IsEnabled = true;
        ComboBox_OutputFileExists.IsEnabled = true;
        Slider_Quality.IsEnabled = true;
        Button_StartConvert.Content = Lang.ImageBatchConvertWindow_StartConvert;
    }




    private string _format;

    private string? _outputFolder;

    private int _overwriteMode;

    private int _quality;

    private string _avifenc;

    private string _avifdec;

    private string _cjxl;

    private string _djxl;


    private async Task ConvertInternalAsync(CancellationToken cancellationToken = default)
    {
        TotalCount = 0;
        SuccessCount = 0;
        ErrorCount = 0;
        TotalSourceFileSize = 0;
        TotalOutputFileSize = 0;
        if (ImageConvertItems.Count == 0)
        {
            DisplayInfo = false;
            return;
        }
        _format = ComboBox_OutputFormat.SelectedIndex switch
        {
            0 => ".jpg",
            1 => ".png",
            2 => ".avif",
            3 => ".jxl",
            _ => "",
        };
        if (string.IsNullOrWhiteSpace(_format))
        {
            // 使下拉菜单可选
            ComboBox_OutputFormat.IsDropDownOpen = true;
            ComboBox_OutputFormat.IsDropDownOpen = false;
            ComboBox_OutputFormat.IsDropDownOpen = true;
            return;
        }
        _overwriteMode = ComboBox_OutputFileExists.SelectedIndex switch
        {
            -1 => 0,
            _ => ComboBox_OutputFileExists.SelectedIndex,
        };


        TotalCount = ImageConvertItems.Count;
        DisplayInfo = true;

        _outputFolder = null;
        if (Path.IsPathFullyQualified(OutputFolder) && Directory.Exists(OutputFolder))
        {
            _outputFolder = OutputFolder;
        }
        _quality = (int)Math.Clamp(Slider_Quality.Value, 0, 100);
        _avifenc = Path.Combine(AppContext.BaseDirectory, "avifenc.exe");
        _avifdec = Path.Combine(AppContext.BaseDirectory, "avifdec.exe");
        _cjxl = Path.Combine(AppContext.BaseDirectory, "cjxl.exe");
        _djxl = Path.Combine(AppContext.BaseDirectory, "djxl.exe");

        foreach (var item in ImageConvertItems)
        {
            await ConvertImageAsync(item, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
        }
    }


    private async Task ConvertImageAsync(ImageConvertItem item, CancellationToken cancellationToken = default)
    {
        if (item.ConvertSuccess)
        {
            SuccessCount++;
            return;
        }
        item.ConvertError = false;
        try
        {
            item.Converting = true;
            if (item.SourceExtension == _format)
            {
                string outputPath = GetOutputPath(item, true);
                if (!File.Exists(outputPath))
                {
                    File.Copy(item.SourceFilePath, outputPath);
                }
                item.OutputFilePath = outputPath;
            }
            else
            {
                Task task = (item.SourceExtension, _format) switch
                {
                    (".jpg" or ".png", ".avif" or ".jxl") => ConvertJpegPngToAvifJxlAsync(item, cancellationToken),
                    (".avif" or ".jxl", ".jpg" or ".png") => ConvertAvifJxlToJpegPngAsync(item, cancellationToken),
                    (".jpg" or ".png" or ".jxr" or ".webp" or ".heic", ".jpg" or ".png") => ConvertJpegPngJxrWebpHeicToJpgPngAsync(item, cancellationToken),
                    (".jxr" or ".webp" or ".heic" or ".avif" or ".jxl", ".avif" or ".jxl") => ConvertJxrWebpHeicAvifJxlToAvifJxlAsync(item, cancellationToken),
                    _ => throw new NotSupportedException($"Unsupported conversion from '{item.SourceExtension}' to '{_format}'."),
                };
                await task;
            }
            item.OutputFileName = Path.GetFileName(item.OutputFilePath);
            item.OutputFileSize = new FileInfo(item.OutputFilePath).Length;
            item.OutputFileSizeText = $"{item.OutputFileSize / 1024:N0} KB";
            long delta = item.OutputFileSize - item.SourceFileSize;
            double percent = item.SourceFileSize == 0 ? 0 : (double)delta / item.SourceFileSize;
            item.FileDeltaPercent = percent switch
            {
                > 0 => $"+{percent:P0}",
                < 0 => $"{percent:P0}",
                _ => "0%",
            };
            item.FileDeltaTextBrush = delta switch
            {
                > 0 => _deltaAddBrush,
                < 0 => _deltaDecreseBrush,
                _ => _deltaZeroBrush,
            };
            item.ConvertSuccess = true;
            SuccessCount++;
            TotalSourceFileSize += item.SourceFileSize;
            TotalOutputFileSize += item.OutputFileSize;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Convert image failed: {file}", item.SourceFilePath);
            item.ConvertError = true;
            if (ex is UnauthorizedAccessException)
            {
                item.ErrorMessage = Lang.ImageBatchConvertWindow_NoReadOrWritePermission;
            }
            else
            {
                item.ErrorMessage = Lang.ImageBatchConvertWindow_ConvertFailed;
            }
            ErrorCount++;
        }
        finally
        {
            item.Converting = false;
        }
    }


    private async Task ConvertJpegPngToAvifJxlAsync(ImageConvertItem item, CancellationToken cancellationToken = default)
    {
        string outputPath = GetOutputPath(item);
        if (File.Exists(outputPath) && _overwriteMode is 0)
        {
            item.OutputFilePath = outputPath;
            return;
        }
        if (_format == ".avif")
        {
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = _avifenc,
                Arguments = $"""
                      "{item.SourceFilePath}" "{outputPath}" -q {_quality} --cicp 1/13/1
                      """,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }) ?? throw new NullReferenceException("avifenc process is null");
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"avifenc failed: {error}");
            }
            item.OutputFilePath = outputPath;
        }
        if (_format == ".jxl")
        {
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = _cjxl,
                Arguments = $"""
                      "{item.SourceFilePath}" "{outputPath}" -q {_quality} --lossless_jpeg={(_quality == 100 ? 1 : 0)}
                      """,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }) ?? throw new NullReferenceException("cjxl process is null");
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"cjxl failed: {error}");
            }
            item.OutputFilePath = outputPath;
        }
    }


    private async Task ConvertAvifJxlToJpegPngAsync(ImageConvertItem item, CancellationToken cancellationToken = default)
    {
        string outputPath = GetOutputPath(item);
        if (File.Exists(outputPath) && _overwriteMode is 0)
        {
            item.OutputFilePath = outputPath;
            return;
        }
        if (item.SourceExtension == ".avif")
        {
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = _avifdec,
                Arguments = $"""
                      "{item.SourceFilePath}" "{outputPath}" -q {_quality}
                      """,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }) ?? throw new NullReferenceException("avifdec process is null");
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"avifdec failed: {error}");
            }
            item.OutputFilePath = outputPath;
        }
        if (item.SourceExtension == ".jxl")
        {
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = _djxl,
                Arguments = $"""
                      "{item.SourceFilePath}" "{outputPath}" -q {_quality} -j
                      """,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }) ?? throw new NullReferenceException("djxl process is null");
            await process.WaitForExitAsync(cancellationToken);
            if (process.ExitCode != 0 || !File.Exists(outputPath))
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"djxl failed: {error}");
            }
            item.OutputFilePath = outputPath;
        }
    }


    private async Task ConvertJpegPngJxrWebpHeicToJpgPngAsync(ImageConvertItem item, CancellationToken cancellationToken = default)
    {
        string outputPath = GetOutputPath(item);
        if (File.Exists(outputPath) && _overwriteMode is 0)
        {
            item.OutputFilePath = outputPath;
            return;
        }
        using var fs_read = File.OpenRead(item.SourceFilePath);
        var decoder = await BitmapDecoder.CreateAsync(fs_read.AsRandomAccessStream()).AsTask(cancellationToken);
        var encoderId = _format switch
        {
            ".jpg" => BitmapEncoder.JpegEncoderId,
            ".png" => BitmapEncoder.PngEncoderId,
            _ => throw new NotSupportedException("Unsupported format"),
        };
        using var ms = new MemoryStream();
        List<KeyValuePair<string, BitmapTypedValue>> options = [KeyValuePair.Create("ImageQuality", new BitmapTypedValue(_quality / 100f, PropertyType.Single)), KeyValuePair.Create("JpegYCrCbSubsampling", new BitmapTypedValue(3, PropertyType.UInt8))];
        var encoder = await BitmapEncoder.CreateAsync(encoderId, ms.AsRandomAccessStream(), options).AsTask(cancellationToken);
        encoder.SetSoftwareBitmap(await decoder.GetSoftwareBitmapAsync().AsTask(cancellationToken));
        await encoder.FlushAsync().AsTask(cancellationToken);
        using var fs_write = File.Create(outputPath);
        ms.Position = 0;
        await ms.CopyToAsync(fs_write, CancellationToken.None);
        item.OutputFilePath = outputPath;
    }


    private async Task ConvertJxrWebpHeicAvifJxlToAvifJxlAsync(ImageConvertItem item, CancellationToken cancellationToken = default)
    {
        string outputPath = GetOutputPath(item);
        if (File.Exists(outputPath) && _overwriteMode is 0)
        {
            item.OutputFilePath = outputPath;
            return;
        }
        using var imageInfo = await ImageLoader.LoadImageAsync(item.SourceFilePath, cancellationToken);
        if (_format == ".avif")
        {
            using var ms = new MemoryStream();
            await SaveAsAvifAsync(imageInfo.CanvasBitmap, ms, item.SourceFileTime, _quality, cancellationToken);
            using var fs = File.Create(outputPath);
            ms.Position = 0;
            await ms.CopyToAsync(fs, CancellationToken.None);
            item.OutputFilePath = outputPath;
        }
        if (_format == ".jxl")
        {
            using var ms = new MemoryStream();
            await SaveAsJxlAsync(imageInfo.CanvasBitmap, ms, item.SourceFileTime, _quality, cancellationToken);
            using var fs = File.Create(outputPath);
            ms.Position = 0;
            await ms.CopyToAsync(fs, CancellationToken.None);
            item.OutputFilePath = outputPath;
        }
    }


    public static async Task SaveAsAvifAsync(CanvasBitmap bitmap, Stream stream, DateTimeOffset frameTime, int quality, CancellationToken cancellationToken = default)
    {
        uint width = bitmap.SizeInPixels.Width;
        uint height = bitmap.SizeInPixels.Height;
        if (bitmap.Format is DirectXPixelFormat.R8G8B8A8UIntNormalized or DirectXPixelFormat.B8G8R8A8UIntNormalized)
        {
            await Task.Run(() =>
            {
                avifRGBFormat format = bitmap.Format switch
                {
                    DirectXPixelFormat.R8G8B8A8UIntNormalized => avifRGBFormat.RGBA,
                    DirectXPixelFormat.B8G8R8A8UIntNormalized => avifRGBFormat.BGRA,
                    _ => throw new NotSupportedException($"{bitmap.Format} is not supported for AVIF encoding."),
                };
                using var encoder = new avifEncoderLite();
                encoder.Quality = quality;
                encoder.QualityAlpha = quality;
                using var rgb = new avifRGBImageWrapper(width, height, 8, format);
                rgb.SetPixelBytes(bitmap.GetPixelBytes());
                using var image = new avifImageWrapper(width, height, 8, avifPixelFormat.YUV444);
                image.ColorPrimaries = avifColorPrimaries.BT709;
                image.TransferCharacteristics = avifTransferCharacteristics.SRGB;
                image.MatrixCoefficients = avifMatrixCoefficients.BT709;
                image.SetXMPMetadata(ScreenCaptureService.BuildXMPMetadata(frameTime));
                image.FromRGBImage(rgb);
                encoder.AddImage(image, 1, avifAddImageFlag.Single);
                stream.Write(encoder.Encode());
            }, cancellationToken).ConfigureAwait(false);
        }
        else if (bitmap.Format is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float)
        {
            await Task.Run(() =>
            {
                using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16UIntNormalized, CanvasAlphaMode.Premultiplied);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var effect = new ScRGBToHDR10Effect
                    {
                        Source = bitmap,
                        BufferPrecision = CanvasBufferPrecision.Precision16Float,
                    };
                    ds.DrawImage(effect);
                }
                using var encoder = new avifEncoderLite();
                encoder.Quality = quality;
                encoder.QualityAlpha = quality;
                using var rgb = new avifRGBImageWrapper(width, height, 16, avifRGBFormat.RGBA);
                rgb.SetPixelBytes(renderTarget.GetPixelBytes());
                using var image = new avifImageWrapper(width, height, 12, avifPixelFormat.YUV444);
                image.ColorPrimaries = avifColorPrimaries.BT2020;
                image.TransferCharacteristics = avifTransferCharacteristics.SMPTE2084;
                image.MatrixCoefficients = avifMatrixCoefficients.BT2020_NCL;
                image.SetXMPMetadata(ScreenCaptureService.BuildXMPMetadata(frameTime));
                image.FromRGBImage(rgb);
                encoder.AddImage(image, 1, avifAddImageFlag.Single);
                stream.Write(encoder.Encode());
            }, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"{bitmap.Format} is not supported for AVIF encoding.");
        }
    }


    public static async Task SaveAsJxlAsync(CanvasBitmap bitmap, Stream stream, DateTimeOffset frameTime, int quality, CancellationToken cancellationToken = default)
    {
        uint width = bitmap.SizeInPixels.Width;
        uint height = bitmap.SizeInPixels.Height;
        bool lossless = quality == 100;

        if (bitmap.Format is DirectXPixelFormat.R8G8B8A8UIntNormalized or DirectXPixelFormat.B8G8R8A8UIntNormalized)
        {
            byte[] pixelBytes;
            if (bitmap.Format is DirectXPixelFormat.B8G8R8A8UIntNormalized)
            {
                using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R8G8B8A8UIntNormalized, CanvasAlphaMode.Premultiplied);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    ds.DrawImage(bitmap);
                }
                pixelBytes = renderTarget.GetPixelBytes();
            }
            else
            {
                pixelBytes = bitmap.GetPixelBytes();
            }
            await Task.Run(() =>
            {
                using var encoder = new JxlEncoder();
                encoder.SetBasicInfo(new JxlBasicInfo(width, height, JxlPixelFormat.R8G8B8A8UInt, true) { UsesOriginalProfile = lossless });
                encoder.SetColorEncoding(JxlColorEncoding.SRGB);
                encoder.AddBox(JxlBoxType.XMP, ScreenCaptureService.BuildXMPMetadata(frameTime), false);
                var frameSettings = encoder.CreateFrameSettings();
                frameSettings.Quality = quality;
                frameSettings.Lossless = lossless;
                frameSettings.AddImageFrame(JxlPixelFormat.R8G8B8A8UInt, pixelBytes);
                encoder.Encode(stream);
            }, cancellationToken).ConfigureAwait(false);
        }
        else if (bitmap.Format is DirectXPixelFormat.R16G16B16A16Float or DirectXPixelFormat.R32G32B32A32Float)
        {
            await Task.Run(() =>
            {
                using var renderTarget = new CanvasRenderTarget(CanvasDevice.GetSharedDevice(), width, height, 96, DirectXPixelFormat.R16G16B16A16UIntNormalized, CanvasAlphaMode.Premultiplied);
                using (var ds = renderTarget.CreateDrawingSession())
                {
                    var effect = new ScRGBToHDR10Effect
                    {
                        Source = bitmap,
                        BufferPrecision = CanvasBufferPrecision.Precision16Float,
                    };
                    ds.DrawImage(effect);
                }
                using var encoder = new JxlEncoder();
                encoder.SetBasicInfo(new JxlBasicInfo(width, height, JxlPixelFormat.R16G16B16A16UInt, true) { UsesOriginalProfile = lossless });
                encoder.SetColorEncoding(JxlColorEncoding.HDR10);
                encoder.AddBox(JxlBoxType.XMP, ScreenCaptureService.BuildXMPMetadata(frameTime), false);
                var frameSettings = encoder.CreateFrameSettings();
                frameSettings.Quality = quality;
                frameSettings.Lossless = lossless;
                frameSettings.AddImageFrame(JxlPixelFormat.R16G16B16A16UInt, renderTarget.GetPixelBytes());
                encoder.Encode(stream);
            }, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            throw new NotSupportedException($"{bitmap.Format} is not supported for JPEG XL encoding.");
        }
    }



    private string GetOutputPath(ImageConvertItem item, bool doNotRename = false)
    {
        string name = Path.GetFileNameWithoutExtension(item.SourceFilePath);
        string folder = _outputFolder ?? Path.GetDirectoryName(item.SourceFilePath)!;
        string outputPath = Path.Combine(folder, name + _format);
        if (doNotRename || _overwriteMode is 0 or 1)
        {
            return outputPath;
        }
        int i = 0;
        while (File.Exists(outputPath))
        {
            outputPath = Path.Combine(folder, $"{name}_{++i}{_format}");
        }
        return outputPath;
    }


    public static string SizeToString(long size)
    {
        const double MB = 1 << 20;
        return $"{size / MB:F2} MB";
    }

}



public class ImageConvertItem : ObservableObject
{

    public string SourceFilePath { get; set; }

    public string SourceFileName { get; set; }

    public string SourceExtension { get; set; }

    public long SourceFileSize { get; set; }

    public string SourceFileSizeText { get; set; }

    public DateTimeOffset SourceFileTime { get; set; }


    public bool Converting { get => field; set => SetProperty(ref field, value); }

    public bool ConvertSuccess { get => field; set => SetProperty(ref field, value); }

    public bool ConvertError { get; set => SetProperty(ref field, value); }


    public string OutputFilePath { get; set; }

    public string OutputFileName { get; set => SetProperty(ref field, value); }

    public long OutputFileSize { get; set => SetProperty(ref field, value); }

    public string OutputFileSizeText { get => field; set => SetProperty(ref field, value); }

    public Brush FileDeltaTextBrush { get; set => SetProperty(ref field, value); }

    public string FileDeltaPercent { get; set => SetProperty(ref field, value); }


    public string ErrorMessage { get; set => SetProperty(ref field, value); }


    public ImageConvertItem(string filePath)
    {
        SourceFilePath = filePath;
        SourceFileName = Path.GetFileName(filePath);
        SourceExtension = Path.GetExtension(filePath).ToLowerInvariant();
        FileInfo fileInfo = new FileInfo(filePath);
        SourceFileSize = fileInfo.Length;
        SourceFileSizeText = $"{SourceFileSize / 1024:N0} KB";
        if (ScreenshotItem.TryParseCreationTime(SourceFileName, out DateTime time))
        {
            SourceFileTime = new DateTimeOffset(time);
        }
        else
        {
            SourceFileTime = fileInfo.CreationTime;
        }
    }

}