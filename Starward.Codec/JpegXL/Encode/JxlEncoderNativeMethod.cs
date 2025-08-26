using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.CodeStream;
using Starward.Codec.JpegXL.ParallelRunner;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// Encoding API for JPEG XL.
/// </summary>
public static partial class JxlEncoderNativeMethod
{


    private const string LibraryName = "jxl";


    /// <summary>
    /// Encoder library version.
    /// </summary>
    /// <returns>the encoder library version as an integer:
    /// MAJOR_VERSION * 1000000 + MINOR_VERSION * 1000 + PATCH_VERSION. For example,
    /// version 1.2.3 would return 1002003.</returns>
    [LibraryImport(LibraryName)]
    public static partial uint JxlEncoderVersion();


    /// <summary>
    /// Creates an instance of <see cref="JxlEncoderPtr"/> and initializes it.
    /// <para><paramref name="memoryManager"/> will be used for all the library dynamic allocations made
    /// from this instance. The parameter may be <see langword="default"/>, in which case the default
    /// allocator will be used. See jpegxl/memory_manager.h for details.</para>
    /// </summary>
    /// <param name="memoryManager">custom allocator function. It may be <see langword="default"/>. The memory manager will be copied internally.</param>
    /// <returns>
    /// <para><see langword="null"/> if the instance can not be allocated or initialized</para>
    /// <para>pointer to initialized <see cref="JxlEncoderPtr"/> otherwise</para>
    /// </returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderPtr JxlEncoderCreate(in JxlMemoryManager memoryManager);


    /// <summary>
    /// Creates an instance of <see cref="JxlEncoderPtr"/> and initializes it.
    /// <para><paramref name="memoryManager"/> will be used for all the library dynamic allocations made
    /// from this instance. The parameter may be <see langword="default"/>, in which case the default
    /// allocator will be used. See jpegxl/memory_manager.h for details.</para>
    /// </summary>
    /// <param name="memoryManager">custom allocator function. It may be <see langword="default"/>. The memory manager will be copied internally.</param>
    /// <returns>
    /// <para><see langword="null"/> if the instance can not be allocated or initialized</para>
    /// <para>pointer to initialized <see cref="JxlEncoderPtr"/> otherwise</para>
    /// </returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderPtr JxlEncoderCreate(JxlMemoryManagerPtr memoryManager = default);


    /// <summary>
    /// Re-initializes a <see cref="JxlEncoderPtr"/> instance, so it can be re-used for encoding
    /// another image. All state and settings are reset as if the object was
    /// newly created with <see cref="JxlEncoderCreate(JxlMemoryManagerPtr)"/>, but the memory manager is kept.
    /// </summary>
    /// <param name="enc">instance to be re-initialized.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderReset(JxlEncoderPtr enc);


    /// <summary>
    /// Deinitializes and frees a <see cref="JxlEncoderPtr"/> instance.
    /// </summary>
    /// <param name="enc">instance to be cleaned up and deallocated.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderDestroy(JxlEncoderPtr enc);


    /// <summary>
    /// Sets the color management system (CMS) that will be used for color conversion
    /// (if applicable) during encoding. May only be set before starting encoding. If
    /// left unset, the default CMS implementation will be used.
    /// </summary>
    /// <param name="enc">encoder object</param>
    /// <param name="cms">structure representing a CMS implementation. See <see cref="JxlCmsInterface"/> for more details.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderSetCms(JxlEncoderPtr enc, JxlCmsInterface cms);


    /// <summary>
    /// Set the parallel runner for multithreading. May only be set before starting encoding.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="parallel_runner">function pointer to runner for multithreading. It may
    /// be <see langword="null"/> to use the default, single-threaded, runner. A multithreaded
    /// runner should be set to reach fast performance.</param>
    /// <param name="parallel_runner_opaque">opaque pointer for parallel_runner, <see cref="JxlThreadParallelRunnerPtr"/> or <see cref="JxlResizableParallelRunnerPtr"/>.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the runner was set, <see cref="JxlEncoderStatus.Error"/> otherwise (the previous runner remains set).</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetParallelRunner(JxlEncoderPtr enc, IntPtr parallel_runner, IntPtr parallel_runner_opaque);


    /// <summary>
    /// Get the (last) error code in case <see cref="JxlEncoderStatus.Error"/> was returned.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <returns>the <see cref="JxlEncoderError"/> that caused the (last) <see cref="JxlEncoderStatus.Error"/> to be returned.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderError JxlEncoderGetError(JxlEncoderPtr enc);


    /// <summary>
    /// Encodes a JPEG XL file using the available bytes. <c>avail_out</c> indicates how
    /// many output bytes are available, and <c>next_out</c> points to the input bytes.
    /// <c>avail_out</c> will be decremented by the amount of bytes that have been
    /// processed by the encoder and <c>next_out</c> will be incremented by the same
    /// amount, so <c>next_out</c> will now point at the amount of <c>avail_out</c> unprocessed
    /// bytes.
    /// <para>
    /// The returned status indicates whether the encoder needs more output bytes.
    /// When the return value is not <see cref="JxlEncoderStatus.Error"/> or <see cref="JxlEncoderStatus.Success"/>, the
    /// encoding requires more <see cref="JxlEncoderProcessOutput"/> calls to continue.
    /// </para>
    /// <para>
    /// The caller must guarantee that <c>avail_out</c> >= 32 when calling
    /// <see cref="JxlEncoderProcessOutput"/>; otherwise, <see cref="JxlEncoderStatus.NeedMoreOutput"/> will
    /// be returned. It is guaranteed that, if <c>avail_out</c> >= 32, at least one byte of
    /// output will be written.
    /// </para>
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="next_out">pointer to next bytes to write to.</param>
    /// <param name="avail_out">amount of bytes available starting from <c>next_out</c>.</param>
    /// <returns>
    /// <see cref="JxlEncoderStatus.Success"/> when encoding finished and all events handled.
    /// <see cref="JxlEncoderStatus.Error"/> when encoding failed, e.g. invalid input.
    /// <see cref="JxlEncoderStatus.NeedMoreOutput"/> more output buffer is necessary.
    /// </returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderProcessOutput(JxlEncoderPtr enc, ref IntPtr next_out, ref nuint avail_out);



