using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using Windows.Graphics.Imaging;

namespace Starward.Features.Screenshot;

internal static class ScreenshotHelper
{

    public static bool IsSupportedExtension(string file)
    {
        return Path.GetExtension(file) is ".jpg" or ".png" or ".jxr" or ".webp" or ".heic" or ".avif" or ".jxl";
    }



    public static List<string> WatcherFilters { get; } = new List<string> { "*.jpg", "*.png", "*.jxr", "*.webp", "*.heic", "*.avif", "*.jxl" };



    public static async Task WaitForFileReleaseAsync(string filePath, CancellationToken cancellation = default)
    {
        int count = 0;
        while (count < 30)
        {
            using var handle = Kernel32.CreateFile2(filePath, Kernel32.FileAccess.GENERIC_READ, 0, Kernel32.CreationOption.OPEN_EXISTING);
            if (handle.IsNull || handle.IsInvalid)
            {
                await Task.Delay(100, cancellation);
                count++;
                continue;
            }
            break;
        }
    }


    private static string avifdec = Path.Combine(AppContext.BaseDirectory, "avifdec.exe");

    private static string djxl = Path.Combine(AppContext.BaseDirectory, "djxl.exe");


    public static async Task<string> ConvertToJpgAsync(string filePath)
    {
        if (Path.GetExtension(filePath).Equals(".jpg", StringComparison.OrdinalIgnoreCase))
        {
            return filePath;
        }
        string cacheFolder = Path.Combine(AppConfig.CacheFolder, "cache");
        Directory.CreateDirectory(cacheFolder);
        string jpgFilePath = Path.Combine(cacheFolder, Path.GetFileNameWithoutExtension(filePath) + ".jpg");
        string extension = Path.GetExtension(filePath).ToLowerInvariant();

        if (extension is ".avif" && File.Exists(avifdec))
        {
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = avifdec,
                Arguments = $"""
                      "{filePath}" "{jpgFilePath}" -q 90
                      """,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }) ?? throw new NullReferenceException("avifdec process is null");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0 || !File.Exists(jpgFilePath))
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"avifdec failed: {error}");
            }
        }
        else if (extension is ".jxl" && File.Exists(djxl))
        {
            Process? process = Process.Start(new ProcessStartInfo
            {
                FileName = djxl,
                Arguments = $"""
                      "{filePath}" "{jpgFilePath}" -q 90
                      """,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            }) ?? throw new NullReferenceException("djxl process is null");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0 || !File.Exists(jpgFilePath))
            {
                string error = await process.StandardError.ReadToEndAsync();
                throw new Exception($"djxl failed: {error}");
            }
        }
        else
        {
            using var fs = File.OpenRead(filePath);
            var decoder = await BitmapDecoder.CreateAsync(fs.AsRandomAccessStream());
            var ms = new MemoryStream();
            var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, ms.AsRandomAccessStream());
            encoder.SetSoftwareBitmap(await decoder.GetSoftwareBitmapAsync());
            await encoder.FlushAsync();
            await File.WriteAllBytesAsync(jpgFilePath, ms.ToArray());
        }
        return jpgFilePath;
    }



}
