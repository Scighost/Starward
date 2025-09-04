namespace Starward.Codec.UltraHdr;


/// <summary>
/// <para>The basic usage of uhdr encoder is as follows:</para>
/// <para>
/// The program registers input images to the encoder using:<br/>
///   - uhdr_enc_set_raw_image(ctxt, img, UHDR_HDR_IMG)<br/>
///   - uhdr_enc_set_raw_image(ctxt, img, UHDR_SDR_IMG)<br/>
/// The program overrides the default settings using uhdr_enc_set_*() functions.<br/>
///   - uhdr_enc_set_quality()<br/>
///   - uhdr_enc_set_exif_data()<br/>
///   - uhdr_enc_set_gainmap_scale_factor()<br/>
///   - uhdr_enc_set_using_multi_channel_gainmap()<br/>
///   - uhdr_enc_set_gainmap_gamma()<br/>
///   - uhdr_enc_set_min_max_content_boost()<br/>
///   - uhdr_enc_set_target_display_peak_brightness()<br/>
///   - uhdr_enc_set_preset()<br/>
///   - uhdr_enc_set_output_format()<br/>
/// The program calls uhdr_encode() to encode data. This initiates computing the gain map from
/// HDR and SDR intents, then compresses SDR and gain map at the configured quality.<br/>
/// On success, the program can access the encoded output with uhdr_get_encoded_stream().<br/>
/// The program finishes the encoding with uhdr_release_encoder().
/// </para>
///
/// <para>The library also allows setting HDR and/or SDR intent in compressed format:</para>
/// <para>
/// - uhdr_enc_set_compressed_image(ctxt, img, UHDR_HDR_IMG)<br/>
/// - uhdr_enc_set_compressed_image(ctxt, img, UHDR_SDR_IMG)<br/>
/// These are decoded to raw images, then processed through gain map computation and encoding.
/// The usage flow is:<br/>
/// - uhdr_create_encoder()<br/>
/// - uhdr_enc_set_compressed_image(ctxt, img, UHDR_HDR_IMG)<br/>
/// - uhdr_enc_set_compressed_image(ctxt, img, UHDR_SDR_IMG)<br/>
/// - uhdr_encode()<br/>
/// - uhdr_get_encoded_stream()<br/>
/// - uhdr_release_encoder()<br/>
/// If the SDR image format matches the output, it is used directly without re-encoding. Only the
/// gain map is encoded. Otherwise, transcoding is done.<br/>
/// </para>
///
/// <para>The library can also combine base and gain map images directly:</para>
/// <para>
/// - uhdr_enc_set_compressed_image(ctxt, img, UHDR_BASE_IMG)<br/>
/// - uhdr_enc_set_gainmap_image(ctxt, img, metadata)<br/>
/// Gain map computation is skipped; inputs are transcoded (if needed) and combined.
/// </para>
///
/// <para>It is possible to create a UltraHDR image solely from HDR intent:</para>
/// <para>
/// - uhdr_create_encoder()<br/>
/// - uhdr_enc_set_raw_image(ctxt, img, UHDR_HDR_IMG)<br/>
/// - (Optional) uhdr_enc_set_quality(), uhdr_enc_set_exif_data(), uhdr_enc_set_output_format(),
///   uhdr_enc_set_gainmap_scale_factor(), uhdr_enc_set_using_multi_channel_gainmap(),
///   uhdr_enc_set_gainmap_gamma(), uhdr_enc_set_min_max_content_boost(),
///   uhdr_enc_set_target_display_peak_brightness()<br/>
/// - uhdr_encode()<br/>
/// - uhdr_get_encoded_stream()<br/>
/// - uhdr_release_encoder()<br/>
/// The SDR rendition is created from HDR by tone-mapping, then both images go through gain map
/// computation and encoding.<br/>
/// </para>
///
/// <para>In all modes, Exif data is inserted if requested.</para>
/// </summary>

public class UhdrEncoder : UhdrCodec, IDisposable
{

    protected UhdrEncoderPtr _codecHandle => base._codecPtr;


    public UhdrEncoder()
    {
        base._codecPtr = UhdrNativeMethod.uhdr_create_encoder();
        if (_codecHandle == IntPtr.Zero)
        {
            throw new UhdrException(UhdrCodecError.Error, "Failed to create UHDR encoder.");
        }
    }


