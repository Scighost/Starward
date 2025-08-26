using Starward.Codec.JpegXL.Encode;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// The header of one displayed frame or non-coalesced layer.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlFrameHeader
{
    /// <summary>
    /// How long to wait after rendering in ticks. The duration in seconds of a
    /// tick is given by <see cref="JxlAnimationHeader.TpsNumerator"/> and <see cref="JxlAnimationHeader.TpsDenominator"/>.
    /// </summary>
    public uint Duration;

    /// <summary>
    /// SMPTE timecode of the current frame in form 0xHHMMSSFF, or 0. The bits are
    /// interpreted from most-significant to least-significant as hour, minute,
    /// second, and frame. If timecode is nonzero, it is strictly larger than that
    /// of a previous frame with nonzero duration. These values are only available
    /// if <see cref="JxlAnimationHeader.HaveTimeCodes"/> is <see langword="true"/>.
    /// This value is only used if <see cref="JxlAnimationHeader.HaveTimeCodes"/> is
    /// <see langword="true"/>.
    /// </summary>
    public uint TimeCode;

    /// <summary>
    /// Length of the frame name in bytes, or 0 if no name.
    /// Excludes null termination character. This value is set by the decoder.
    /// For the encoder, this value is ignored and JxlEncoderSetFrameName is
    /// used instead to set the name and the length.
    /// </summary>
    public uint NameLength;

    /// <summary>
    /// Indicates this is the last animation frame. This value is set by the
    /// decoder to indicate no further frames follow. For the encoder, it is not
    /// required to set this value and it is ignored, JxlEncoderCloseFrames is
    /// used to indicate the last frame to the encoder instead.
    /// </summary>
    public JxlBool IsLast;

    /// <summary>
    /// Information about the layer in case of no coalescing.
    /// </summary>
    public JxlLayerInfo LayerInfo;


    /// <summary>
    /// Initializes a new instance of the <see cref="JxlFrameHeader"/> struct.
    /// </summary>
    public JxlFrameHeader()
    {
        JxlEncoderNativeMethod.JxlEncoderInitFrameHeader(ref this);
    }

}

