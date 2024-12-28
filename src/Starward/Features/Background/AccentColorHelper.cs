using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;
using Windows.UI;
using WinRT;

namespace Starward.Features.Background;

internal static class AccentColorHelper
{



    public unsafe static Color? GetAccentColor(byte[] bgra, int width, int height)
    {
        if (bgra.Length % 4 == 0)
        {
            fixed (byte* ptr = bgra)
            {
                return GetAccentColorInternal(ptr, width, height);
            }
        }
        return null;
    }




    public unsafe static Color? GetAccentColor(IBuffer buffer, int width, int height)
    {
        int length = (int)buffer.Length;
        if (length > 0 && length % 4 == 0)
        {
            if (buffer.As<IBufferByteAccess>().Buffer(out nint ptr) == 0)
            {
                return GetAccentColorInternal((void*)ptr, width, height);
            }
        }
        return null;
    }



    public unsafe static Color? GetAccentColor(nint bufferPtr, uint capacity, int width, int height)
    {
        if (capacity > 0 && capacity % 4 == 0)
        {
            return GetAccentColorInternal((void*)bufferPtr, width, height);
        }
        return null;
    }




    private unsafe static Color? GetAccentColorInternal(void* bgra, int width, int height)
    {
        try
        {
            uint* p = (uint*)bgra;
            long b = 0, g = 0, r = 0;
            int[] hueCircle = new int[360];
            for (int y = 0; y < height; y += 2)
            {
                for (int x = 0; x < width; x += 2)
                {
                    Bgra32 pixel = Unsafe.AsRef<Bgra32>(p);
                    b += pixel.B;
                    g += pixel.G;
                    r += pixel.R;
                    p += 2;
                }
                p += width - width % 2;
            }

            int c = (width / 2) * (height / 2);
            Unsafe.SkipInit(out Color color);
            color.B = (byte)(b / c);
            color.G = (byte)(g / c);
            color.R = (byte)(r / c);
            color.A = 255;
            HsvColor hsv = color.ToHsv();

            return CommunityToolkit.WinUI.Helpers.ColorHelper.FromHsv(hsv.H, 0.6, hsv.V);
        }
        catch { }
        return null;
    }




    private static Color ColorMix(Color input, Color blend, double percent)
    {
        return Color.FromArgb(255,
                              (byte)(input.R * percent + blend.R * (1 - percent)),
                              (byte)(input.G * percent + blend.G * (1 - percent)),
                              (byte)(input.B * percent + blend.B * (1 - percent)));
    }



    public static void ChangeAppAccentColor(Color? color)
    {
        if (color is null)
        {
            return;
        }

        Color light1 = ColorMix(color.Value, Colors.White, 0.8);
        Color light2 = ColorMix(color.Value, Colors.White, 0.6);
        Color light3 = ColorMix(color.Value, Colors.White, 0.4);
        Color dark1 = ColorMix(color.Value, Colors.Black, 0.8);
        Color dark2 = ColorMix(color.Value, Colors.Black, 0.6);
        Color dark3 = ColorMix(color.Value, Colors.Black, 0.4);

        Application.Current.Resources["SystemAccentColor"] = color;
        Application.Current.Resources["SystemAccentColorLight1"] = light1;
        Application.Current.Resources["SystemAccentColorLight2"] = light2;
        Application.Current.Resources["SystemAccentColorLight3"] = light3;
        Application.Current.Resources["SystemAccentColorDark1"] = dark1;
        Application.Current.Resources["SystemAccentColorDark2"] = dark2;
        Application.Current.Resources["SystemAccentColorDark3"] = dark3;

        WeakReferenceMessenger.Default.Send(new AccentColorChangedMessage());
    }




    [ComImport]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IBufferByteAccess
    {
        int Buffer([Out] out nint value);
    }



    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer([Out] out nint buffer, [Out] out uint capacity);
    }



    [StructLayout(LayoutKind.Explicit, Size = 4)]
    private readonly struct Bgra32
    {
        [FieldOffset(0)] public readonly byte B;
        [FieldOffset(1)] public readonly byte G;
        [FieldOffset(2)] public readonly byte R;
        [FieldOffset(3)] public readonly byte A;
    }



    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static int Bgra32ToHue(in Bgra32 bgra)
    {
        byte max = Math.Max(Math.Max(bgra.R, bgra.G), bgra.B);
        byte min = Math.Min(Math.Min(bgra.R, bgra.G), bgra.B);
        float chroma = max - min;
        float h;

        if (chroma <= 8)
        {
            // ignore white black gray
            h = -1;
        }
        else if (max == bgra.R)
        {
            h = (((bgra.G - bgra.B) / chroma) + 6) % 6;
        }
        else if (max == bgra.G)
        {
            h = 2 + ((bgra.B - bgra.R) / chroma);
        }
        else
        {
            h = 4 + ((bgra.R - bgra.G) / chroma);
        }
        return (int)(h * 60);
    }


}