    /// <summary>
    /// Add raw image descriptor to encoder context. The function goes through all the fields of
    /// the image descriptor and checks for their sanity. If no anomalies are seen then the image is
    /// added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="rawImage">image descriptor</param>
    /// <param name="intent">HDR or SDR</param>
    public void SetRawImage(UhdrRawImage rawImage, UhdrImageLabel intent)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_raw_image(_codecHandle, ref rawImage, intent);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Add raw image descriptor to encoder context. The function goes through all the fields of
    /// the image descriptor and checks for their sanity. If no anomalies are seen then the image is
    /// added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="intent">HDR or SDR</param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <param name="bytes"></param>
    /// <param name="pixelFormat">Only RGB supported</param>
    /// <param name="colorGamut"></param>
    /// <param name="colorTransfer"></param>
    /// <param name="colorRange"></param>
    public unsafe void SetRawImage(UhdrImageLabel intent, uint width, uint height, ReadOnlySpan<byte> bytes, UhdrPixelFormat pixelFormat, UhdrColorGamut colorGamut, UhdrColorTransfer colorTransfer, UhdrColorRange colorRange)
    {
        fixed (byte* ptr = bytes)
        {
            UhdrRawImage rawImage = new UhdrRawImage
            {
                PixelFormat = pixelFormat,
                ColorGamut = colorGamut,
                ColorTransfer = colorTransfer,
                ColorRange = colorRange,
                Height = height,
                Width = width,
            };
            rawImage.Plane[0] = (IntPtr)ptr;
            rawImage.Stride[0] = width;
            SetRawImage(rawImage, intent);
        }
    }


    /// <summary>
    /// Add compressed image descriptor to encoder context. The function goes through all the
    /// fields of the image descriptor and checks for their sanity. If no anomalies are seen then the
    /// image is added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="compressedImage">image descriptor</param>
    /// <param name="intent">SDR / HDR / Base</param>
    public void SetCompressedImage(UhdrCompressedImage compressedImage, UhdrImageLabel intent)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_compressed_image(_codecHandle, ref compressedImage, intent);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Add compressed image descriptor to encoder context. The function goes through all the
    /// fields of the image descriptor and checks for their sanity. If no anomalies are seen then the
    /// image is added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="intent">SDR / HDR / Base</param>
    /// <param name="bytes">image bytes</param>
    /// <param name="colorGamut"></param>
    /// <param name="colorTransfer"></param>
    /// <param name="colorRange"></param>
    public unsafe void SetCompressedImage(UhdrImageLabel intent, ReadOnlySpan<byte> bytes, UhdrColorGamut colorGamut = UhdrColorGamut.Unspecified, UhdrColorTransfer colorTransfer = UhdrColorTransfer.Unspecified, UhdrColorRange colorRange = UhdrColorRange.Unspecified)
    {
        fixed (byte* p = bytes)
        {
            UhdrCompressedImage compressedImage = new UhdrCompressedImage
            {
                Data = (IntPtr)p,
                DataSize = (ulong)bytes.Length,
                Capacity = (ulong)bytes.Length,
                ColorGamut = colorGamut,
                ColorTransfer = colorTransfer,
                ColorRange = colorRange,
            };
            UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_compressed_image(_codecHandle, ref compressedImage, intent);
            errorInfo.ThrowIfError();
        }
    }


    /// <summary>
    /// Add gain map image descriptor and gainmap metadata info that was used to generate the
    /// aforth gainmap image to encoder context. The function internally goes through all the fields of
    /// the image descriptor and checks for their sanity. If no anomalies are seen then the image is
    /// added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="gainmapImage">gainmap image desciptor</param>
    /// <param name="gainmapMetadata"> gainmap metadata descriptor</param>
    public void SetGainmapImage(UhdrCompressedImage gainmapImage, UhdrGainmapMetadata gainmapMetadata)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_gainmap_image(_codecHandle, ref gainmapImage, ref gainmapMetadata);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Add gain map image descriptor and gainmap metadata info that was used to generate the
    /// aforth gainmap image to encoder context. The function internally goes through all the fields of
    /// the image descriptor and checks for their sanity. If no anomalies are seen then the image is
    /// added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="gainmapMetadata"> gainmap metadata descriptor</param>
    /// <param name="bytes">gainmap image bytes</param>
    /// <param name="colorGamut"></param>
    /// <param name="colorTransfer"></param>
    /// <param name="colorRange"></param>
    public unsafe void SetGainmapImage(UhdrGainmapMetadata gainmapMetadata, ReadOnlySpan<byte> bytes, UhdrColorGamut colorGamut = UhdrColorGamut.Unspecified, UhdrColorTransfer colorTransfer = UhdrColorTransfer.Unspecified, UhdrColorRange colorRange = UhdrColorRange.Unspecified)
    {
        fixed (byte* p = bytes)
        {
            UhdrCompressedImage compressedImage = new UhdrCompressedImage
            {
                Data = (IntPtr)p,
                DataSize = (ulong)bytes.Length,
                Capacity = (ulong)bytes.Length,
                ColorGamut = colorGamut,
                ColorTransfer = colorTransfer,
                ColorRange = colorRange,
            };
            UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_gainmap_image(_codecHandle, ref compressedImage, ref gainmapMetadata);
            errorInfo.ThrowIfError();
        }
    }


