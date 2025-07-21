namespace Starward.Codec.UltraHdr;

/// <summary>
/// List of supported pixel formats
/// </summary>
public enum UhdrPixelFormat
{
    /// <summary>
    /// Unspecified
    /// </summary>
    Unspecified = -1,
    /// <summary>
    /// 10-bit-per component 4:2:0 YCbCr semiplanar format.
    /// Each chroma and luma component has 16 allocated bits in
    /// little-endian configuration with 10 MSB of actual data.
    /// </summary>
    _24bppYCbCrP010 = 0,
    /// <summary>
    /// 8-bit-per component 4:2:0 YCbCr planar format
    /// </summary>
    _12bppYCbCr420 = 1,
    /// <summary>
    /// 8-bit-per component Monochrome format
    /// </summary>
    _8bppYCbCr400 = 2,
    /// <summary>
    /// 32 bits per pixel RGBA color format, with 8-bit red, green, blue
    /// and alpha components. Using 32-bit little-endian representation,
    /// colors stored as Red 7:0, Green 15:8, Blue 23:16, Alpha 31:24.
    /// </summary>
    _32bppRGBA8888 = 3,
    /// <summary>
    /// 64 bits per pixel, 16 bits per channel, half-precision floating point RGBA color
    /// format. colors stored as Red 15:0, Green 31:16, Blue 47:32, Alpha 63:48. In a pixel
    /// even though each channel has storage space of 16 bits, the nominal range is expected to
    /// be [0.0..(10000/203)]
    /// </summary>
    _64bppRGBAHalfFloat = 4,
    /// <summary>
    /// 32 bits per pixel RGBA color format, with 10-bit red,
    /// green, blue, and 2-bit alpha components. Using 32-bit
    /// little-endian representation, colors stored as Red 9:0, Green
    /// 19:10, Blue 29:20, and Alpha 31:30.
    /// </summary>
    _32bppRGBA1010102 = 5,
    /// <summary>
    /// 8-bit-per component 4:4:4 YCbCr planar format
    /// </summary>
    _24bppYCbCr444 = 6,
    /// <summary>
    /// 8-bit-per component 4:2:2 YCbCr planar format
    /// </summary>
    _16bppYCbCr422 = 7,
    /// <summary>
    /// 8-bit-per component 4:4:0 YCbCr planar format
    /// </summary>
    _16bppYCbCr440 = 8,
    /// <summary>
    /// 8-bit-per component 4:1:1 YCbCr planar format
    /// </summary>
    _12bppYCbCr411 = 9,
    /// <summary>
    /// 8-bit-per component 4:1:0 YCbCr planar format
    /// </summary>
    _10bppYCbCr410 = 10,
    /// <summary>
    /// 8-bit-per component RGB interleaved format
    /// </summary>
    _24bppRGB888 = 11,
    /// <summary>
    /// 10-bit-per component 4:4:4 YCbCr planar format
    /// </summary>
    _30bppYCbCr444 = 12,
}
