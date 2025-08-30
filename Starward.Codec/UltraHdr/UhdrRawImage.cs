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
    public FixedArray3<IntPtr> Plane;

    /// <summary>
    /// stride in pixels between rows for each plane
    /// </summary>
    public FixedArray3<uint> Stride;
}


public struct UhdrRawImagePtr
{
    private IntPtr _ptr;
    public bool IsNull => _ptr == IntPtr.Zero;

    public UhdrRawImage ToRawImage()
    {
        if (IsNull)
        {
            throw new InvalidOperationException("Pointer is null. Cannot convert to UhdrRawImage.");
        }
        return Marshal.PtrToStructure<UhdrRawImage>(_ptr);
    }
}
