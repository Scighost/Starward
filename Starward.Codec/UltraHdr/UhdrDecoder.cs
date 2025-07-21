using System.Runtime.InteropServices;

namespace Starward.Codec.UltraHdr;


/// <summary>
/// <para>
/// After initializing the decoder context, call this function to submit data for decoding.  
/// If the call is successful, the decoded output is stored internally and can be accessed  
/// via uhdr_get_decoded_image().
/// </para>
///
/// <para>The basic usage of the uhdr decoder is as follows:</para>
/// <para>
/// - The program registers input images to the decoder using:<br/>
///   - uhdr_dec_set_image(ctxt, img)<br/>
/// - The program overrides the default settings using uhdr_dec_set_*() functions:<br/>
///   - uhdr_dec_set_out_img_format()<br/>
///   - uhdr_dec_set_out_color_transfer()<br/>
///   - uhdr_dec_set_out_max_display_boost()<br/>
///   - uhdr_enable_gpu_acceleration()<br/>
/// - The program calls uhdr_decode() to decode the UltraHDR stream. This starts decoding  
///   the base image and gain map image, which are combined to produce the final rendition image.<br/>
/// - The program accesses the decoded output using uhdr_get_decoded_image().<br/>
/// - The program finishes decoding with uhdr_release_decoder().
/// </para>
/// </summary>

public class UhdrDecoder : UhdrCodec
{

    public UhdrDecoder()
    {
        _codecHandle = UhdrNativeMethod.uhdr_create_decoder();
        if (_codecHandle == IntPtr.Zero)
        {
            throw new UhdrException(UhdrCodecError.Error, "Failed to create UHDR decoder.");
        }
    }


    /// <summary>
    /// Creates a new instance of the <see cref="UhdrDecoder"/> class and initializes it with the specified compressed image data.
    /// </summary>
    /// <param name="bytes">A read-only span of bytes representing the image data to be decoded. This span must contain valid image data.</param>
    /// <returns>A <see cref="UhdrDecoder"/> instance initialized with the provided image data.</returns>
    public static UhdrDecoder Create(ReadOnlySpan<byte> bytes)
    {
        var decoder = new UhdrDecoder();
        decoder.SetImage(bytes);
        decoder.Probe();
        return decoder;
    }


    /// <summary>
    /// Reset decoder instance.
    /// Clears all previous settings and resets to default state and ready for re-initialization and usage.
    /// </summary>
    public void ResetDecoder()
    {
        UhdrNativeMethod.uhdr_reset_decoder(_codecHandle);
    }


    /// <summary>
    /// Add compressed image descriptor to decoder context. The function goes through all the
    /// fields of the image descriptor and checks for their sanity. If no anomalies are seen then the
    /// image is added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="image">image descriptor</param>
    public void SetImage(UhdrCompressedImage image)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_dec_set_image(_codecHandle, ref image);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// /// <summary>
    /// Add compressed image descriptor to decoder context. The function goes through all the
    /// fields of the image descriptor and checks for their sanity. If no anomalies are seen then the
    /// image is added to internal list. Repeated calls to this function will replace the old entry with the current.
    /// </summary>
    /// <param name="bytes"></param>
    public unsafe void SetImage(ReadOnlySpan<byte> bytes)
    {
        fixed (byte* p = bytes)
        {
            UhdrCompressedImage image = new UhdrCompressedImage
            {
                Data = (nint)p,
                DataSize = (ulong)bytes.Length,
                Capacity = (ulong)bytes.Length,
                ColorGamut = UhdrColorGamut.Unspecified,
                ColorTransfer = UhdrColorTransfer.Unspecified,
                ColorRange = UhdrColorRange.Unspecified
            };
            UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_dec_set_image(_codecHandle, ref image);
            errorInfo.ThrowIfError();
        }
    }


    /// <summary>
    /// Set output image pixel format.
    /// Supported values are #UHDR_IMG_FMT_64bppRGBAHalfFloat, #UHDR_IMG_FMT_32bppRGBA1010102, #UHDR_IMG_FMT_32bppRGBA8888.
    /// </summary>
    /// <param name="format">output image pixel format</param>
    public void SetOutImagePixelFormat(UhdrPixelFormat format)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_dec_set_out_img_format(_codecHandle, format);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set output image color transfer characteristics. It should be noted that not all
    /// combinations of output color format and output transfer function are supported. #UHDR_CT_SRGB
    /// output color transfer shall be paired with #UHDR_IMG_FMT_32bppRGBA8888 only. #UHDR_CT_HLG,
    /// #UHDR_CT_PQ shall be paired with #UHDR_IMG_FMT_32bppRGBA1010102. #UHDR_CT_LINEAR shall be paired
    /// with #UHDR_IMG_FMT_64bppRGBAHalfFloat.
    /// </summary>
    /// <param name="colorTransfer">output color transfer</param>
    public void SetOutColorTransfer(UhdrColorTransfer colorTransfer)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_dec_set_out_color_transfer(_codecHandle, colorTransfer);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Set output display's HDR capacity. Value MUST be in linear scale. This value determines
    /// the weight by which the gain map coefficients are scaled. If no value is configured, no weight is
    /// applied to gainmap image.
    /// </summary>
    /// <param name="displayBoost">hdr capacity of target display. Any real number >= 1.0f</param>
    public void SetOutMaxDisplayBoost(float displayBoost)
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_dec_set_out_max_display_boost(_codecHandle, displayBoost);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// This function parses the bitstream that is registered with the decoder context and makes
    /// image information available to the client via uhdr_dec_get_() functions. It does not decompress
    /// the image. That is done by uhdr_decode().
    /// </summary>
    public void Probe()
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_dec_probe(_codecHandle);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Get base image width
    /// </summary>
    /// <returns>-1 if probe call is unsuccessful, base image width otherwise</returns>
    public int GetImageWidth()
    {
        return UhdrNativeMethod.uhdr_dec_get_image_width(_codecHandle);
    }


