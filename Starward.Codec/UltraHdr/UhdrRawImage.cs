using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;

/// <summary>
/// Raw Image Descriptor
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdrRawImage
{
    /// <summary>
    /// Pixel Format
    /// </summary>
    public UhdrPixelFormat PixelFormat;
    /// <summary>
    /// Color Gamut
    /// </summary>
    public UhdrColorGamut ColorGamut;
    /// <summary>
    /// Color Transfer
    /// </summary>
    public UhdrColorTransfer ColorTransfer;
    /// <summary>
    /// Color Range
    /// </summary>
    public UhdrColorRange ColorRange;

    /// <summary>
    /// Stored image width
    /// </summary>
    public uint Width;
    /// <summary>
    /// Stored image height
    /// </summary>
    public uint Height;

    /// <summary>
    /// pointer to the top left pixel for each plane
    /// </summary>
    public IntPtr Plane0;
    public IntPtr Plane1;
    public IntPtr Plane2;

    /// <summary>
    /// stride in pixels between rows for each plane
    /// </summary>
    public uint Stride0;
    public uint Stride1;
    public uint Stride2;
}
