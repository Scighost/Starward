using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL;

/// <summary>
/// <para>
/// Data type for the sample values per channel per pixel for the output buffer
/// for pixels. This is not necessarily the same as the data type encoded in the
/// codestream.
/// </para>
/// <para>
/// The channels are interleaved per pixel. The pixels are
/// organized row by row, left to right, top to bottom.
/// </para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public record struct JxlPixelFormat
{
    /// <summary>
    /// <para>Amount of channels available in a pixel buffer.</para>
    /// <para>1: single-channel data, e.g. grayscale or a single extra channel</para>
    /// <para>2: single-channel + alpha</para>
    /// <para>3: trichromatic, e.g. RGB</para>
    /// <para>4: trichromatic + alpha</para>
    /// </summary>
    public uint NumChannels;

    /// <summary>
    /// Data type of each channel.
    /// </summary>
    public JxlDataType DataType;

    /// <summary>
    /// Whether multi-byte data types are represented in big endian or little
    /// endian format. This applies to <see cref="JxlDataType.UInt16"/>, 
    /// <see cref="JxlDataType.Float16"/> and <see cref="JxlDataType.Float"/>.
    /// </summary>
    public JxlEndianness Endianness;

    /// <summary>
    /// Align scanlines to a multiple of align bytes, or 0 to require no
    /// alignment at all (which has the same effect as value 1).
    /// </summary>
    public nuint Align;


    /// <summary>
    /// 3-channel, 8-bit unsigned integer, RGB pixel format.
    /// </summary>
    public static JxlPixelFormat R8G8B8UInt => new JxlPixelFormat
    {
        NumChannels = 3,
        DataType = JxlDataType.UInt8,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };

    /// <summary>
    /// 4-channel, 8-bit unsigned integer, RGBA pixel format.
    /// </summary>
    public static JxlPixelFormat R8G8B8A8UInt => new JxlPixelFormat
    {
        NumChannels = 4,
        DataType = JxlDataType.UInt8,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };

    /// <summary>
    /// 3-channel, 16-bit unsigned integer, RGB pixel format.
    /// </summary>
    public static JxlPixelFormat R16G16B16UInt => new JxlPixelFormat
    {
        NumChannels = 3,
        DataType = JxlDataType.UInt16,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };

    /// <summary>
    /// 4-channel, 16-bit unsigned integer, RGBA pixel format.
    /// </summary>
    public static JxlPixelFormat R16G16B16A16UInt => new JxlPixelFormat
    {
        NumChannels = 4,
        DataType = JxlDataType.UInt16,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };

    /// <summary>
    /// 3-channel, 16-bit float, RGB pixel format.
    /// </summary>
    public static JxlPixelFormat R16G16B16Float => new JxlPixelFormat
    {
        NumChannels = 3,
        DataType = JxlDataType.Float16,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };

    /// <summary>
    /// 4-channel, 16-bit float, RGBA pixel format.
    /// </summary>
    public static JxlPixelFormat R16G16B16A16Float => new JxlPixelFormat
    {
        NumChannels = 4,
        DataType = JxlDataType.Float16,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };

    /// <summary>
    /// 3-channel, 32-bit float, RGB pixel format.
    /// </summary>
    public static JxlPixelFormat R32G32B32Float => new JxlPixelFormat
    {
        NumChannels = 3,
        DataType = JxlDataType.Float,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };

    /// <summary>
    /// 4-channel, 32-bit float, RGBA pixel format.
    /// </summary>
    public static JxlPixelFormat R32G32B32A32Float => new JxlPixelFormat
    {
        NumChannels = 4,
        DataType = JxlDataType.Float,
        Endianness = JxlEndianness.Native,
        Align = 0,
    };


}
