namespace Starward.Codec.JpegXL;

/// <summary>
/// Data type for the sample values per channel per pixel.
/// </summary>
public enum JxlDataType
{
    /// <summary>
    /// <para>
    /// Use 32-bit single-precision floating point values, with range 0.0-1.0
    /// (within gamut, may go outside this range for wide color gamut).
    /// </para>
    /// <para>
    /// Floating point output, either <see cref="Float"/> or <see cref="Float16"/>, is recommended
    /// for HDR and wide gamut images when color profile conversion is required.
    /// </para>
    /// </summary>
    Float = 0,

    /// <summary>
    /// Use type <see langword="byte"/>. May clip wide color gamut data.
    /// </summary>
    UInt8 = 2,

    /// <summary>
    /// Use type <see langword="ushort"/>. May clip wide color gamut data.
    /// </summary>
    UInt16 = 3,

    /// <summary>
    /// Use 16-bit IEEE 754 half-precision floating point values
    /// </summary>
    Float16 = 5,
}
