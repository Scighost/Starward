using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public struct avifScalingMode
{
    public avifFraction Horizontal;
    public avifFraction Vertical;
}