    /// <summary>
    /// Sets the frame information for this frame to the encoder. This includes
    /// animation information such as frame duration to store in the frame header.
    /// The frame header fields represent the frame as passed to the encoder, but not
    /// necessarily the exact values as they will be encoded file format: the encoder
    /// could change crop and blending options of a frame for more efficient encoding
    /// or introduce additional internal frames. Animation duration and time code
    /// information is not altered since those are immutable metadata of the frame.
    /// <para>
    /// It is not required to use this function, however if have_animation is set
    /// to true in the basic info, then this function should be used to set the
    /// time duration of this individual frame. By default individual frames have a
    /// time duration of 0, making them form a composite still. See <see cref="JxlFrameHeader"/> 
    /// for more information.
    /// </para>
    /// <para>
    /// This information is stored in the <see cref="JxlEncoderFrameSettingsPtr"/> and so is used
    /// for any frame encoded with these <see cref="JxlEncoderFrameSettingsPtr"/>. It is ok to
    /// change between <see cref="JxlEncoderAddImageFrame"/> calls, each added image frame
    /// will have the frame header that was set in the options at the time of calling
    /// <see cref="JxlEncoderAddImageFrame"/>.
    /// </para>
    /// <para>
    /// The is_last and name_length fields of the <see cref="JxlFrameHeader"/> are ignored,
    /// use <see cref="JxlEncoderCloseFrames"/> to indicate last frame, and <see cref="JxlEncoderSetFrameName"/> 
    /// to indicate the name and its length instead.
    /// Calling this function will clear any name that was previously set with 
    /// <see cref="JxlEncoderSetFrameName"/>.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="frame_header">frame header data to set. Object owned by the caller and
    /// does not need to be kept in memory, its information is copied internally.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetFrameHeader(JxlEncoderFrameSettingsPtr frame_settings, in JxlFrameHeader frame_header);



    /// <summary>
    /// Sets blend info of an extra channel. The blend info of extra channels is set
    /// separately from that of the color channels, the color channels are set with
    /// <see cref="JxlEncoderSetFrameHeader"/>.
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="index">index of the extra channel to use.</param>
    /// <param name="blend_info">blend info to set for the extra channel</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetExtraChannelBlendInfo(JxlEncoderFrameSettingsPtr frame_settings, nuint index, in JxlBlendInfo blend_info);


    /// <summary>
    /// Sets the name of the animation frame. This function is optional, frames are
    /// not required to have a name. This setting is a part of the frame header, and
    /// the same principles as for <see cref="JxlEncoderSetFrameHeader"/> apply. The
    /// name_length field of <see cref="JxlFrameHeader"/> is ignored by the encoder, this
    /// function determines the name length instead as the length in bytes of the C
    /// string.
    /// <para>
    /// The maximum possible name length is 1071 bytes (excluding terminating null
    /// character).
    /// </para>
    /// <para>
    /// Calling <see cref="JxlEncoderSetFrameHeader"/> clears any name that was
    /// previously set.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="frame_name">name of the next frame to be encoded, as a UTF-8 encoded C
    /// string (zero terminated). Owned by the caller, and copied internally.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetFrameName(JxlEncoderFrameSettingsPtr frame_settings, [MarshalAs(UnmanagedType.LPUTF8Str)] string frame_name);


    /// <summary>
    /// Sets the bit depth of the input buffer.
    /// <para>
    /// For float pixel formats, only the default <see cref="JxlBitDepthType.FromPixelFormat"/>
    /// setting is allowed, while for unsigned pixel formats,
    /// <see cref="JxlBitDepthType.FromCodestream"/> setting is also allowed. See the comment on
    /// <see cref="JxlEncoderAddImageFrame"/> for the effects of the bit depth setting.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="bit_depth">the bit depth setting of the pixel input</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetFrameBitDepth(JxlEncoderFrameSettingsPtr frame_settings, in JxlBitDepth bit_depth);


    /// <summary>
    /// Sets the buffer to read JPEG encoded bytes from for the next frame to encode.
    /// <para>
    /// If <see cref="JxlEncoderSetBasicInfo"/> has not yet been called, calling
    /// <see cref="JxlEncoderAddJPEGFrame"/> will implicitly call it with the parameters of
    /// the added JPEG frame.
    /// </para>
    /// <para>
    /// If <see cref="JxlEncoderSetColorEncoding"/> or <see cref="JxlEncoderSetICCProfile"/> has not
    /// yet been called, calling <see cref="JxlEncoderAddJPEGFrame"/> will implicitly call it
    /// with the parameters of the added JPEG frame.
    /// </para>
    /// <para>
    /// If the encoder is set to store JPEG reconstruction metadata using
    /// <see cref="JxlEncoderStoreJPEGMetadata"/> and a single JPEG frame is added, it will be
    /// possible to losslessly reconstruct the JPEG codestream.
    /// </para>
    /// <para>
    /// If this is the last frame, <see cref="JxlEncoderCloseInput"/> or
    /// <see cref="JxlEncoderCloseFrames"/> must be called before the next
    /// <see cref="JxlEncoderProcessOutput"/> call.
    /// </para>
    /// <para>
    /// Note, this can only be used to add JPEG frames for lossless compression. To
    /// encode with lossy compression, the JPEG must be decoded manually and a pixel
    /// buffer added using <see cref="JxlEncoderAddImageFrame"/>.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="buffer">bytes to read JPEG from. Owned by the caller and its contents
    /// are copied internally.</param>
    /// <param name="size">size of buffer in bytes.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderAddJPEGFrame(JxlEncoderFrameSettingsPtr frame_settings, IntPtr buffer, nuint size);