    /// <summary>
    /// Get base image height
    /// </summary>
    /// <returns>-1 if probe call is unsuccessful, base image height otherwise</returns>
    public int GetImageHeight()
    {
        return UhdrNativeMethod.uhdr_dec_get_image_height(_codecHandle);
    }


    /// <summary>
    /// Get gainmap image width
    /// </summary>
    /// <returns>-1 if probe call is unsuccessful, gain map image width otherwise</returns>
    public int GetGainmapWidth()
    {
        return UhdrNativeMethod.uhdr_dec_get_gainmap_width(_codecHandle);
    }


    /// <summary>
    /// Get gainmap image height
    /// </summary>
    /// <returns>-1 if probe call is unsuccessful, gain map image height otherwise</returns>
    public int GetGainmapHeight()
    {
        return UhdrNativeMethod.uhdr_dec_get_gainmap_height(_codecHandle);
    }


    /// <summary>
    /// Get exif information
    /// </summary>
    public ReadOnlySpan<byte> GetExifData()
    {
        nint memory_block_t = UhdrNativeMethod.uhdr_dec_get_exif(_codecHandle);
        if (memory_block_t == IntPtr.Zero)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        else
        {
            return Marshal.PtrToStructure<UhdrMemoryBlock>(memory_block_t).AsSpan();
        }
    }


    /// <summary>
    /// Get icc information
    /// </summary>
    /// <param name="index"></param>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetIccData()
    {
        nint memory_block_t = UhdrNativeMethod.uhdr_dec_get_icc(_codecHandle);
        if (memory_block_t == IntPtr.Zero)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        else
        {
            return Marshal.PtrToStructure<UhdrMemoryBlock>(memory_block_t).AsSpan();
        }
    }


    /// <summary>
    /// Get base image (compressed)
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetBaseImage()
    {
        nint memory_block_t = UhdrNativeMethod.uhdr_dec_get_base_image(_codecHandle);
        if (memory_block_t == IntPtr.Zero)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        else
        {
            return Marshal.PtrToStructure<UhdrMemoryBlock>(memory_block_t).AsSpan();
        }
    }


    /// <summary>
    /// Get gainmap image (compressed)
    /// </summary>
    /// <returns></returns>
    public ReadOnlySpan<byte> GetGainmapImage()
    {
        nint memory_block_t = UhdrNativeMethod.uhdr_dec_get_gainmap_image(_codecHandle);
        if (memory_block_t == IntPtr.Zero)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        else
        {
            return Marshal.PtrToStructure<UhdrMemoryBlock>(memory_block_t).AsSpan();
        }
    }


    /// <summary>
    /// Get gain map metadata
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UhdrException"></exception>
    public UhdrGainmapMetadata GetGainmapMetadata()
    {
        nint metadata_t = UhdrNativeMethod.uhdr_dec_get_gainmap_metadata(_codecHandle);
        if (metadata_t == IntPtr.Zero)
        {
            throw new UhdrException(UhdrCodecError.Error, "Failed to get gainmap metadata.");
        }
        else
        {
            return Marshal.PtrToStructure<UhdrGainmapMetadata>(metadata_t);
        }
    }


    /// <summary>
    /// Decode process call.
    /// After initializing the decoder context, call to this function will submit data for decoding. If
    /// the call is successful, the decoded output is stored internally and is accessible via uhdr_get_decoded_image().
    /// </summary>
    public void Decode()
    {
        UhdrErrorInfo errorInfo = UhdrNativeMethod.uhdr_decode(_codecHandle);
        errorInfo.ThrowIfError();
    }


    /// <summary>
    /// Get final rendition image
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UhdrException"></exception>
    public UhdrRawImage GetDecodedImage()
    {
        nint image_t = UhdrNativeMethod.uhdr_get_decoded_image(_codecHandle);
        if (image_t == IntPtr.Zero)
        {
            throw new UhdrException(UhdrCodecError.Error, "Failed to get decoded image.");
        }
        else
        {
            return Marshal.PtrToStructure<UhdrRawImage>(image_t);
        }
    }


    /// <summary>
    /// Get gain map image
    /// </summary>
    /// <returns></returns>
    /// <exception cref="UhdrException"></exception>
    public UhdrRawImage GetDecodedGainmapImage()
    {
        nint image_t = UhdrNativeMethod.uhdr_get_decoded_gainmap_image(_codecHandle);
        if (image_t == IntPtr.Zero)
        {
            throw new UhdrException(UhdrCodecError.Error, "Failed to get decoded gainmap image.");
        }
        else
        {
            return Marshal.PtrToStructure<UhdrRawImage>(image_t);
        }
    }


}