namespace Starward.Codec.JpegXL;

/// <summary>
/// Settings for the interpretation of UINT input and output buffers.
/// (buffers using a FLOAT data type are not affected by this)
/// </summary>
public enum JxlBitDepthType
{
    /// <summary>
    /// <para>
    /// This is the default setting, where the encoder expects the input pixels
    /// to use the full range of the pixel format data type (e.g. for UINT16, the
    /// input range is 0 .. 65535 and the value 65535 is mapped to 1.0 when
    /// converting to float), and the decoder uses the full range to output
    /// pixels.
    /// </para>
    /// <para>
    /// If the bit depth in the basic info is different from this, the
    /// encoder expects the values to be rescaled accordingly (e.g. multiplied by
    /// 65535/4095 for a 12-bit image using UINT16 input data type).
    /// </para>
    /// </summary>
    FromPixelFormat = 0,

    /// <summary>
    /// If this setting is selected, the encoder expects the input pixels to be
    /// in the range defined by the <c>bits_per_sample</c> value of the basic info (e.g.
    /// for 12-bit images using UINT16 input data types, the allowed range is
    /// 0 .. 4095 and the value 4095 is mapped to 1.0 when converting to float),
    /// and the decoder outputs pixels in this range.
    /// </summary>
    FromCodestream = 1,

    /// <summary>
    /// This setting can only be used in the decoder to select a custom range for
    /// pixel output
    /// </summary>
    Custom = 2,
}