    /// <summary>
    /// Sets the buffer to read pixels from for the next image to encode. Must call
    /// <see cref="JxlEncoderSetBasicInfo"/> before <see cref="JxlEncoderAddImageFrame"/>.
    /// <para>
    /// Currently only some data types for pixel formats are supported:
    /// <list type="bullet">
    /// <item><description><see cref="JxlDataType.UInt8"/>, with range 0..255</description></item>
    /// <item><description><see cref="JxlDataType.UInt16"/>, with range 0..65535</description></item>
    /// <item><description><see cref="JxlDataType.Float16"/>, with nominal range 0..1</description></item>
    /// <item><description><see cref="JxlDataType.Float"/>, with nominal range 0..1</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: the sample data type in pixel_format is allowed to be different from
    /// what is described in the <see cref="JxlBasicInfo"/>. The type in pixel_format,
    /// together with an optional <see cref="JxlBitDepth"/> parameter set by
    /// <see cref="JxlEncoderSetFrameBitDepth"/> describes the format of the uncompressed pixel
    /// buffer. The bits_per_sample and exponent_bits_per_sample in the
    /// <see cref="JxlBasicInfo"/> describes what will actually be encoded in the JPEG XL
    /// codestream. For example, to encode a 12-bit image, you would set
    /// bits_per_sample to 12, while the input frame buffer can be in the following
    /// formats:
    /// <list type="bullet">
    /// <item><description>if pixel format is in <see cref="JxlDataType.UInt16"/> with default bit depth setting
    /// (i.e. <see cref="JxlBitDepthType.FromPixelFormat"/>), input sample values are
    /// rescaled to 16-bit, i.e. multiplied by 65535/4095;</description></item>
    /// <item><description>if pixel format is in <see cref="JxlDataType.UInt16"/> with
    /// <see cref="JxlBitDepthType.FromCodestream"/> bit depth setting, input sample values are
    /// provided unscaled;</description></item>
    /// <item><description>if pixel format is in <see cref="JxlDataType.Float"/>, input sample values are
    /// rescaled to 0..1, i.e.  multiplied by 1.f/4095.f. While it is allowed, it is
    /// obviously not recommended to use a pixel_format with lower precision than
    /// what is specified in the <see cref="JxlBasicInfo"/>.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// We support interleaved channels as described by the <see cref="JxlPixelFormat"/>:
    /// <list type="bullet">
    /// <item><description>single-channel data, e.g. grayscale</description></item>
    /// <item><description>single-channel + alpha</description></item>
    /// <item><description>trichromatic, e.g. RGB</description></item>
    /// <item><description>trichromatic + alpha</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Extra channels not handled here need to be set by
    /// <see cref="JxlEncoderSetExtraChannelBuffer"/>.
    /// If the image has alpha, and alpha is not passed here, it will implicitly be
    /// set to all-opaque (an alpha value of 1.0 everywhere).
    /// </para>
    /// <para>
    /// The pixels are assumed to be encoded in the original profile that is set with
    /// <see cref="JxlEncoderSetColorEncoding"/> or <see cref="JxlEncoderSetICCProfile"/>. If none of
    /// these functions were used, the pixels are assumed to be nonlinear sRGB for
    /// integer data types (<see cref="JxlDataType.UInt8"/>, <see cref="JxlDataType.UInt16"/>), and linear
    /// sRGB for floating point data types (<see cref="JxlDataType.Float16"/>,
    /// <see cref="JxlDataType.Float"/>).
    /// </para>
    /// <para>
    /// Sample values in floating-point pixel formats are allowed to be outside the
    /// nominal range, e.g. to represent out-of-sRGB-gamut colors in the
    /// uses_original_profile=false case. They are however not allowed to be NaN or
    /// +-infinity.
    /// </para>
    /// <para>
    /// If this is the last frame, <see cref="JxlEncoderCloseInput"/> or
    /// <see cref="JxlEncoderCloseFrames"/> must be called before the next
    /// <see cref="JxlEncoderProcessOutput"/> call.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="pixel_format">format for pixels. Object owned by the caller and its
    /// contents are copied internally.</param>
    /// <param name="buffer">buffer type to input the pixel data from. Owned by the caller
    /// and its contents are copied internally.</param>
    /// <param name="size">size of buffer in bytes. This size should match what is implied
    /// by the frame dimensions and the pixel format.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderAddImageFrame(JxlEncoderFrameSettingsPtr frame_settings, in JxlPixelFormat pixel_format, IntPtr buffer, nuint size);


    /// <summary>
    /// Sets the output processor for the encoder. This processor determines how the
    /// encoder will handle buffering, writing, seeking (if supported), and
    /// setting a finalized position during the encoding process.
    /// 
    /// This should not be used when using JxlEncoderProcessOutput.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="output_processor">the struct containing the callbacks for managing
    /// output.</param>
    /// <returns>JXL_ENC_SUCCESS on success, JXL_ENC_ERROR on error.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetOutputProcessor(JxlEncoderPtr enc, in JxlEncoderOutputProcessor output_processor);


