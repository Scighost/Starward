using Starward.Codec.JpegXL.Encode;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// Basic image information. This information is available from the file
/// signature and first part of the codestream header.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlBasicInfo
{
    /// <summary>
    /// Whether the codestream is embedded in the container format. If true,
    /// metadata information and extensions may be available in addition to the
    /// codestream.
    /// </summary>
    public JxlBool HaveContainer;

    /// <summary>
    /// Width of the image in pixels, before applying orientation.
    /// </summary>
    public uint XSize;

    /// <summary>
    /// Height of the image in pixels, before applying orientation.
    /// </summary>
    public uint YSize;

    /// <summary>
    /// Original image color channel bit depth.
    /// </summary>
    public uint BitsPerSample;

    /// <summary>
    /// Original image color channel floating point exponent bits, or 0 if they
    /// are unsigned integer. For example, if the original data is half-precision
    /// (binary16) floating point, <see cref="BitsPerSample"/> is 16 and
    /// <see cref="ExponentBitsPerSample"/> is 5, and so on for other floating point
    /// precisions.
    /// </summary>
    public uint ExponentBitsPerSample;

    /// <summary>
    /// <para>
    /// Upper bound on the intensity level present in the image in nits. For
    /// unsigned integer pixel encodings, this is the brightness of the largest
    /// representable value. The image does not necessarily contain a pixel
    /// actually this bright. An encoder is allowed to set 255 for SDR images
    /// without computing a histogram.
    /// </para>
    /// <para>
    /// Leaving this set to its default of 0 lets libjxl choose a sensible default
    /// value based on the color encoding.
    /// </para>
    /// </summary>
    public float IntensityTarget;

    /// <summary>
    /// Lower bound on the intensity level present in the image. This may be
    /// loose, i.e. lower than the actual darkest pixel. When tone mapping, a
    /// decoder will map [<see cref="MinNits"/>, <see cref="IntensityTarget"/>] to the display range.
    /// </summary>
    public float MinNits;

    /// <summary>
    /// See the description of <see cref="LinearBelow"/>.
    /// </summary>
    public JxlBool RelativeToMaxDisplay;

    /// <summary>
    /// The tone mapping will leave unchanged (linear mapping) any pixels whose
    /// brightness is strictly below this. The interpretation depends on
    /// <see cref="RelativeToMaxDisplay"/>. If true, this is a ratio [0, 1] of the maximum
    /// display brightness [nits], otherwise an absolute brightness [nits].
    /// </summary>
    public float LinearBelow;

    /// <summary>
    /// <para>
    /// Whether the data in the codestream is encoded in the original color
    /// profile that is attached to the codestream metadata header, or is
    /// encoded in an internally supported absolute color space (which the decoder
    /// can always convert to linear or non-linear sRGB or to XYB). If the original
    /// profile is used, the decoder outputs pixel data in the color space matching
    /// that profile, but doesn't convert it to any other color space. If the
    /// original profile is not used, the decoder only outputs the data as sRGB
    /// (linear if outputting to floating point, nonlinear with standard sRGB
    /// transfer function if outputting to unsigned integers) but will not convert
    /// it to to the original color profile. The decoder also does not convert to
    /// the target display color profile.
    /// </para>
    /// <para>
    /// To convert the pixel data produced by
    /// the decoder to the original color profile, one of the JxlDecoderGetColor*
    /// functions needs to be called with
    /// <see cref="Decode.JxlColorProfileTarget.Data"/> to get the color profile of the decoder
    /// output, and then an external CMS can be used for conversion. Note that for
    /// lossy compression, this should be set to false for most use cases, and if
    /// needed, the image should be converted to the original color profile after
    /// decoding, as described above.
    /// </para>
    /// </summary>
    public JxlBool UsesOriginalProfile;

    /// <summary>
    /// Indicates a preview image exists near the beginning of the codestream.
    /// The preview itself or its dimensions are not included in the basic info.
    /// </summary>
    public JxlBool HavePreview;

    /// <summary>
    /// Indicates animation frames exist in the codestream. The animation
    /// information is not included in the basic info.
    /// </summary>
    public JxlBool HaveAnimation;

    /// <summary>
    /// Image orientation, value 1-8 matching the values used by JEITA CP-3451C
    /// (Exif version 2.3).
    /// </summary>
    public JxlOrientation Orientation;

    /// <summary>
    /// <para>
    /// Number of color channels encoded in the image, this is either 1 for
    /// grayscale data, or 3 for colored data. This count does not include
    /// the alpha channel or other extra channels. To check presence of an alpha
    /// channel, such as in the case of RGBA color, check <see cref="AlphaBits"/> != 0.
    /// </para>
    /// <para>
    /// If and only if this is 1, the <see cref="CMS.JxlColorSpace"/> in the
    /// <see cref="CMS.JxlColorEncoding"/> is <see cref="CMS.JxlColorSpace.Gray"/>.
    /// </para>
    /// </summary>
    public uint NumColorChannels;

    /// <summary>
    /// <para>
    /// Number of additional image channels. This includes the main alpha channel,
    /// but can also include additional channels such as depth, additional alpha
    /// channels, spot colors, and so on. Information about the extra channels
    /// can be queried with <see cref="Decode.JxlDecoderNativeMethod.JxlDecoderGetExtraChannelInfo"/>.
    /// </para>
    /// <para>
    /// The main alpha
    /// channel, if it exists, also has its information available in the
    /// <see cref="AlphaBits"/>, <see cref="AlphaExponentBits"/> and <see cref="AlphaPremultiplied"/> fields in this
    /// <see cref="JxlBasicInfo"/>.
    /// </para>
    /// </summary>
    public uint NumExtraChannels;

    /// <summary>
    /// Bit depth of the encoded alpha channel, or 0 if there is no alpha channel.
    /// If present, matches the <see cref="AlphaBits"/> value of the <see cref="JxlExtraChannelInfo"/>
    /// associated with this alpha channel.
    /// </summary>
    public uint AlphaBits;

    /// <summary>
    /// Alpha channel floating point exponent bits, or 0 if they are unsigned
    /// integer. If present, matches the <see cref="AlphaExponentBits"/> value of the <see cref="JxlExtraChannelInfo"/>
    /// associated with this alpha channel.
    /// </summary>
    public uint AlphaExponentBits;

    /// <summary>
    /// Whether the alpha channel is premultiplied. Only used if there is a main
    /// alpha channel. Matches the <see cref="AlphaPremultiplied"/> value of the
    /// <see cref="JxlExtraChannelInfo"/> associated with this alpha channel.
    /// </summary>
    public JxlBool AlphaPremultiplied;

    /// <summary>
    /// Dimensions of encoded preview image, only used if <see cref="HavePreview"/> is
    /// <see langword="true"/>.
    /// </summary>
    public JxlPreviewHeader Preview;

    /// <summary>
    /// Animation header with global animation properties for all frames, only
    /// used if <see cref="HaveAnimation"/> is <see langword="true"/>.
    /// </summary>
    public JxlAnimationHeader Animation;

    /// <summary>
    /// Intrinsic width of the image.
    /// The intrinsic size can be different from the actual size in pixels
    /// (as given by <see cref="XSize"/> and <see cref="YSize"/>) and it denotes the recommended dimensions
    /// for displaying the image, i.e. applications are advised to resample the
    /// decoded image to the intrinsic dimensions.
    /// </summary>
    public uint IntrinsicXSize;

    /// <summary>
    /// Intrinsic height of the image.
    /// The intrinsic size can be different from the actual size in pixels
    /// (as given by <see cref="XSize"/> and <see cref="YSize"/>) and it denotes the recommended dimensions
    /// for displaying the image, i.e. applications are advised to resample the
    /// decoded image to the intrinsic dimensions.
    /// </summary>
    public uint IntrinsicYSize;

    /// <summary>
    /// Padding for forwards-compatibility, in case more fields are exposed
    /// in a future version of the library.
    /// </summary>
    public unsafe fixed byte Padding[100];


    /// <summary>
    /// Initializes a new instance of the <see cref="JxlBasicInfo"/> struct.
    /// </summary>
    public JxlBasicInfo()
    {
        JxlEncoderNativeMethod.JxlEncoderInitBasicInfo(ref this);
    }


    /// <summary>
    /// Initializes a new instance of the <see cref="JxlBasicInfo"/> struct with specified parameters.
    /// </summary>
    /// <param name="width">Width of the image.</param>
    /// <param name="height">Height of the image.</param>
    /// <param name="jxlPixelFormat">The pixel format of the image.</param>
    /// <param name="alphaPremultiplied">Whether the alpha channel is premultiplied.</param>
    /// <exception cref="NotSupportedException">Unsupported data type in pixel format.</exception>
    public JxlBasicInfo(uint width, uint height, in JxlPixelFormat jxlPixelFormat, bool alphaPremultiplied = false)
    {
        JxlEncoderNativeMethod.JxlEncoderInitBasicInfo(ref this);
        XSize = width;
        YSize = height;
        bool hasAlpha = jxlPixelFormat.NumChannels is 2 or 4;
        BitsPerSample = jxlPixelFormat.DataType switch
        {
            JxlDataType.UInt8 => 8,
            JxlDataType.UInt16 => 16,
            JxlDataType.Float16 => 16,
            JxlDataType.Float => 32,
            _ => throw new NotSupportedException($"Unsupported data type: {jxlPixelFormat.DataType}"),
        };
        ExponentBitsPerSample = jxlPixelFormat.DataType switch
        {
            JxlDataType.Float16 => 5,
            JxlDataType.Float => 8,
            _ => 0,
        };
        AlphaBits = hasAlpha ? BitsPerSample : 0;
        AlphaExponentBits = hasAlpha ? ExponentBitsPerSample : 0;
        NumColorChannels = jxlPixelFormat.NumChannels - (hasAlpha ? 1u : 0u);
        NumExtraChannels = hasAlpha ? 1u : 0u;
        AlphaPremultiplied = alphaPremultiplied;
    }

}