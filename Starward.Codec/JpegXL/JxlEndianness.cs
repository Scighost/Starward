namespace Starward.Codec.JpegXL;

/// <summary>
/// Ordering of multi-byte data.
/// </summary>
public enum JxlEndianness
{
    /// <summary>
    /// <para>
    /// Use the endianness of the system, either little endian or big endian,
    /// without forcing either specific endianness.
    /// </para>
    /// <para>
    /// Do not use if pixel data should be exported to a well defined format.
    /// </para>
    /// </summary>
    Native = 0,

    /// <summary>
    /// Force little endian
    /// </summary>
    Little = 1,

    /// <summary>
    /// Force big endian
    /// </summary>
    Big = 2,
}