    /// <summary>
    /// Flushes any buffered input in the encoder, ensuring that all available input
    /// data has been processed and written to the output.
    /// 
    /// This function can only be used after JxlEncoderSetOutputProcessor.
    /// Before making the last call to JxlEncoderFlushInput, users should call
    /// JxlEncoderCloseInput to signal the end of input data.
    /// 
    /// This should not be used when using JxlEncoderProcessOutput.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <returns>JXL_ENC_SUCCESS on success, JXL_ENC_ERROR on error.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderFlushInput(JxlEncoderPtr enc);





    /// <summary>
    /// Adds a frame to the encoder using a chunked input source.
    /// 
    /// This function gives a way to encode a frame by providing pixel data in a
    /// chunked or streaming manner, which can be especially useful when dealing with
    /// large images that may not fit entirely in memory or when trying to optimize
    /// memory usage. The input data is provided through callbacks defined in the
    /// JxlChunkedFrameInputSource struct. Once the frame data has been
    /// completely retrieved, this function will flush the input and close it if it
    /// is the last frame.
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="is_last_frame">indicates if this is the last frame.</param>
    /// <param name="chunked_frame_input">struct providing callback methods for retrieving
    /// pixel data in chunks.</param>
    /// <returns>Returns a status indicating the success or failure of adding the
    /// frame.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderAddChunkedFrame(JxlEncoderFrameSettingsPtr frame_settings, [MarshalAs(UnmanagedType.Bool)] bool is_last_frame, in JxlChunkedFrameInputSource chunked_frame_input);


    /// <summary>
    /// Sets the buffer to read pixels from for an extra channel at a given index.
    /// <para>
    /// The index must be smaller than the num_extra_channels given in the
    /// <see cref="JxlBasicInfo"/>. The <see cref="JxlEncoderSetExtraChannelInfo"/> must have been called for this
    /// index.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="pixel_format">format for pixels. Object owned by the caller and its
    /// contents are copied internally. The same restrictions as in
    /// <see cref="JxlEncoderAddImageFrame"/> apply. The number of channels in the pixel format must
    /// be 1.</param>
    /// <param name="buffer">buffer type to input the pixel data from. Owned by the caller
    /// and its contents are copied internally.</param>
    /// <param name="size">size of buffer in bytes. This size should match what is implied
    /// by the frame dimensions and the pixel format.</param>
    /// <param name="index">index of the extra channel to set.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetExtraChannelBuffer(JxlEncoderFrameSettingsPtr frame_settings, in JxlPixelFormat pixel_format, IntPtr buffer, nuint size, uint index);


    /// <summary>
    /// Adds a metadata box to the file format. <see cref="JxlEncoderProcessOutput"/> must be
    /// used to effectively write the box to the output. <see cref="JxlEncoderUseBoxes"/> must
    /// be enabled before using this function.
    /// <para>
    /// Boxes allow inserting application-specific data and metadata (Exif, XML/XMP,
    /// JUMBF and user defined boxes).
    /// </para>
    /// <para>
    /// The box format follows ISO BMFF and shares features and box types with other
    /// image and video formats, including the Exif, XML and JUMBF boxes. The box
    /// format for JPEG XL is specified in ISO/IEC 18181-2.
    /// </para>
    /// <para>
    /// Boxes in general don't contain other boxes inside, except a JUMBF superbox.
    /// Boxes follow each other sequentially and are byte-aligned. If the container
    /// format is used, the JXL stream consists of concatenated boxes.
    /// It is also possible to use a direct codestream without boxes, but in that
    /// case metadata cannot be added.
    /// </para>
    /// <para>
    /// Each box generally has the following byte structure in the file:
    /// <list type="bullet">
    /// <item><description>4 bytes: box size including box header (Big endian. If set to 0, an
    /// 8-byte 64-bit size follows instead).</description></item>
    /// <item><description>4 bytes: type, e.g. "JXL " for the signature box, "jxlc" for a codestream
    /// box.</description></item>
    /// <item><description>N bytes: box contents.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Only the box contents are provided to the contents argument of this function,
    /// the encoder encodes the size header itself. Most boxes are written
    /// automatically by the encoder as needed ("JXL ", "ftyp", "jxll", "jxlc",
    /// "jxlp", "jxli", "jbrd"), and this function only needs to be called to add
    /// optional metadata when encoding from pixels (using
    /// <see cref="JxlEncoderAddImageFrame"/>). When recompressing JPEG files (using
    /// <see cref="JxlEncoderAddJPEGFrame"/>), if the input JPEG contains EXIF, XMP or JUMBF
    /// metadata, the corresponding boxes are already added automatically.
    /// </para>
    /// <para>
    /// Box types are given by 4 characters. The following boxes can be added with
    /// this function:
    /// <list type="bullet">
    /// <item><description>"Exif": a box with EXIF metadata, can be added by libjxl users, or is
    /// automatically added when needed for JPEG reconstruction. The contents of
    /// this box must be prepended by a 4-byte tiff header offset, which may
    /// be 4 zero bytes in case the tiff header follows immediately.
    /// The EXIF metadata must be in sync with what is encoded in the JPEG XL
    /// codestream, specifically the image orientation. While this is not
    /// recommended in practice, in case of conflicting metadata, the JPEG XL
    /// codestream takes precedence.</description></item>
    /// <item><description>"xml ": a box with XML data, in particular XMP metadata, can be added by
    /// libjxl users, or is automatically added when needed for JPEG reconstruction</description></item>
    /// <item><description>"jumb": a JUMBF superbox, which can contain boxes with different types of
    /// metadata inside. This box type can be added by the encoder transparently,
    /// and other libraries to create and handle JUMBF content exist.</description></item>
    /// <item><description>Application-specific boxes. Their typename should not begin with "jxl" or
    /// "JXL" or conflict with other existing typenames, and they should be
    /// registered with MP4RA (mp4ra.org).</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// These boxes can be stored uncompressed or Brotli-compressed (using a "brob"
    /// box), depending on the compress_box parameter.
    /// </para>
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="type">the box type, e.g. "Exif" for EXIF metadata, "xml " for XMP or
    /// IPTC metadata, "jumb" for JUMBF metadata.</param>
    /// <param name="contents">the full contents of the box, for example EXIF
    /// data. ISO BMFF box header must not be included, only the contents. Owned by
    /// the caller and its contents are copied internally.</param>
    /// <param name="size">size of the box contents.</param>
    /// <param name="compress_box">Whether to compress this box as a "brob" box. Requires
    /// Brotli support.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error, such as
    /// when using this function without <see cref="JxlEncoderUseContainer"/>, or adding a box
    /// type that would result in an invalid file format.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderAddBox(JxlEncoderPtr enc, JxlBoxType type, IntPtr contents, nuint size, [MarshalAs(UnmanagedType.Bool)] bool compress_box);


