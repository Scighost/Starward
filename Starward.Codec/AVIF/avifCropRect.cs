using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public record struct avifCropRect
{
    public uint X;
    public uint Y;
    public uint Width;
    public uint Height;
}
