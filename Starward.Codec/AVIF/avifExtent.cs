using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public record struct avifExtent
{
    public ulong Offset;
    public nuint Size;
}