    /// <summary>
    /// Indicates the intention to add metadata boxes. This allows
    /// JxlEncoderAddBox to be used. When using this function, then it is required
    /// to use JxlEncoderCloseBoxes at the end.
    /// 
    /// By default the encoder assumes no metadata boxes will be added.
    /// 
    /// This setting can only be set at the beginning, before encoding starts.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <returns>JXL_ENC_SUCCESS on success, JXL_ENC_ERROR on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderUseBoxes(JxlEncoderPtr enc);


    /// <summary>
    /// Declares that no further boxes will be added with JxlEncoderAddBox.
    /// This function must be called after the last box is added so the encoder knows
    /// the stream will be finished. It is not necessary to use this function if
    /// JxlEncoderUseBoxes is not used. Further frames may still be added.
    /// 
    /// Must be called between JxlEncoderAddBox of the last box
    /// and the next call to JxlEncoderProcessOutput, or
    /// JxlEncoderProcessOutput won't output the last box correctly.
    /// 
    /// NOTE: if you don't need to close frames and boxes at separate times, you can
    /// use JxlEncoderCloseInput instead to close both at once.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderCloseBoxes(JxlEncoderPtr enc);


    /// <summary>
    /// Declares that no frames will be added and JxlEncoderAddImageFrame and
    /// JxlEncoderAddJPEGFrame won't be called anymore. Further metadata boxes
    /// may still be added. This function or JxlEncoderCloseInput must be called
    /// after adding the last frame and the next call to
    /// JxlEncoderProcessOutput, or the frame won't be properly marked as last.
    /// 
    /// NOTE: if you don't need to close frames and boxes at separate times, you can
    /// use JxlEncoderCloseInput instead to close both at once.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderCloseFrames(JxlEncoderPtr enc);


    /// <summary>
    /// Closes any input to the encoder, equivalent to calling
    /// JxlEncoderCloseFrames as well as calling JxlEncoderCloseBoxes if needed.
    /// No further input of any kind may be given to the encoder, but further
    /// JxlEncoderProcessOutput calls should be done to create the final output.
    /// 
    /// The requirements of both JxlEncoderCloseFrames and
    /// JxlEncoderCloseBoxes apply to this function. Either this function or the
    /// other two must be called after the final frame and/or box, and the next
    /// JxlEncoderProcessOutput call, or the codestream won't be encoded
    /// correctly.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderCloseInput(JxlEncoderPtr enc);


    /// <summary>
    /// Sets the original color encoding of the image encoded by this encoder. This
    /// is an alternative to JxlEncoderSetICCProfile and only one of these two
    /// must be used. This one sets the color encoding as a JxlColorEncoding,
    /// while the other sets it as ICC binary data. Must be called after
    /// JxlEncoderSetBasicInfo.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="color">color encoding. Object owned by the caller and its contents are
    /// copied internally.</param>
    /// <returns>JXL_ENC_SUCCESS if the operation was successful,
    /// JXL_ENC_ERROR otherwise</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetColorEncoding(JxlEncoderPtr enc, in JxlColorEncoding color);


    /// <summary>
    /// Sets the original color encoding of the image encoded by this encoder as an
    /// ICC color profile. This is an alternative to JxlEncoderSetColorEncoding
    /// and only one of these two must be used. This one sets the color encoding as
    /// ICC binary data, while the other defines it as a JxlColorEncoding. Must
    /// be called after JxlEncoderSetBasicInfo.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="icc_profile">bytes of the original ICC profile</param>
    /// <param name="size">size of the icc_profile buffer in bytes</param>
    /// <returns>JXL_ENC_SUCCESS if the operation was successful,
    /// JXL_ENC_ERROR otherwise</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetICCProfile(JxlEncoderPtr enc, IntPtr icc_profile, nuint size);


    /// <summary>
    /// Initializes a JxlBasicInfo struct to default values.
    /// For forwards-compatibility, this function has to be called before values
    /// are assigned to the struct fields.
    /// The default values correspond to an 8-bit RGB image, no alpha or any
    /// other extra channels.
    /// </summary>
    /// <param name="info">global image metadata. Object owned by the caller.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderInitBasicInfo(ref JxlBasicInfo info);



    /// <summary>
    /// Initializes a JxlFrameHeader struct to default values.
    /// For forwards-compatibility, this function has to be called before values
    /// are assigned to the struct fields.
    /// The default values correspond to a frame with no animation duration and the
    /// 'replace' blend mode. After using this function, For animation duration must
    /// be set, for composite still blend settings must be set.
    /// </summary>
    /// <param name="frame_header">frame metadata. Object owned by the caller.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderInitFrameHeader(ref JxlFrameHeader frame_header);



