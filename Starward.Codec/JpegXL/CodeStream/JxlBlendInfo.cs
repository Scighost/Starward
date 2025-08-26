using Starward.Codec.JpegXL.Encode;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// The information about blending the color channels or a single extra channel.
/// When decoding, if coalescing is enabled (default), this can be ignored and
/// the blend mode is considered to be <see cref="JxlBlendMode.Replace"/>.
/// When encoding, these settings apply to the pixel data given to the encoder.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlBlendInfo
{
    /// <summary>
    /// Blend mode.
    /// </summary>
    public JxlBlendMode blendMode;

    /// <summary>
    /// Reference frame ID to use as the 'bottom' layer (0-3).
    /// </summary>
    public uint Source;

    /// <summary>
    /// Which extra channel to use as the 'alpha' channel for blend modes
    /// <see cref="JxlBlendMode.Blend"/> and <see cref="JxlBlendMode.MulAdd"/>.
    /// </summary>
    public uint Alpha;

    /// <summary>
    /// Clamp values to [0,1] for the purpose of blending.
    /// </summary>
    public JxlBool Clamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="JxlBlendInfo"/> struct.
    /// </summary>
    public JxlBlendInfo()
    {
        JxlEncoderNativeMethod.JxlEncoderInitBlendInfo(ref this);
    }

}
