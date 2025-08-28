using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

/// <summary>
/// 'pasp' from ISO/IEC 14496-12:2022 12.1.4.3
/// define the relative width and height of a pixel
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct avifPixelAspectRatioBox
{
    public uint HSpacing;
    public uint VSpacing;
}
