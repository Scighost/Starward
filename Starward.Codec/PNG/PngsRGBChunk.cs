using System.Runtime.InteropServices;

namespace Starward.Codec.PNG;

[StructLayout(LayoutKind.Sequential)]
public struct PngsRGBChunk
{
    public byte RenderingIntent; // 0: Perceptual, 1: Relative colorimetric, 2: Saturation, 3: Absolute colorimetric
}
