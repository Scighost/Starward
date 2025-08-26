using Starward.Codec.JpegXL.Encode;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// Information for a single extra channel.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlExtraChannelInfo
{
    /// <summary>
    /// Given type of an extra channel.
    /// </summary>
    public JxlExtraChannelType Type;

    /// <summary>
    /// Total bits per sample for this channel.
    /// </summary>
    public uint BitsPerSample;

    /// <summary>
    /// Floating point exponent bits per channel, or 0 if they are unsigned
    /// integer.
    /// </summary>
    public uint ExponentBitsPerSample;

    /// <summary>
    /// The exponent the channel is downsampled by on each axis.
    /// TODO(lode): expand this comment to match the JPEG XL specification,
    /// specify how to upscale, how to round the size computation, and to which
    /// extra channels this field applies.
    /// </summary>
    public uint DimShift;

    /// <summary>
    /// Length of the extra channel name in bytes, or 0 if no name.
    /// Excludes null termination character.
    /// </summary>
    public uint NameLength;

    /// <summary>
    /// Whether alpha channel uses premultiplied alpha. Only applicable if
    /// type is <see cref="JxlExtraChannelType.Alpha"/>.
    /// </summary>
    public JxlBool AlphaPremultiplied;

    /// <summary>
    /// Spot color of the current spot channel in linear RGBA. Only applicable if
    /// type is <see cref="JxlExtraChannelType.SpotColor"/>.
    /// </summary>
    public unsafe fixed float SpotColor[4];

    /// <summary>
    /// Only applicable if type is <see cref="JxlExtraChannelType.CFA"/>.
    /// TODO(lode): add comment about the meaning of this field.
    /// </summary>
    public uint CfaChannel;


    /// <summary>
    /// Initializes a new instance of the <see cref="JxlExtraChannelInfo"/> struct.
    /// </summary>
    /// <param name="type">The type of the extra channel.</param>
    public JxlExtraChannelInfo(JxlExtraChannelType type = JxlExtraChannelType.Alpha)
    {
        JxlEncoderNativeMethod.JxlEncoderInitExtraChannelInfo(type, ref this);
    }

}