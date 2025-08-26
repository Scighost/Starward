using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL;

/// <summary>
/// Data type for describing the interpretation of the input and output buffers
/// in terms of the range of allowed input and output pixel values.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlBitDepth
{
    /// <summary>
    /// Bit depth setting, see comment on <see cref="JxlBitDepthType"/>
    /// </summary>
    public JxlBitDepthType Type;

    /// <summary>
    /// Custom bits per sample
    /// </summary>
    public uint BitsPerSample;

    /// <summary>
    /// Custom exponent bits per sample
    /// </summary>
    public uint ExponentBitsPerSample;
}