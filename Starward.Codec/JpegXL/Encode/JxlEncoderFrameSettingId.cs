namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// ID of encoder options for a frame. 
/// <para>This includes options such as setting encoding effort/speed or overriding the use of certain coding tools, for this frame.</para>
/// <para>This does not include non-frame related encoder options such as for boxes.</para>
/// </summary>
public enum JxlEncoderFrameSettingId
{
    /// <summary>
    /// Sets encoder effort/speed level without affecting decoding speed.
    /// <para>Valid values are, from faster to slower speed: 1:lightning 2:thunder 3:falcon 4:cheetah 5:hare 6:wombat 7:squirrel 8:kitten 9:tortoise 10:glacier.</para>
    /// <para>Default: squirrel (7).</para>
    /// </summary>
    Effort = 0,

    /// <summary>
    /// Sets the decoding speed tier for the provided options.
    /// <para>Minimum is 0 (slowest to decode, best quality/density), and maximum is 4 (fastest to decode, at the cost of some quality/density).</para>
    /// <para>Default is 0.</para>
    /// </summary>
    DecodingSpeed = 1,

    /// <summary>
    /// Sets resampling option. If enabled, the image is downsampled before compression, and upsampled to original size in the decoder.
    /// <para>Integer option, use -1 for the default behavior (resampling only applied for low quality), 1 for no downsampling (1x1), 2 for 2x2 downsampling, 4 for 4x4 downsampling, 8 for 8x8 downsampling.</para>
    /// </summary>
    Resampling = 2,

    /// <summary>
    /// Similar to <see cref="Resampling"/>, but for extra channels.
    /// <para>Integer option, use -1 for the default behavior (depends on encoder implementation), 1 for no downsampling (1x1), 2 for 2x2 downsampling, 4 for 4x4 downsampling, 8 for 8x8 downsampling.</para>
    /// </summary>
    ExtraChannelResampling = 3,

    /// <summary>
    /// Indicates the frame added with <see cref="JxlEncoderFrameSettings.AddImageFrame"/> is already downsampled by the downsampling factor set with <see cref="Resampling"/>.
    /// <para>The input frame must then be given in the downsampled resolution, not the full image resolution.</para>
    /// <para>The downsampled resolution is given by <c>ceil(xsize / resampling)</c>, <c>ceil(ysize / resampling)</c> with xsize and ysize the dimensions given in the basic info, and resampling the factor set with <see cref="Resampling"/>.</para>
    /// <para>Use 0 to disable, 1 to enable. Default value is 0.</para>
    /// </summary>
    AlreadyDownsampled = 4,

    /// <summary>
    /// Adds noise to the image emulating photographic film noise, the higher the given number, the grainier the image will be.
    /// <para>As an example, a value of 100 gives low noise whereas a value of 3200 gives a lot of noise.</para>
    /// <para>The default value is 0.</para>
    /// </summary>
    PhotonNoise = 5,

    /// <summary>
    /// Enables adaptive noise generation. This setting is not recommended for use, please use <see cref="PhotonNoise"/> instead.
    /// <para>Use -1 for the default (encoder chooses), 0 to disable, 1 to enable.</para>
    /// </summary>
    Noise = 6,

    /// <summary>
    /// Enables or disables dots generation.
    /// <para>Use -1 for the default (encoder chooses), 0 to disable, 1 to enable.</para>
    /// </summary>
    Dots = 7,

    /// <summary>
    /// Enables or disables patches generation.
    /// <para>Use -1 for the default (encoder chooses), 0 to disable, 1 to enable.</para>
    /// </summary>
    Patches = 8,

    /// <summary>
    /// Edge preserving filter level, -1 to 3.
    /// <para>Use -1 for the default (encoder chooses), 0 to 3 to set a strength.</para>
    /// </summary>
    EPF = 9,

    /// <summary>
    /// Enables or disables the gaborish filter.
    /// <para>Use -1 for the default (encoder chooses), 0 to disable, 1 to enable.</para>
    /// </summary>
    Gaborish = 10,

    /// <summary>
    /// Enables modular encoding.
    /// <para>Use -1 for default (encoder chooses), 0 to enforce VarDCT mode (e.g. for photographic images), 1 to enforce modular mode (e.g. for lossless images).</para>
    /// </summary>
    Modular = 11,

    /// <summary>
    /// Enables or disables preserving color of invisible pixels.
    /// <para>Use -1 for the default (1 if lossless, 0 if lossy), 0 to disable, 1 to enable.</para>
    /// </summary>
    KeepInvisible = 12,

    /// <summary>
    /// Determines the order in which 256x256 regions are stored in the codestream for progressive rendering.
    /// <para>Use -1 for the encoder default, 0 for scanline order, 1 for center-first order.</para>
    /// </summary>
    GroupOrder = 13,