    /// <summary>
    /// Initializes a JxlBlendInfo struct to default values.
    /// For forwards-compatibility, this function has to be called before values
    /// are assigned to the struct fields.
    /// </summary>
    /// <param name="blend_info">blending info. Object owned by the caller.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderInitBlendInfo(ref JxlBlendInfo blend_info);



    /// <summary>
    /// Sets the global metadata of the image encoded by this encoder.
    /// 
    /// If the JxlBasicInfo contains information of extra channels beyond an
    /// alpha channel, then JxlEncoderSetExtraChannelInfo must be called between
    /// JxlEncoderSetBasicInfo and JxlEncoderAddImageFrame. In order to
    /// indicate extra channels, the value of `info.num_extra_channels` should be set
    /// to the number of extra channels, also counting the alpha channel if present.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="info">global image metadata. Object owned by the caller and its
    /// contents are copied internally.</param>
    /// <returns>JXL_ENC_SUCCESS if the operation was successful,
    /// JXL_ENC_ERROR otherwise</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetBasicInfo(JxlEncoderPtr enc, in JxlBasicInfo info);



    /// <summary>
    /// Sets the upsampling method the decoder will use in case there are frames
    /// with JXL_ENC_FRAME_SETTING_RESAMPLING set. This is useful in combination
    /// with the JXL_ENC_FRAME_SETTING_ALREADY_DOWNSAMPLED option, to control
    /// the type of upsampling that will be used.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="factor">upsampling factor to configure (1, 2, 4 or 8; for 1 this
    /// function has no effect at all)</param>
    /// <param name="mode">upsampling mode to use for this upsampling:
    /// -1: default (good for photographic images, no signaling overhead)
    /// 0: nearest neighbor (good for pixel art)
    /// 1: 'pixel dots' (same as NN for 2x, diamond-shaped 'pixel dots' for 4x/8x)</param>
    /// <returns>JXL_ENC_SUCCESS if the operation was successful,
    /// JXL_ENC_ERROR otherwise</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetUpsamplingMode(JxlEncoderPtr enc, long factor, long mode);


    /// <summary>
    /// Initializes a JxlExtraChannelInfo struct to default values.
    /// For forwards-compatibility, this function has to be called before values
    /// are assigned to the struct fields.
    /// The default values correspond to an 8-bit channel of the provided type.
    /// </summary>
    /// <param name="type">type of the extra channel.</param>
    /// <param name="info">global extra channel metadata. Object owned by the caller and its
    /// contents are copied internally.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderInitExtraChannelInfo(JxlExtraChannelType type, ref JxlExtraChannelInfo info);


    /// <summary>
    /// Sets information for the extra channel at the given index. The index
    /// must be smaller than num_extra_channels in the associated JxlBasicInfo.
    /// </summary>
    /// <param name="enc">encoder object</param>
    /// <param name="index">index of the extra channel to set.</param>
    /// <param name="info">global extra channel metadata. Object owned by the caller and its
    /// contents are copied internally.</param>
    /// <returns>JXL_ENC_SUCCESS on success, JXL_ENC_ERROR on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetExtraChannelInfo(JxlEncoderPtr enc, nuint index, in JxlExtraChannelInfo info);



    /// <summary>
    /// Sets the name for the extra channel at the given index in UTF-8. The index
    /// must be smaller than the num_extra_channels in the associated
    /// <see cref="JxlBasicInfo"/>.
    /// </summary>
    /// <param name="enc">encoder object</param>
    /// <param name="index">index of the extra channel to set.</param>
    /// <param name="name">buffer with the name of the extra channel.</param>
    /// <param name="size">size of the name buffer in bytes, not counting the terminating character.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> on success, <see cref="JxlEncoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetExtraChannelName(JxlEncoderPtr enc, nuint index, IntPtr name, nuint size);


    /// <summary>
    /// Sets a frame-specific option of integer type to the encoder options.
    /// The <see cref="JxlEncoderFrameSettingId"/> argument determines which option is set.
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="option">ID of the option to set.</param>
    /// <param name="value">Integer value to set for this option.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful,
    /// <see cref="JxlEncoderStatus.Error"/> in case of an error, such as invalid or unknown option id, or
    /// invalid integer value for the given option. If an error is returned, the
    /// state of the <see cref="JxlEncoderFrameSettingsPtr"/> object is still valid and is the same as before
    /// this function was called.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderFrameSettingsSetOption(JxlEncoderFrameSettingsPtr frame_settings, JxlEncoderFrameSettingId option, long value);


    /// <summary>
    /// Sets a frame-specific option of float type to the encoder options.
    /// The <see cref="JxlEncoderFrameSettingId"/> argument determines which option is set.
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="option">ID of the option to set.</param>
    /// <param name="value">Float value to set for this option.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful,
    /// <see cref="JxlEncoderStatus.Error"/> in case of an error, such as invalid or unknown option id, or
    /// invalid integer value for the given option. If an error is returned, the
    /// state of the <see cref="JxlEncoderFrameSettingsPtr"/> object is still valid and is the same as before
    /// this function was called.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderFrameSettingsSetFloatOption(JxlEncoderFrameSettingsPtr frame_settings, JxlEncoderFrameSettingId option, float value);