    /// <summary>
    /// Set quality factor for compressing base image and/or gainmap image. Default configured
    /// quality factor of base image and gainmap image are 95 and 95 respectively.
    /// </summary>
    /// <param name="quality">quality factor. Any integer in range [0 - 100].</param>
    /// <param name="intent">Base or GainMap</param>
    public void SetQuality(int quality, UhdrImageLabel intent)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_quality(_codecHandle, quality, intent);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set Exif data that needs to be inserted in the output compressed stream. This function
    /// does not generate or validate exif data on its own. It merely copies the supplied information
    /// into the bitstream.
    /// </summary>
    /// <param name="exif">exif data memory block</param>
    public unsafe void SetExifData(ReadOnlySpan<byte> exif)
    {
        fixed (byte* p = exif)
        {
            UhdrMemoryBlock exifBlock = new UhdrMemoryBlock
            {
                Data = (IntPtr)p,
                DataSize = (ulong)exif.Length,
                Capacity = (ulong)exif.Length
            };
            UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_exif_data(_codecHandle, ref exifBlock);
            errorInfo.ThrowIfError();
        }
    }


    /// <summary>
    /// Enable/Disable multi-channel gainmap. By default multi-channel gainmap is enabled.
    /// </summary>
    /// <param name="multiChannelGainmap">enable/disable multichannel gain map</param>
    public void SetMutliChannelGainmap(bool multiChannelGainmap)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_using_multi_channel_gainmap(_codecHandle, multiChannelGainmap ? 1 : 0);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set gain map scaling factor. The encoding process allows signalling a downscaled gainmap
    /// image instead of full resolution. This setting controls the factor by which the renditions are
    /// downscaled. For instance, gainmap_scale_factor = 2 implies gainmap_image_width =
    /// primary_image_width / 2 and gainmap image height = primary_image_height / 2.
    /// Default gain map scaling factor is 1.
    /// NOTE: This has no effect on base image rendition. Base image is signalled in full resolution always.
    /// </summary>
    /// <param name="scaleFactor">gain map scale factor. Any integer in range (0, 128]</param>
    public void SetGainmapScaleFactor(int scaleFactor)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_gainmap_scale_factor(_codecHandle, scaleFactor);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set encoding gamma of gainmap image. For multi-channel gainmap image, set gamma is used
    /// for gamma correction of all planes separately. Default gamma value is 1.0.
    /// </summary>
    /// <param name="gamma">gamma of gainmap image. Any positive real number.</param>
    public void SetGainmapGamma(float gamma)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_gainmap_gamma(_codecHandle, gamma);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set min max content boost. This configuration is treated as a recommendation by the
    /// library. It is entirely possible for the library to use a different set of values. Value MUST be
    /// in linear scale.
    /// </summary>
    /// <param name="minBoost">min content boost. Any positive real number.</param>
    /// <param name="maxBoost">max content boost. Any positive real number >= min_boost.</param>
    public void SetMinMaxContentBoost(float minBoost, float maxBoost)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_min_max_content_boost(_codecHandle, minBoost, maxBoost);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set target display peak brightness in nits. This is used for configuring #hdr_capacity_max
    /// of gainmap metadata. This value determines the weight by which the gain map coefficients are
    /// scaled during decode. If this is not configured, then default peak luminance of HDR intent's
    /// color transfer under test is used. For #UHDR_CT_HLG, this corresponds to 1000 nits and for
    /// #UHDR_CT_LINEAR and #UHDR_CT_PQ, this corresponds to 10000 nits.
    /// </summary>
    /// <param name="nits">target display peak brightness in nits. Any positive real number in range [203, 10000].</param>
    public void SetTargetDisplayPeakBrightness(float nits)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_target_display_peak_brightness(_codecHandle, nits);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set encoding preset. Tunes the encoder configurations for performance or quality. Default
    /// configuration is #UHDR_USAGE_BEST_QUALITY.
    /// </summary>
    /// <param name="preset">Realtime or BestQuality</param>
    public void SetPreset(UhdrEncodePreset preset)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_preset(_codecHandle, preset);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set output image compression format. Selects the compression format for encoding base
    /// image and gainmap image. Default configuration is #UHDR_CODEC_JPG
    /// </summary>
    /// <param name="format"></param>
    public void SetOutputFormat(UhdrImageFormat format)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_enc_set_output_format(_codecHandle, format);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Encode process call
    /// After initializing the encoder context, call to this function will submit data for encoding. If
    /// the call is successful, the encoded output is stored internally and is accessible via uhdr_get_encoded_stream().
    /// </summary>
    public void Encode()
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_encode(_codecHandle);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Get encoded ultra hdr image bytes
    /// </summary>
    /// <exception cref="UhdrException"></exception>
    public unsafe ReadOnlySpan<byte> GetEncodedBytes()
    {
        UhdrCompressedImage image = GetEncodedImage();
        return new ReadOnlySpan<byte>((void*)image.Data, (int)image.DataSize);
    }


    /// <summary>
    /// Get encoded ultra hdr image
    /// </summary>
    /// <exception cref="UhdrException"></exception>
    public unsafe UhdrCompressedImage GetEncodedImage()
    {
        UhdrCompressedImagePtr imagePtr = UhdrNativeMethod.uhdr_get_encoded_stream(_codecHandle);
        if (imagePtr.IsNull)
        {
            throw new UhdrException(UhdrCodecError.Error, "Failed to get encoded stream from UHDR encoder.");
        }
        return imagePtr.ToCompressedImage();
    }

}