    /// <summary>
    /// Determines the horizontal position of center for the center-first group order.
    /// <para>Use -1 to automatically use the middle of the image, 0..xsize to specifically set it.</para>
    /// </summary>
    GroupOrderCenterX = 14,

    /// <summary>
    /// Determines the center for the center-first group order.
    /// <para>Use -1 to automatically use the middle of the image, 0..ysize to specifically set it.</para>
    /// </summary>
    GroupOrderCenterY = 15,

    /// <summary>
    /// Enables or disables progressive encoding for modular mode.
    /// <para>Use -1 for the encoder default, 0 to disable, 1 to enable.</para>
    /// </summary>
    Responsive = 16,

    /// <summary>
    /// Set the progressive mode for the AC coefficients of VarDCT, using spectral progression from the DCT coefficients.
    /// <para>Use -1 for the encoder default, 0 to disable, 1 to enable.</para>
    /// </summary>
    ProgressiveAC = 17,

    /// <summary>
    /// Set the progressive mode for the AC coefficients of VarDCT, using quantization of the least significant bits.
    /// <para>Use -1 for the encoder default, 0 to disable, 1 to enable.</para>
    /// </summary>
    QprogressiveAC = 18,

    /// <summary>
    /// Set the progressive mode using lower-resolution DC images for VarDCT.
    /// <para>Use -1 for the encoder default, 0 to disable, 1 to have an extra 64x64 lower resolution pass, 2 to have a 512x512 and 64x64 lower resolution pass.</para>
    /// </summary>
    ProgressiveDC = 19,

    /// <summary>
    /// Use Global channel palette if the amount of colors is smaller than this percentage of range.
    /// <para>Use 0-100 to set an explicit percentage, -1 to use the encoder default.</para>
    /// <para>Used for modular encoding.</para>
    /// </summary>
    ChannelColorsGlobalPercent = 20,

    /// <summary>
    /// Use Local (per-group) channel palette if the amount of colors is smaller than this percentage of range.
    /// <para>Use 0-100 to set an explicit percentage, -1 to use the encoder default.</para>
    /// <para>Used for modular encoding.</para>
    /// </summary>
    ChannelColorsGroupPercent = 21,

    /// <summary>
    /// Use color palette if amount of colors is smaller than or equal to this amount, or -1 to use the encoder default.
    /// <para>Used for modular encoding.</para>
    /// </summary>
    PaletteColors = 22,

    /// <summary>
    /// Enables or disables delta palette.
    /// <para>Use -1 for the default (encoder chooses), 0 to disable, 1 to enable.</para>
    /// <para>Used in modular mode.</para>
    /// </summary>
    LossyPalette = 23,

    /// <summary>
    /// Color transform for internal encoding: -1 = default, 0=XYB, 1=none (RGB), 2=YCbCr.
    /// <para>The XYB setting performs the forward XYB transform.</para>
    /// <para>None and YCbCr both perform no transform, but YCbCr is used to indicate that the encoded data losslessly represents YCbCr values.</para>
    /// </summary>
    ColorTransform = 24,

    /// <summary>
    /// Reversible color transform for modular encoding: -1=default, 0-41=RCT index, e.g. index 0 = none, index 6 = YCoCg.
    /// <para>If this option is set to a non-default value, the RCT will be globally applied to the whole frame.</para>
    /// <para>The default behavior is to try several RCTs locally per modular group, depending on the speed and distance setting.</para>
    /// </summary>
    ModularColorSpace = 25,

    /// <summary>
    /// Group size for modular encoding: -1=default, 0=128, 1=256, 2=512, 3=1024.
    /// </summary>
    ModularGroupSize = 26,

    /// <summary>
    /// Predictor for modular encoding. -1 = default, 0=zero, 1=left, 2=top, 3=avg0, 4=select, 5=gradient, 6=weighted, 7=topright, 8=topleft, 9=leftleft, 10=avg1, 11=avg2, 12=avg3, 13=toptop predictive average 14=mix 5 and 6, 15=mix everything.
    /// </summary>
    ModularPredictor = 27,

    /// <summary>
    /// Fraction of pixels used to learn MA trees as a percentage.
    /// <para>-1 = default, 0 = no MA and fast decode, 50 = default value, 100 = all, values above 100 are also permitted.</para>
    /// <para>Higher values use more encoder memory.</para>
    /// </summary>
    ModularMaTreeLearningPercent = 28,

    /// <summary>
    /// Number of extra (previous-channel) MA tree properties to use.
    /// <para>-1 = default, 0-11 = valid values.</para>
    /// <para>Recommended values are in the range 0 to 3, or 0 to amount of channels minus 1 (including all extra channels, and excluding color channels when using VarDCT mode).</para>
    /// <para>Higher value gives slower encoding and slower decoding.</para>
    /// </summary>
    ModularNbPrevChannels = 29,

