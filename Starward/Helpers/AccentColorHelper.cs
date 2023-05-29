using ComputeSharp;
using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using Windows.UI;

namespace Starward.Helpers;

internal static partial class AccentColorHelper
{


    public unsafe static Color GetAverageColor(byte[] bgra)
    {
        if (Vector256.IsHardwareAccelerated)
        {
            ulong b = 0, g = 0, r = 0, a = 0;
            int remain = bgra.Length % Vector<byte>.Count;
            Vector<ulong> result = new Vector<ulong>();
            for (int i = 0; i < bgra.Length - remain; i += Vector<byte>.Count)
            {
                var vec_byte = new Vector<byte>(bgra, i);
                Vector.Widen(vec_byte, out Vector<ushort> vec_ushort_1, out Vector<ushort> vec_ushort_2);

                Vector.Widen(vec_ushort_1, out Vector<uint> vec_uint_1, out Vector<uint> vec_uint_2);
                Vector.Widen(vec_ushort_2, out Vector<uint> vec_uint_3, out Vector<uint> vec_uint_4);

                Vector.Widen(vec_uint_1, out Vector<ulong> vec_ulong_1, out Vector<ulong> vec_ulong_2);
                Vector.Widen(vec_uint_2, out Vector<ulong> vec_ulong_3, out Vector<ulong> vec_ulong_4);
                Vector.Widen(vec_uint_3, out Vector<ulong> vec_ulong_5, out Vector<ulong> vec_ulong_6);
                Vector.Widen(vec_uint_4, out Vector<ulong> vec_ulong_7, out Vector<ulong> vec_ulong_8);

                var add_1 = Vector.Add(vec_ulong_1, vec_ulong_2);
                var add_2 = Vector.Add(vec_ulong_3, vec_ulong_4);
                var add_3 = Vector.Add(vec_ulong_5, vec_ulong_6);
                var add_4 = Vector.Add(vec_ulong_7, vec_ulong_8);

                var add_5 = Vector.Add(add_1, add_2);
                var add_6 = Vector.Add(add_3, add_4);

                var add = Vector.Add(add_5, add_6);
                result = Vector.Add(result, add);
            }
            for (int i = 0; i < Vector<ulong>.Count; i += 4)
            {
                b += result[i];
                g += result[i + 1];
                r += result[i + 2];
                a += result[i + 3];
            }
            for (int i = bgra.Length - remain; i < bgra.Length - remain; i += 4)
            {
                b += bgra[i];
                g += bgra[i + 1];
                r += bgra[i + 2];
                a += bgra[i + 3];
            }
            Unsafe.SkipInit(out Color color);
            color.B = (byte)(b * 4 / ((ulong)bgra.Length));
            color.G = (byte)(g * 4 / ((ulong)bgra.Length));
            color.R = (byte)(r * 4 / ((ulong)bgra.Length));
            color.A = (byte)(a * 4 / ((ulong)bgra.Length));
            return color;
        }
        else
        {
            long b = 0, g = 0, r = 0, a = 0;
            fixed (byte* ptr = bgra)
            {
                ReadOnlySpan<Bgra32> pixels = new(ptr, bgra.Length / 4);
                foreach (ref readonly Bgra32 pixel in pixels)
                {
                    b += pixel.B;
                    g += pixel.G;
                    r += pixel.R;
                    a += pixel.A;
                }
                Unsafe.SkipInit(out Color color);
                color.B = (byte)(b / pixels.Length);
                color.G = (byte)(g / pixels.Length);
                color.R = (byte)(r / pixels.Length);
                color.A = (byte)(a / pixels.Length);
                return color;
            }
        }
    }




    public unsafe static int[] GetHueColorCircle(byte[] bgra, int width, int height)
    {
        fixed (byte* ptr = bgra)
        {
            Span<Bgra32> pixels = new(ptr, bgra.Length / 4);
            float[] hs = new float[pixels.Length];

            using var input = GraphicsDevice.GetDefault().AllocateReadOnlyTexture2D<Bgra32, float4>(pixels, width, height);
            using var output = GraphicsDevice.GetDefault().AllocateReadWriteTexture2D<float>(width, height);

            GraphicsDevice.GetDefault().For(width, height, new HueComputeShader(input, output));
            output.CopyTo(hs);

            var circle = new int[360];
            for (int i = 0; i < hs.Length; i++)
            {
                // ignore white black gray
                if (hs[i] > 0)
                {
                    circle[(int)hs[i]]++;
                }
            }
            return circle;
        }
    }



    [AutoConstructor]
    [EmbeddedBytecode(8, 8, 1)]
    private readonly partial struct HueComputeShader : IComputeShader
    {
        public readonly IReadOnlyNormalizedTexture2D<float4> input;

        public readonly ReadWriteTexture2D<float> output;

        public void Execute()
        {
            float4 bgra = input[ThreadIds.XY];
            float max = Math.Max(Math.Max(bgra.R, bgra.G), bgra.B);
            float min = Math.Min(Math.Min(bgra.R, bgra.G), bgra.B);
            float chroma = max - min;
            float h1;

            if (chroma == 0)
            {
                // ignore white black gray
                h1 = -1;
            }
            else if (max == bgra.R)
            {
                h1 = (((bgra.G - bgra.B) / chroma) + 6) % 6;
            }
            else if (max == bgra.G)
            {
                h1 = 2 + ((bgra.B - bgra.R) / chroma);
            }
            else
            {
                h1 = 4 + ((bgra.R - bgra.G) / chroma);
            }

            output[ThreadIds.XY] = 60 * h1;
        }
    }


}
