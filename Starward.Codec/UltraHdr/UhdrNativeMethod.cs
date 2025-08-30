using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;


public static partial class UhdrNativeMethod
{

    private const string LibraryName = "uhdr.dll";


    // Encoder APIs

    /// <summary>
    /// Create a new encoder instance. The instance is initialized with default settings.
    /// To override the settings use uhdr_enc_set_*().
    /// Returns: null if error allocating memory, else a fresh opaque encoder handle.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrEncoderPtr uhdr_create_encoder();

    /// <summary>
    /// Release encoder instance. Frees all allocated storage associated with encoder instance.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial void uhdr_release_encoder(UhdrEncoderPtr enc);

    /// <summary>
    /// Add raw image descriptor to encoder context. Checks all fields for sanity and adds to internal list.
    /// Repeated calls replace old entry with current.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_raw_image(UhdrEncoderPtr enc, ref UhdrRawImage img, UhdrImageLabel intent);

    /// <summary>
    /// Add compressed image descriptor to encoder context. Checks all fields for sanity and adds to internal list.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_compressed_image(UhdrEncoderPtr enc, ref UhdrCompressedImage img, UhdrImageLabel intent);

    /// <summary>
    /// Add gain map image descriptor and gainmap metadata info to encoder context.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_gainmap_image(UhdrEncoderPtr enc, ref UhdrCompressedImage img, ref UhdrGainmapMetadata metadata);

    /// <summary>
    /// Set quality factor for compressing base image and/or gainmap image. Quality in range [0 - 100].
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_quality(UhdrEncoderPtr enc, int quality, UhdrImageLabel intent);

    /// <summary>
    /// Set Exif data that needs to be inserted in the output compressed stream.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_exif_data(UhdrEncoderPtr enc, ref UhdrMemoryBlock exif);

    /// <summary>
    /// Enable/Disable multi-channel gainmap. 0 - single-channel enabled, otherwise multi-channel.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_using_multi_channel_gainmap(UhdrEncoderPtr enc, int use_multi_channel_gainmap);

    /// <summary>
    /// Set gain map scaling factor. Factor in range (0, 128].
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_gainmap_scale_factor(UhdrEncoderPtr enc, int gainmap_scale_factor);

    /// <summary>
    /// Set encoding gamma of gainmap image. Any positive real number.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_gainmap_gamma(UhdrEncoderPtr enc, float gamma);

    /// <summary>
    /// Set min max content boost. Value MUST be in linear scale.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_min_max_content_boost(UhdrEncoderPtr enc, float min_boost, float max_boost);

    /// <summary>
    /// Set target display peak brightness in nits. Range [203, 10000].
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_target_display_peak_brightness(UhdrEncoderPtr enc, float nits);

    /// <summary>
    /// Set encoding preset. Choose between best performance or best quality.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_preset(UhdrEncoderPtr enc, UhdrEncodePreset preset);

    /// <summary>
    /// Set output image compression format.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enc_set_output_format(UhdrEncoderPtr enc, UhdrImageFormat media_type);

    /// <summary>
    /// Encode process call. Submits data for encoding.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_encode(UhdrEncoderPtr enc);

    /// <summary>
    /// Get encoded ultra hdr stream.
    /// Returns: null if encode process unsuccessful, image descriptor otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrCompressedImagePtr uhdr_get_encoded_stream(UhdrEncoderPtr enc);

    /// <summary>
    /// Reset encoder instance to default state.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial void uhdr_reset_encoder(UhdrEncoderPtr enc);

    // Decoder APIs

    /// <summary>
    /// Check if it is a valid ultrahdr image.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial int is_uhdr_image(IntPtr data, int size);

    /// <summary>
    /// Create a new decoder instance. The instance is initialized with default settings.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrDecoderPtr uhdr_create_decoder();

    /// <summary>
    /// Release decoder instance.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial void uhdr_release_decoder(UhdrDecoderPtr dec);

    /// <summary>
    /// Add compressed image descriptor to decoder context.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_dec_set_image(UhdrDecoderPtr dec, ref UhdrCompressedImage img);