    /// <summary>
    /// Enable or disable CFL (chroma-from-luma) for lossless JPEG recompression.
    /// <para>-1 = default, 0 = disable CFL, 1 = enable CFL.</para>
    /// </summary>
    JpegReconCFL = 30,

    /// <summary>
    /// Prepare the frame for indexing in the frame index box.
    /// <para>0 = ignore this frame (same as not setting a value), 1 = index this frame within the Frame Index Box.</para>
    /// <para>If any frames are indexed, the first frame needs to be indexed, too.</para>
    /// <para>If the first frame is not indexed, and a later frame is attempted to be indexed, <see cref="JxlEncoderStatus.Error"/> will occur.</para>
    /// <para>If non-keyframes, i.e., frames with cropping, blending or patches are attempted to be indexed, <see cref="JxlEncoderStatus.Error"/> will occur.</para>
    /// </summary>
    FrameIndexBox = 31,

    /// <summary>
    /// Sets brotli encode effort for use in JPEG recompression and compressed metadata boxes (brob).
    /// <para>Can be -1 (default) or 0 (fastest) to 11 (slowest).</para>
    /// <para>Default is based on the general encode effort in case of JPEG recompression, and 4 for brob boxes.</para>
    /// </summary>
    BrotliEffort = 32,

    /// <summary>
    /// Enables or disables brotli compression of metadata boxes derived from a JPEG frame when using <see cref="JxlEncoderFrameSettings.AddJpegFrame"/>.
    /// <para>This has no effect on boxes added using <see cref="JxlEncoder.AddBox"/>.</para>
    /// <para>-1 = default, 0 = disable compression, 1 = enable compression.</para>
    /// </summary>
    JpegCompressBoxes = 33,

    /// <summary>
    /// Control what kind of buffering is used, when using chunked image frames.
    /// <para>-1 = default (let the encoder decide)</para>
    /// <para>0 = buffers everything, basically the same as non-streamed code path (mainly for testing)</para>
    /// <para>1 = buffers everything for images that are smaller than 2048 x 2048, and uses streaming input and output for larger images</para>
    /// <para>2 = uses streaming input and output for all images that are larger than one group, i.e. 256 x 256 pixels by default</para>
    /// <para>3 = currently same as 2</para>
    /// <para>When using streaming input and output the encoder minimizes memory usage at the cost of compression density.</para>
    /// <para>Also note that images produced with streaming mode might not be progressively decodeable.</para>
    /// </summary>
    Buffering = 34,

    /// <summary>
    /// Keep or discard Exif metadata boxes derived from a JPEG frame when using <see cref="JxlEncoderFrameSettings.AddJpegFrame"/>.
    /// <para>This has no effect on boxes added using <see cref="JxlEncoder.AddBox"/>.</para>
    /// <para>When <see cref="JxlEncoder.StoreJpegMetadata"/> is set to <see langword="true"/>, this option cannot be set to 0.</para>
    /// <para>Even when Exif metadata is discarded, the orientation will still be applied.</para>
    /// <para>0 = discard Exif metadata, 1 = keep Exif metadata (default).</para>
    /// </summary>
    JpegKeepExif = 35,

    /// <summary>
    /// Keep or discard XMP metadata boxes derived from a JPEG frame when using <see cref="JxlEncoderFrameSettings.AddJpegFrame"/>.
    /// <para>This has no effect on boxes added using <see cref="JxlEncoder.AddBox"/>.</para>
    /// <para>When <see cref="JxlEncoder.StoreJpegMetadata"/> is set to <see langword="true"/>, this option cannot be set to 0.</para>
    /// <para>0 = discard XMP metadata, 1 = keep XMP metadata (default).</para>
    /// </summary>
    JpegKeepXmp = 36,

    /// <summary>
    /// Keep or discard JUMBF metadata boxes derived from a JPEG frame when using <see cref="JxlEncoderFrameSettings.AddJpegFrame"/>.
    /// <para>This has no effect on boxes added using <see cref="JxlEncoder.AddBox"/>.</para>
    /// <para>0 = discard JUMBF metadata, 1 = keep JUMBF metadata (default).</para>
    /// </summary>
    JpegKeepJumbf = 37,

    /// <summary>
    /// If this mode is disabled, the encoder will not make any image quality decisions that are computed based on the full image, but stored only once (e.g. the X quant multiplier in the frame header).
    /// <para>Used mainly for testing equivalence of streaming and non-streaming code.</para>
    /// <para>0 = disabled, 1 = enabled (default)</para>
    /// </summary>
    UseFullImageHeuristics = 38,

    /// <summary>
    /// Disable perceptual optimizations.
    /// <para>0 = optimizations enabled (default), 1 = optimizations disabled.</para>
    /// </summary>
    DisablePerceptualHeuristics = 39,

    /// <summary>
    /// Enum value not to be used as an option. This value is added to force the C compiler to have the enum to take a known size.
    /// </summary>
    FillEnum = 65535,
}