using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;

/// <summary>
/// Compressed Image Descriptor
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct UhdrCompressedImage
{
    /// <summary>
    /// Pointer to a block of data to decode
    /// </summary>
    public IntPtr Data;
    /// <summary>
    /// size of the data buffer
    /// </summary>
    public ulong DataSize;
    /// <summary>
    /// maximum size of the data buffer
    /// </summary>
    public ulong Capacity;
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
}



public struct UhdrCompressedImagePtr
{
    private IntPtr _ptr;

    public bool IsNull => _ptr == IntPtr.Zero;


    public UhdrCompressedImage ToCompressedImage()
    {
        if (IsNull)
        {
            throw new InvalidOperationException("Pointer is null. Cannot convert to UhdrCompressedImage.");
        }
        return Marshal.PtrToStructure<UhdrCompressedImage>(_ptr);
    }
}