    /// <summary>
    /// Forces the encoder to use the box-based container format (BMFF) even
    /// when not necessary.
    /// <para>
    /// When using <see cref="JxlEncoderUseBoxes"/>, <see cref="JxlEncoderStoreJPEGMetadata"/> or
    /// <see cref="JxlEncoderSetCodestreamLevel"/> with level 10, the encoder will automatically
    /// also use the container format, it is not necessary to use
    /// <see cref="JxlEncoderUseContainer"/> for those use cases.
    /// </para>
    /// <para>
    /// By default this setting is disabled.
    /// </para>
    /// <para>
    /// This setting can only be set at the beginning, before encoding starts.
    /// </para>
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="use_container">true if the encoder should always output the JPEG XL
    /// container format, false to only output it when necessary.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful, <see cref="JxlEncoderStatus.Error"/>
    /// otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderUseContainer(JxlEncoderPtr enc, [MarshalAs(UnmanagedType.Bool)] bool use_container);


    /// <summary>
    /// Configure the encoder to store JPEG reconstruction metadata in the JPEG XL
    /// container.
    /// <para>
    /// If this is set to true and a single JPEG frame is added, it will be
    /// possible to losslessly reconstruct the JPEG codestream.
    /// </para>
    /// <para>
    /// This setting can only be set at the beginning, before encoding starts.
    /// </para>
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="store_jpeg_metadata">true if the encoder should store JPEG metadata.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful,
    /// <see cref="JxlEncoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderStoreJPEGMetadata(JxlEncoderPtr enc, [MarshalAs(UnmanagedType.Bool)] bool store_jpeg_metadata);


    /// <summary>
    /// Sets the feature level of the JPEG XL codestream. Valid values are 5 and
    /// 10, or -1 (to choose automatically). Using the minimum required level, or
    /// level 5 in most cases, is recommended for compatibility with all decoders.
    /// <para>
    /// Level 5: for end-user image delivery, this level is the most widely
    /// supported level by image decoders and the recommended level to use unless a
    /// level 10 feature is absolutely necessary. Supports a maximum resolution
    /// 268435456 pixels total with a maximum width or height of 262144 pixels,
    /// maximum 16-bit color channel depth, maximum 120 frames per second for
    /// animation, maximum ICC color profile size of 4 MiB, it allows all color
    /// models and extra channel types except CMYK and the <see cref="JxlExtraChannelType.Black"/>
    /// extra channel, and a maximum of 4 extra channels in addition to the 3 color
    /// channels. It also sets boundaries to certain internally used coding tools.
    /// </para>
    /// <para>
    /// Level 10: this level removes or increases the bounds of most of the level
    /// 5 limitations, allows CMYK color and up to 32 bits per color channel, but
    /// may be less widely supported.
    /// </para>
    /// <para>
    /// The default value is -1. This means the encoder will automatically choose
    /// between level 5 and level 10 based on what information is inside the
    /// <see cref="JxlBasicInfo"/> structure. Do note that some level 10 features, particularly
    /// those used by animated JPEG XL codestreams, might require level 10, even
    /// though the <see cref="JxlBasicInfo"/> only suggests level 5. In this case, the level
    /// must be explicitly set to 10, otherwise the encoder will return an error.
    /// The encoder will restrict internal encoding choices to those compatible with
    /// the level setting.
    /// </para>
    /// <para>
    /// This setting can only be set at the beginning, before encoding starts.
    /// </para>
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="level">the level value to set, must be -1, 5, or 10.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful,
    /// <see cref="JxlEncoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetCodestreamLevel(JxlEncoderPtr enc, int level);



