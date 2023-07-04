using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Helpers;
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Windows.Storage.Streams;
using Windows.UI;
using WinRT;

namespace Starward.Helpers;

internal static class AccentColorHelper
{



    public unsafe static (Color? Back, Color? Fore) GetAccentColor(byte[] bgra, int width, int height)
    {
        if (bgra.Length % 4 == 0)
        {
            fixed (byte* ptr = bgra)
            {
                return GetAccentColorInternal(ptr, width, height);
            }
        }
        return (null, null);
    }




    public unsafe static (Color? Back, Color? Fore) GetAccentColor(IBuffer buffer, int width, int height)
    {
        int length = (int)buffer.Length;
        if (length > 0 && length % 4 == 0)
        {
            if (buffer.As<IBufferByteAccess>().Buffer(out nint ptr) == 0)
            {
                return GetAccentColorInternal((void*)ptr, width, height);
            }
        }
        return (null, null);
    }




    private unsafe static (Color? Back, Color? Fore) GetAccentColorInternal(void* bgra, int width, int height)
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
                    int hue = Bgra32ToHue(pixel);
                    // ignore white black gray
                    if (hue >= 0)
                    {
                        hueCircle[hue]++;
                    }
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

            int maxHueStart = 0;
            int maxHueCount = 0;
            for (int i = ((int)hsv.H) + 30; i < ((int)hsv.H) + 300; i++)
            {
                int count = 0;
                for (int j = i; j < i + 30; j++)
                {
                    int h = j % 360;
                    count += hueCircle[h];
                }
                if (count > maxHueCount)
                {
                    maxHueStart = i;
                    maxHueCount = count;
                }
            }

            long sum = 0;
            for (int i = maxHueStart; i < maxHueStart + 30; i++)
            {
                int h = i % 360;
                sum += h * hueCircle[h];
            }

            return (ColorHelper.FromHsv(hsv.H, 0.6, hsv.V), ColorHelper.FromHsv((double)sum / maxHueCount, 0.9, 0.9));
        }
        catch { }
        return (null, null);
    }




    [ComImport]
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IBufferByteAccess
    {
        int Buffer([Out] out nint value);
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