    /// <summary>
    /// Set output image color format.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_dec_set_out_img_format(UhdrDecoderPtr dec, UhdrPixelFormat fmt);

    /// <summary>
    /// Set output image color transfer characteristics.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_dec_set_out_color_transfer(UhdrDecoderPtr dec, UhdrColorTransfer ct);

    /// <summary>
    /// Set output display's HDR capacity. Value >= 1.0f.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_dec_set_out_max_display_boost(UhdrDecoderPtr dec, float display_boost);

    /// <summary>
    /// Parse the bitstream registered with decoder context and make image info available.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_dec_probe(UhdrDecoderPtr dec);

    /// <summary>
    /// Get base image width.
    /// Returns: -1 if probe unsuccessful, width otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial int uhdr_dec_get_image_width(UhdrDecoderPtr dec);

    /// <summary>
    /// Get base image height.
    /// Returns: -1 if probe unsuccessful, height otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial int uhdr_dec_get_image_height(UhdrDecoderPtr dec);

    /// <summary>
    /// Get gainmap image width.
    /// Returns: -1 if probe unsuccessful, width otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial int uhdr_dec_get_gainmap_width(UhdrDecoderPtr dec);

    /// <summary>
    /// Get gainmap image height.
    /// Returns: -1 if probe unsuccessful, height otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial int uhdr_dec_get_gainmap_height(UhdrDecoderPtr dec);

    /// <summary>
    /// Get exif information.
    /// Returns: null if probe unsuccessful, memory block with exif data otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrMemoryBlockPtr uhdr_dec_get_exif(UhdrDecoderPtr dec);

    /// <summary>
    /// Get icc information.
    /// Returns: null if probe unsuccessful, memory block with icc data otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrMemoryBlockPtr uhdr_dec_get_icc(UhdrDecoderPtr dec);

    /// <summary>
    /// Get base image (compressed).
    /// Returns: null if probe unsuccessful, memory block with base image data otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrMemoryBlockPtr uhdr_dec_get_base_image(UhdrDecoderPtr dec);

    /// <summary>
    /// Get gain map image (compressed).
    /// Returns: null if probe unsuccessful, memory block with gainmap image data otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrMemoryBlockPtr uhdr_dec_get_gainmap_image(UhdrDecoderPtr dec);

    /// <summary>
    /// Get gain map metadata.
    /// Returns: null if probe unsuccessful, gainmap metadata descriptor otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrGainmapMetadataPtr uhdr_dec_get_gainmap_metadata(UhdrDecoderPtr dec);

    /// <summary>
    /// Decode process call. Submits data for decoding.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_decode(UhdrDecoderPtr dec);

    /// <summary>
    /// Get final rendition image.
    /// Returns: null if decode unsuccessful, raw image descriptor otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrRawImagePtr uhdr_get_decoded_image(UhdrDecoderPtr dec);

    /// <summary>
    /// Get gain map image.
    /// Returns: null if decode unsuccessful, raw image descriptor otherwise.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrRawImagePtr uhdr_get_decoded_gainmap_image(UhdrDecoderPtr dec);

    /// <summary>
    /// Reset decoder instance to default state.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial void uhdr_reset_decoder(UhdrDecoderPtr dec);

    // Common APIs

    /// <summary>
    /// Enable/Disable GPU acceleration. Certain operations may be offloaded to GPU.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_enable_gpu_acceleration(IntPtr codec, int enable);

    /// <summary>
    /// Add mirror effect.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_add_effect_mirror(IntPtr codec, UhdrMirrorDirection direction);

    /// <summary>
    /// Add rotate effect. degrees: 90, 180, 270.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_add_effect_rotate(IntPtr codec, int degrees);

    /// <summary>
    /// Add crop effect.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_add_effect_crop(IntPtr codec, int left, int right, int top, int bottom);

    /// <summary>
    /// Add resize effect.
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial UhdrErrorInfo uhdr_add_effect_resize(IntPtr codec, int width, int height);


}