    /// <summary>
    /// Returns the codestream level required to support the currently configured
    /// settings and basic info. This function can only be used at the beginning,
    /// before encoding starts, but after setting basic info.
    /// <para>
    /// This does not support per-frame settings, only global configuration, such as
    /// the image dimensions, that are known at the time of writing the header of
    /// the JPEG XL file.
    /// </para>
    /// <para>
    /// If this returns 5, nothing needs to be done and the codestream can be
    /// compatible with any decoder. If this returns 10,
    /// <see cref="JxlEncoderSetCodestreamLevel"/> has to be used to set the codestream level to
    /// 10, or the encoder can be configured differently to allow using the more
    /// compatible level 5.
    /// </para>
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <returns>-1 if no level can support the configuration (e.g. image dimensions
    /// larger than even level 10 supports), 5 if level 5 is supported, 10 if setting
    /// the codestream level to 10 is required.</returns>
    [LibraryImport(LibraryName)]
    public static partial int JxlEncoderGetRequiredCodestreamLevel(JxlEncoderPtr enc);



    /// <summary>
    /// Enables lossless encoding.
    /// <para>
    /// This is not an option like the others on itself, but rather while enabled it
    /// overrides a set of existing options (such as distance, modular mode and
    /// color transform) that enables bit-for-bit lossless encoding.
    /// </para>
    /// <para>
    /// When disabled, those options are not overridden, but since those options
    /// could still have been manually set to a combination that operates losslessly,
    /// using this function with lossless set to <see langword="false"/> does not
    /// guarantee lossy encoding, though the default set of options is lossy.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="lossless">whether to override options for lossless mode</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful,
    /// <see cref="JxlEncoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetFrameLossless(JxlEncoderFrameSettingsPtr frame_settings, [MarshalAs(UnmanagedType.Bool)] bool lossless);


    /// <summary>
    /// Sets the distance level for lossy compression: target max butteraugli
    /// distance, lower = higher quality. Range: 0 .. 25.
    /// 0.0 = mathematically lossless (however, use <see cref="JxlEncoderSetFrameLossless"/>
    /// instead to use true lossless, as setting distance to 0 alone is not the only
    /// requirement). 1.0 = visually lossless. Recommended range: 0.5 .. 3.0. Default
    /// value: 1.0.
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="distance">the distance value to set.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful,
    /// <see cref="JxlEncoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetFrameDistance(JxlEncoderFrameSettingsPtr frame_settings, float distance);



    /// <summary>
    /// Sets the distance level for lossy compression of extra channels.
    /// The distance is as in <see cref="JxlEncoderSetFrameDistance"/> (lower = higher
    /// quality). If not set, or if set to the special value -1, the distance that
    /// was set with <see cref="JxlEncoderSetFrameDistance"/> will be used.
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="index">index of the extra channel to set a distance value for.</param>
    /// <param name="distance">the distance value to set.</param>
    /// <returns><see cref="JxlEncoderStatus.Success"/> if the operation was successful,
    /// <see cref="JxlEncoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderStatus JxlEncoderSetExtraChannelDistance(JxlEncoderFrameSettingsPtr frame_settings, nuint index, float distance);



    /// <summary>
    /// Maps JPEG-style quality factor to distance.
    /// <para>
    /// This function takes in input a JPEG-style quality factor <c>quality</c> and
    /// produces as output a <c>distance</c> value suitable to be used with
    /// <see cref="JxlEncoderSetFrameDistance"/> and <see cref="JxlEncoderSetExtraChannelDistance"/>.
    /// </para>
    /// <para>
    /// The <c>distance</c> value influences the level of compression, with lower values
    /// indicating higher quality:
    /// <list type="bullet">
    /// <item><description>0.0 implies lossless compression (however, note that calling
    /// <see cref="JxlEncoderSetFrameLossless"/> is required).</description></item>
    /// <item><description>1.0 represents a visually lossy compression, which is also the default
    /// setting.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// The <c>quality</c> parameter, ranging up to 100, is inversely related to
    /// 'distance':
    /// <list type="bullet">
    /// <item><description>A <c>quality</c> of 100.0 maps to a <c>distance</c> of 0.0 (lossless).</description></item>
    /// <item><description>A <c>quality</c> of 90.0 corresponds to a <c>distance</c> of 1.0.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Recommended Range:
    /// <list type="bullet">
    /// <item><description><c>distance</c>: 0.5 to 3.0.</description></item>
    /// <item><description>corresponding <c>quality</c>: approximately 96 to 68.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Allowed Range:
    /// <list type="bullet">
    /// <item><description><c>distance</c>: 0.0 to 25.0.</description></item>
    /// <item><description>corresponding <c>quality</c>: 100.0 to 0.0.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Note: the <c>quality</c> parameter has no consistent psychovisual meaning
    /// across different codecs and libraries. Using the mapping defined by
    /// <see cref="JxlEncoderDistanceFromQuality"/> will result in a visual quality roughly
    /// equivalent to what would be obtained with <c>libjpeg-turbo</c> with the same
    /// <c>quality</c> parameter, but that is by no means guaranteed; do not assume that
    /// the same quality value will result in similar file sizes and image quality
    /// across different codecs.
    /// </para>
    /// </summary>
    [LibraryImport(LibraryName)]
    public static partial float JxlEncoderDistanceFromQuality(float quality);



    /// <summary>
    /// Create a new set of encoder options, with all values initially copied from
    /// the source options, or set to default if source is <see langword="default"/>.
    /// <para>
    /// The returned pointer is an opaque struct tied to the encoder and it will be
    /// deallocated by the encoder when <see cref="JxlEncoderDestroy"/> is called. For
    /// functions taking both a <see cref="JxlEncoderPtr"/> and a <see cref="JxlEncoderFrameSettingsPtr"/>,
    /// only <see cref="JxlEncoderFrameSettingsPtr"/> created with this function for the same
    /// encoder instance can be used.
    /// </para>
    /// </summary>
    /// <param name="enc">encoder object.</param>
    /// <param name="source">source options to copy initial values from, or <see langword="default"/> to get
    /// defaults initialized to defaults.</param>
    /// <returns>the opaque struct pointer identifying a new set of encoder options.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlEncoderFrameSettingsPtr JxlEncoderFrameSettingsCreate(JxlEncoderPtr enc, JxlEncoderFrameSettingsPtr source = default);


    /// <summary>
    /// Sets a color encoding to be sRGB.
    /// </summary>
    /// <param name="color_encoding">color encoding instance.</param>
    /// <param name="is_gray">whether the color encoding should be gray scale or color.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlColorEncodingSetToSRGB(ref JxlColorEncoding color_encoding, [MarshalAs(UnmanagedType.Bool)] bool is_gray);



    /// <summary>
    /// Sets a color encoding to be linear sRGB.
    /// </summary>
    /// <param name="color_encoding">color encoding instance.</param>
    /// <param name="is_gray">whether the color encoding should be gray scale or color.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlColorEncodingSetToLinearSRGB(ref JxlColorEncoding color_encoding, [MarshalAs(UnmanagedType.Bool)] bool is_gray);



    /// <summary>
    /// Enables usage of expert options.
    /// 
    /// At the moment, the only expert option is setting an effort value of 11,
    /// which gives the best compression for pixel-lossless modes but is very slow.
    /// </summary>
    /// <param name="enc">encoder object.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlEncoderAllowExpertOptions(JxlEncoderPtr enc);


    /// <summary>
    /// Sets the given debug image callback that will be used by the encoder to
    /// output various debug images during encoding.
    /// <para>
    /// This only has any effect if the encoder was compiled with the appropriate
    /// debug build flags.
    /// </para>
    /// </summary>
    /// <param name="frame_settings">set of options and metadata for this frame. Also
    /// includes reference to the encoder object.</param>
    /// <param name="callback">used to return the debug image</param>
    /// <param name="opaque">user supplied parameter to the image callback</param>
    [LibraryImport(LibraryName)]
    public unsafe static partial void JxlEncoderSetDebugImageCallback(JxlEncoderFrameSettingsPtr frame_settings, JxlDebugImageCallback callback, IntPtr opaque);


}
