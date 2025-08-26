using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// The information about layers.
/// When decoding, if coalescing is enabled (default), this can be ignored.
/// When encoding, these settings apply to the pixel data given to the encoder,
/// the encoder could choose an internal representation that differs.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlLayerInfo
{
    /// <summary>
    /// Whether cropping is applied for this frame. When decoding, if false,
    /// crop_x0 and crop_y0 are set to zero, and xsize and ysize to the main
    /// image dimensions. When encoding and this is false, those fields are
    /// ignored. When decoding, if coalescing is enabled (default), this is always
    /// false, regardless of the internal encoding in the JPEG XL codestream.
    /// </summary>
    public JxlBool HaveCrop;

    /// <summary>
    /// Horizontal offset of the frame (can be negative).
    /// </summary>
    public int CropX0;

    /// <summary>
    /// Vertical offset of the frame (can be negative).
    /// </summary>
    public int CropY0;

    /// <summary>
    /// Width of the frame (number of columns).
    /// </summary>
    public uint XSize;

    /// <summary>
    /// Height of the frame (number of rows).
    /// </summary>
    public uint YSize;

    /// <summary>
    /// The blending info for the color channels. Blending info for extra channels
    /// has to be retrieved separately using <see cref="Decode.JxlDecoderNativeMethod.JxlDecoderGetExtraChannelBlendInfo"/>.
    /// </summary>
    public JxlBlendInfo BlendInfo;

    /// <summary>
    /// After blending, save the frame as reference frame with this ID (0-3).
    /// Special case: if the frame duration is nonzero, ID 0 means "will not be
    /// referenced in the future". This value is not used for the last frame.
    /// When encoding, ID 3 is reserved to frames that are generated internally by
    /// the encoder, and should not be used by applications.
    /// </summary>
    public uint SaveAsReference;
}
