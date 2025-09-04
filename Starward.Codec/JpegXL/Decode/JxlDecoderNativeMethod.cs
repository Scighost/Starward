using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.CodeStream;
using Starward.Codec.JpegXL.Encode;
using Starward.Codec.JpegXL.ParallelRunner;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Decode;


/// <summary>
/// Decoding API for JPEG XL.
/// </summary>
public static partial class JxlDecoderNativeMethod
{

    private const string LibraryName = "jxl";


    /// <summary>
    /// Decoder library version.
    /// </summary>
    /// <returns>the decoder library version as an integer:
    /// MAJOR_VERSION * 1000000 + MINOR_VERSION * 1000 + PATCH_VERSION. For example,
    /// version 1.2.3 would return 1002003.</returns>
    [LibraryImport(LibraryName)]
    public static partial uint JxlDecoderVersion();


    /// <summary>
    /// JPEG XL signature identification.
    /// <para>Checks if the passed buffer contains a valid JPEG XL signature. The passed <paramref name="buffer"/> of size
    /// <paramref name="size"/> doesn't need to be a full image, only the beginning of the file.</para>
    /// </summary>
    /// <param name="buffer"></param>
    /// <param name="size"></param>
    /// <returns>a flag indicating if a JPEG XL signature was found and what type.
    /// <list type="bullet">
    /// <item><description><see cref="JxlSignature.NotEnoughBytes"/> if not enough bytes were passed to
    /// determine if a valid signature is there.</description></item>
    /// <item><description><see cref="JxlSignature.Invalid"/> if no valid signature found for JPEG XL decoding.</description></item>
    /// <item><description><see cref="JxlSignature.CodeStream"/> if a valid JPEG XL codestream signature was
    /// found.</description></item>
    /// <item><description><see cref="JxlSignature.Container"/> if a valid JPEG XL container signature was found.</description></item>
    /// </list>
    /// </returns>
    [LibraryImport(LibraryName)]
    public static partial JxlSignature JxlSignatureCheck(IntPtr buffer, nuint size);


    /// <summary>
    /// Creates an instance of <see cref="JxlDecoderPtr"/> and initializes it.
    /// <para><paramref name="memoryManager"/> will be used for all the library dynamic allocations made
    /// from this instance. The parameter may be <see langword="default"/>, in which case the default
    /// allocator will be used. See jxl/memory_manager.h for details.</para>
    /// </summary>
    /// <param name="memoryManager">custom allocator function. It may be <see langword="default"/>. The memory
    /// manager will be copied internally.</param>
    /// <returns>
    /// <para><c>NULL</c> if the instance can not be allocated or initialized</para>
    /// <para>pointer to initialized <see cref="JxlDecoderPtr"/> otherwise</para>
    /// </returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderPtr JxlDecoderCreate(JxlMemoryManagerPtr memoryManager = default);


    /// <summary>
    /// Re-initializes a <see cref="JxlDecoderPtr"/> instance, so it can be re-used for decoding
    /// another image. All state and settings are reset as if the object was
    /// newly created with <see cref="JxlDecoderCreate"/>, but the memory manager is kept.
    /// </summary>
    /// <param name="dec">instance to be re-initialized.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlDecoderReset(JxlDecoderPtr dec);


    /// <summary>
    /// Deinitializes and frees <see cref="JxlDecoderPtr"/> instance.
    /// </summary>
    /// <param name="dec">instance to be cleaned up and deallocated.</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlDecoderDestroy(JxlDecoderPtr dec);


    /// <summary>
    /// Rewinds decoder to the beginning. The same input must be given again from
    /// the beginning of the file and the decoder will emit events from the beginning
    /// again. When rewinding (as opposed to <see cref="JxlDecoderReset"/>), the decoder can
    /// keep state about the image, which it can use to skip to a requested frame
    /// more efficiently with <see cref="JxlDecoderSkipFrames"/>. Settings such as parallel
    /// runner or subscribed events are kept. After rewind, <see cref="JxlDecoderSubscribeEvents"/> 
    /// can be used again, and it is feasible to leave out
    /// events that were already handled before, such as <see cref="JxlDecoderStatus.BasicInfo"/>
    /// and <see cref="JxlDecoderStatus.ColorEncoding"/>, since they will provide the same information
    /// as before.
    /// The difference to <see cref="JxlDecoderReset"/> is that some state is kept, namely
    /// settings set by a call to
    /// <list type="bullet">
    /// <item><description><see cref="JxlDecoderSetCoalescing"/></description></item>
    /// <item><description><see cref="JxlDecoderSetDesiredIntensityTarget"/></description></item>
    /// <item><description><see cref="JxlDecoderSetDecompressBoxes"/></description></item>
    /// <item><description><see cref="JxlDecoderSetKeepOrientation"/></description></item>
    /// <item><description><see cref="JxlDecoderSetUnpremultiplyAlpha"/></description></item>
    /// <item><description><see cref="JxlDecoderSetParallelRunner"/></description></item>
    /// <item><description><see cref="JxlDecoderSetRenderSpotcolors"/></description></item>
    /// <item><description><see cref="JxlDecoderSubscribeEvents"/></description></item>
    /// </list>
    /// </summary>
    /// <param name="dec">decoder object</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlDecoderRewind(JxlDecoderPtr dec);


    /// <summary>
    /// Makes the decoder skip the next `amount` frames. It still needs to process
    /// the input, but will not output the frame events. It can be more efficient
    /// when skipping frames, and even more so when using this after <see cref="JxlDecoderRewind"/>. 
    /// If the decoder is already processing a frame (could
    /// have emitted <see cref="JxlDecoderStatus.Frame"/> but not yet <see cref="JxlDecoderStatus.FullImage"/>), it
    /// starts skipping from the next frame. If the amount is larger than the amount
    /// of frames remaining in the image, all remaining frames are skipped. Calling
    /// this function multiple times adds the amount to skip to the already existing
    /// amount.
    /// <para>A frame here is defined as a frame that without skipping emits events such
    /// as <see cref="JxlDecoderStatus.Frame"/> and <see cref="JxlDecoderStatus.FullImage"/>, frames that are internal
    /// to the file format but are not rendered as part of an animation, or are not
    /// the final still frame of a still image, are not counted.</para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="amount">the amount of frames to skip</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlDecoderSkipFrames(JxlDecoderPtr dec, nuint amount);


    /// <summary>
    /// Skips processing the current frame. Can be called after frame processing
    /// already started, signaled by a <see cref="JxlDecoderStatus.NeedImageOutBuffer"/> event,
    /// but before the corresponding <see cref="JxlDecoderStatus.FullImage"/> event. The next signaled
    /// event will be another <see cref="JxlDecoderStatus.Frame"/>, or <see cref="JxlDecoderStatus.Success"/> if there
    /// are no more frames. If pixel data is required from the already processed part
    /// of the frame, <see cref="JxlDecoderFlushImage"/> must be called before this.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if there is a frame to skip, and 
    /// <see cref="JxlDecoderStatus.Error"/> if the function was not called during frame processing.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSkipCurrentFrame(JxlDecoderPtr dec);


    /// <summary>
    /// Set the parallel runner for multithreading. May only be set before starting
    /// decoding.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="parallel_runner">function pointer to runner for multithreading. It may
    /// be <c>NULL</c> to use the default, single-threaded, runner. A multithreaded
    /// runner should be set to reach fast performance.</param>
    /// <param name="parallel_runner_opaque">opaque pointer for parallel_runner, <see cref="JxlThreadParallelRunnerPtr"/> or <see cref="JxlResizableParallelRunnerPtr"/>.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the runner was set, <see cref="JxlDecoderStatus.Error"/>
    /// otherwise (the previous runner remains set).</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetParallelRunner(JxlDecoderPtr dec, IntPtr parallel_runner, IntPtr parallel_runner_opaque);


    /// <summary>
    /// Returns a hint indicating how many more bytes the decoder is expected to
    /// need to make <see cref="JxlDecoderGetBasicInfo"/> available after the next <see cref="JxlDecoderProcessInput"/> call. 
    /// This is a suggested large enough value for
    /// the amount of bytes to provide in the next <see cref="JxlDecoderSetInput"/> call, but
    /// it is not guaranteed to be an upper bound nor a lower bound. This number does
    /// not include bytes that have already been released from the input. Can be used
    /// before the first <see cref="JxlDecoderProcessInput"/> call, and is correct the first
    /// time in most cases. If not, <see cref="JxlDecoderSizeHintBasicInfo"/> can be called
    /// again to get an updated hint.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns>
    /// <para>the size hint in bytes if the basic info is not yet fully decoded.</para>
    /// <para>0 when the basic info is already available.</para>
    /// </returns>
    [LibraryImport(LibraryName)]
    public static partial nuint JxlDecoderSizeHintBasicInfo(JxlDecoderPtr dec);


    /// <summary>
    /// Select for which informative events, i.e. <see cref="JxlDecoderStatus.BasicInfo"/>, etc., the
    /// decoder should return with a status. It is not required to subscribe to any
    /// events, data can still be requested from the decoder as soon as it available.
    /// By default, the decoder is subscribed to no events (events_wanted == 0), and
    /// the decoder will then only return when it cannot continue because it needs
    /// more input data or more output buffer. This function may only be be called
    /// before using <see cref="JxlDecoderProcessInput"/>.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="events_wanted">bitfield of desired events.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if no error, <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSubscribeEvents(JxlDecoderPtr dec, JxlDecoderStatus events_wanted);


    /// <summary>
    /// Enables or disables preserving of as-in-bitstream pixeldata
    /// orientation. Some images are encoded with an Orientation tag
    /// indicating that the decoder must perform a rotation and/or
    /// mirroring to the encoded image data.
    /// <list type="bullet">
    /// <item><description>If skip_reorientation is <see langword="false"/> (the default): the decoder
    /// will apply the transformation from the orientation setting, hence
    /// rendering the image according to its specified intent. When
    /// producing a <see cref="JxlBasicInfo"/>, the decoder will always set the
    /// orientation field to <see cref="JxlOrientation.Identity"/> (matching the returned
    /// pixel data) and also align xsize and ysize so that they correspond
    /// to the width and the height of the returned pixel data.</description></item>
    /// <item><description>If skip_reorientation is <see langword="true"/>: the decoder will skip
    /// applying the transformation from the orientation setting, returning
    /// the image in the as-in-bitstream pixeldata orientation.
    /// This may be faster to decode since the decoder doesn't have to apply the
    /// transformation, but can cause wrong display of the image if the
    /// orientation tag is not correctly taken into account by the user.</description></item>
    /// </list>
    /// By default, this option is disabled, and the returned pixel data is
    /// re-oriented according to the image's Orientation setting.
    /// <para>This function must be called at the beginning, before decoding is performed.</para>
    /// </summary>
    /// <seealso cref="JxlBasicInfo"/>
    /// <seealso cref="JxlOrientation"/>
    /// <param name="dec">decoder object</param>
    /// <param name="skip_reorientation"><see langword="true"/> to enable, <see langword="false"/> to disable.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if no error, <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetKeepOrientation(JxlDecoderPtr dec, [MarshalAs(UnmanagedType.Bool)] bool skip_reorientation);


    /// <summary>
    /// Enables or disables preserving of associated alpha channels. If
    /// unpremul_alpha is set to <see langword="false"/> then for associated alpha channel,
    /// the pixel data is returned with premultiplied colors. If it is set to 
    /// <see langword="true"/>, The colors will be unpremultiplied based on the alpha channel. This
    /// function has no effect if the image does not have an associated alpha
    /// channel.
    /// <para>By default, this option is disabled, and the returned pixel data "as is".</para>
    /// <para>This function must be called at the beginning, before decoding is performed.</para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="unpremul_alpha"><see langword="true"/> to enable, <see langword="false"/> to disable.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if no error, <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetUnpremultiplyAlpha(JxlDecoderPtr dec, [MarshalAs(UnmanagedType.Bool)] bool unpremul_alpha);


    /// <summary>
    /// Enables or disables rendering spot colors. By default, spot colors
    /// are rendered, which is OK for viewing the decoded image. If render_spotcolors
    /// is <see langword="false"/>, then spot colors are not rendered, and have to be
    /// retrieved separately using <see cref="JxlDecoderSetExtraChannelBuffer"/>. This is
    /// useful for e.g. printing applications.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="render_spotcolors"><see langword="true"/> to enable (default), <see langword="false"/> to disable.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if no error, <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetRenderSpotcolors(JxlDecoderPtr dec, [MarshalAs(UnmanagedType.Bool)] bool render_spotcolors);


    /// <summary>
    /// Enables or disables coalescing of zero-duration frames. By default, frames
    /// are returned with coalescing enabled, i.e. all frames have the image
    /// dimensions, and are blended if needed. When coalescing is disabled, frames
    /// can have arbitrary dimensions, a non-zero crop offset, and blending is not
    /// performed. For display, coalescing is recommended. For loading a multi-layer
    /// still image as separate layers (as opposed to the merged image), coalescing
    /// has to be disabled.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="coalescing"><see langword="true"/> to enable coalescing (default), <see langword="false"/> to
    /// disable it.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if no error, <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetCoalescing(JxlDecoderPtr dec, [MarshalAs(UnmanagedType.Bool)] bool coalescing);


    /// <summary>
    /// Decodes JPEG XL file using the available bytes. Requires input has been
    /// set with <see cref="JxlDecoderSetInput"/>. After <see cref="JxlDecoderProcessInput"/>, input
    /// can optionally be released with <see cref="JxlDecoderReleaseInput"/> and then set
    /// again to next bytes in the stream. <see cref="JxlDecoderReleaseInput"/> returns how
    /// many bytes are not yet processed, before a next call to 
    /// <see cref="JxlDecoderProcessInput"/> all unprocessed bytes must be provided again (the
    /// address need not match, but the contents must), and more bytes may be
    /// concatenated after the unprocessed bytes.
    /// <para>The returned status indicates whether the decoder needs more input bytes, or
    /// more output buffer for a certain type of output data. No matter what the
    /// returned status is (other than <see cref="JxlDecoderStatus.Error"/>), new information, such
    /// as <see cref="JxlDecoderGetBasicInfo"/>, may have become available after this call.
    /// When the return value is not <see cref="JxlDecoderStatus.Error"/> or <see cref="JxlDecoderStatus.Success"/>, the
    /// decoding requires more <see cref="JxlDecoderProcessInput"/> calls to continue.</para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns>
    /// <list type="bullet">
    /// <item><description><see cref="JxlDecoderStatus.Success"/> when decoding finished and all events handled.
    /// If you still have more unprocessed input data anyway, then you can still
    /// continue by using <see cref="JxlDecoderSetInput"/> and calling 
    /// <see cref="JxlDecoderProcessInput"/> again, similar to handling 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/>. <see cref="JxlDecoderStatus.Success"/> can occur instead of 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> when, for example, the input data ended right at
    /// the boundary of a box of the container format, all essential codestream
    /// boxes were already decoded, but extra metadata boxes are still present in
    /// the next data. <see cref="JxlDecoderProcessInput"/> cannot return success if all
    /// codestream boxes have not been seen yet.</description></item>
    /// <item><description><see cref="JxlDecoderStatus.Error"/> when decoding failed, e.g. invalid codestream.
    /// TODO(lode): document the input data mechanism</description></item>
    /// <item><description><see cref="JxlDecoderStatus.NeedMoreInput"/> when more input data is necessary.</description></item>
    /// <item><description><see cref="JxlDecoderStatus.BasicInfo"/> when basic info such as image dimensions is
    /// available and this informative event is subscribed to.</description></item>
    /// <item><description><see cref="JxlDecoderStatus.ColorEncoding"/> when color profile information is
    /// available and this informative event is subscribed to.</description></item>
    /// <item><description><see cref="JxlDecoderStatus.PreviewImage"/> when preview pixel information is
    /// available and output in the preview buffer.</description></item>
    /// <item><description><see cref="JxlDecoderStatus.FullImage"/> when all pixel information at highest detail
    /// is available and has been output in the pixel buffer.</description></item>
    /// </list>
    /// </returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderProcessInput(JxlDecoderPtr dec);


    /// <summary>
    /// Sets input data for <see cref="JxlDecoderProcessInput"/>. The data is owned by the
    /// caller and may be used by the decoder until <see cref="JxlDecoderReleaseInput"/> is
    /// called or the decoder is destroyed or reset so must be kept alive until then.
    /// Cannot be called if <see cref="JxlDecoderSetInput"/> was already called and 
    /// <see cref="JxlDecoderReleaseInput"/> was not yet called, and cannot be called after 
    /// <see cref="JxlDecoderCloseInput"/> indicating the end of input was called.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="data">pointer to next bytes to read from</param>
    /// <param name="size">amount of bytes available starting from data</param>
    /// <returns><see cref="JxlDecoderStatus.Error"/> if input was already set without releasing or 
    /// <see cref="JxlDecoderCloseInput"/> was already called, <see cref="JxlDecoderStatus.Success"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetInput(JxlDecoderPtr dec, IntPtr data, nuint size);



    /// <summary>
    /// Releases input which was provided with <see cref="JxlDecoderSetInput"/>. Between 
    /// <see cref="JxlDecoderProcessInput"/> and <see cref="JxlDecoderReleaseInput"/>, the user may not
    /// alter the data in the buffer. Calling <see cref="JxlDecoderReleaseInput"/> is required
    /// whenever any input is already set and new input needs to be added with 
    /// <see cref="JxlDecoderSetInput"/>, but is not required before <see cref="JxlDecoderDestroy"/> or 
    /// <see cref="JxlDecoderReset"/>. Calling <see cref="JxlDecoderReleaseInput"/> when no input is set is
    /// not an error and returns `0`.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns>The amount of bytes the decoder has not yet processed that are still
    /// remaining in the data set by <see cref="JxlDecoderSetInput"/>, or `0` if no input
    /// is set or <see cref="JxlDecoderReleaseInput"/> was already called. For a next call to
    /// <see cref="JxlDecoderProcessInput"/>, the buffer must start with these unprocessed
    /// bytes. From this value it is possible to infer the position of certain JPEG
    /// XL codestream elements (e.g. end of headers, frame start/end). See the
    /// documentation of individual values of <see cref="JxlDecoderStatus"/> for more
    /// information.</returns>
    [LibraryImport(LibraryName)]
    public static partial nuint JxlDecoderReleaseInput(JxlDecoderPtr dec);


    /// <summary>
    /// Marks the input as finished, indicates that no more <see cref="JxlDecoderSetInput"/>
    /// will be called. This function allows the decoder to determine correctly if it
    /// should return success, need more input or error in certain cases. For
    /// backwards compatibility with a previous version of the API, using this
    /// function is optional when not using the <see cref="JxlDecoderStatus.Box"/> event (the decoder
    /// is able to determine the end of the image frames without marking the end),
    /// but using this function is required when using <see cref="JxlDecoderStatus.Box"/> for getting
    /// metadata box contents. This function does not replace 
    /// <see cref="JxlDecoderReleaseInput"/>, that function should still be called if its return
    /// value is needed.
    /// <para><see cref="JxlDecoderCloseInput"/> should be called as soon as all known input bytes
    /// are set (e.g. at the beginning when not streaming but setting all input
    /// at once), before the final <see cref="JxlDecoderProcessInput"/> calls.</para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    [LibraryImport(LibraryName)]
    public static partial void JxlDecoderCloseInput(JxlDecoderPtr dec);


    /// <summary>
    /// Outputs the basic image information, such as image dimensions, bit depth and
    /// all other <see cref="JxlBasicInfo"/> fields, if available.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="info">struct to copy the information into, or <c>NULL</c> to only check
    /// whether the information is available through the return value.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the value is available, 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if not yet available, <see cref="JxlDecoderStatus.Error"/>
    /// in case of other error conditions.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetBasicInfo(JxlDecoderPtr dec, ref JxlBasicInfo info);


    /// <summary>
    /// Outputs information for extra channel at the given index. The index must be
    /// smaller than num_extra_channels in the associated <see cref="JxlBasicInfo"/>.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="index">index of the extra channel to query.</param>
    /// <param name="info">struct to copy the information into, or <c>NULL</c> to only check
    /// whether the information is available through the return value.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the value is available, 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if not yet available, <see cref="JxlDecoderStatus.Error"/>
    /// in case of other error conditions.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetExtraChannelInfo(JxlDecoderPtr dec, nuint index, ref JxlExtraChannelInfo info);


    /// <summary>
    /// Outputs name for extra channel at the given index in UTF-8. The index must be
    /// smaller than `num_extra_channels` in the associated <see cref="JxlBasicInfo"/>. The
    /// buffer for name must have at least `name_length + 1` bytes allocated, gotten
    /// from the associated <see cref="JxlExtraChannelInfo"/>.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="index">index of the extra channel to query.</param>
    /// <param name="name">buffer to copy the name into</param>
    /// <param name="size">size of the name buffer in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the value is available, 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if not yet available, <see cref="JxlDecoderStatus.Error"/>
    /// in case of other error conditions.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetExtraChannelName(JxlDecoderPtr dec, nuint index, IntPtr name, nuint size);


    /// <summary>
    /// Outputs the color profile as JPEG XL encoded structured data, if available.
    /// This is an alternative to an ICC Profile, which can represent a more limited
    /// amount of color spaces, but represents them exactly through enum values.
    /// <para>It is often possible to use <see cref="JxlDecoderGetColorAsICCProfile"/> as an
    /// alternative anyway. The following scenarios are possible:
    /// <list type="bullet">
    /// <item><description>The JPEG XL image has an attached ICC Profile, in that case, the encoded
    /// structured data is not available and this function will return an error
    /// status. <see cref="JxlDecoderGetColorAsICCProfile"/> should be called instead.</description></item>
    /// <item><description>The JPEG XL image has an encoded structured color profile, and it
    /// represents an RGB or grayscale color space. This function will return it.
    /// You can still use <see cref="JxlDecoderGetColorAsICCProfile"/> as well as an
    /// alternative if desired, though depending on which RGB color space is
    /// represented, the ICC profile may be a close approximation. It is also not
    /// always feasible to deduce from an ICC profile which named color space it
    /// exactly represents, if any, as it can represent any arbitrary space.
    /// HDR color spaces such as those using PQ and HLG are also potentially
    /// problematic, in that: while ICC profiles can encode a transfer function
    /// that happens to approximate those of PQ and HLG (HLG for only one given
    /// system gamma at a time, and necessitating a 3D LUT if gamma is to be
    /// different from `1`), they cannot (before ICCv4.4) semantically signal that
    /// this is the color space that they represent. Therefore, they will
    /// typically not actually be interpreted as representing an HDR color space.
    /// This is especially detrimental to PQ which will then be interpreted as if
    /// the maximum signal value represented SDR white instead of 10000 cd/m^2,
    /// meaning that the image will be displayed two orders of magnitude (5-7 EV)
    /// too dim.</description></item>
    /// <item><description>The JPEG XL image has an encoded structured color profile, and it
    /// indicates an unknown or xyb color space. In that case, 
    /// <see cref="JxlDecoderGetColorAsICCProfile"/> is not available.</description></item>
    /// </list>
    /// When rendering an image on a system where ICC-based color management is used,
    /// <see cref="JxlDecoderGetColorAsICCProfile"/> should generally be used first as it will
    /// return a ready-to-use profile (with the aforementioned caveat about HDR).
    /// When knowledge about the nominal color space is desired if available, 
    /// <see cref="JxlDecoderGetColorAsEncodedProfile"/> should be used first.</para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="target">whether to get the original color profile from the metadata
    /// or the color profile of the decoded pixels.</param>
    /// <param name="color_encoding">struct to copy the information into, or <c>NULL</c> to only
    /// check whether the information is available through the return value.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the data is available and returned, 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if not yet available, <see cref="JxlDecoderStatus.Error"/> in
    /// case the encoded structured color profile does not exist in the
    /// codestream.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetColorAsEncodedProfile(JxlDecoderPtr dec, JxlColorProfileTarget target, ref JxlColorEncoding color_encoding);


    /// <summary>
    /// Outputs the size in bytes of the ICC profile returned by 
    /// <see cref="JxlDecoderGetColorAsICCProfile"/>, if available, or indicates there is none
    /// available. In most cases, the image will have an ICC profile available, but
    /// if it does not, <see cref="JxlDecoderGetColorAsEncodedProfile"/> must be used instead.
    /// <para>
    /// See <see cref="JxlDecoderGetColorAsEncodedProfile"/> for more information. The ICC
    /// profile is either the exact ICC profile attached to the codestream metadata,
    /// or a close approximation generated from JPEG XL encoded structured data,
    /// depending of what is encoded in the codestream.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="target">whether to get the original color profile from the metadata
    /// or the color profile of the decoded pixels.</param>
    /// <param name="size">variable to output the size into, or <c>NULL</c> to only check the
    /// return status.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the ICC profile is available, 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if the decoder has not yet received enough
    /// input data to determine whether an ICC profile is available or what its
    /// size is, <see cref="JxlDecoderStatus.Error"/> in case the ICC profile is not available and
    /// cannot be generated.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetICCProfileSize(JxlDecoderPtr dec, JxlColorProfileTarget target, ref nuint size);


    /// <summary>
    /// Outputs ICC profile if available. The profile is only available if 
    /// <see cref="JxlDecoderGetICCProfileSize"/> returns success. The output buffer must have
    /// at least as many bytes as given by <see cref="JxlDecoderGetICCProfileSize"/>.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="target">whether to get the original color profile from the metadata
    /// or the color profile of the decoded pixels.</param>
    /// <param name="icc_profile">buffer to copy the ICC profile into</param>
    /// <param name="size">size of the icc_profile buffer in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the profile was successfully returned,
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if not yet available, 
    /// <see cref="JxlDecoderStatus.Error"/> if the profile doesn't exist or the output size is not
    /// large enough.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetColorAsICCProfile(JxlDecoderPtr dec, JxlColorProfileTarget target, IntPtr icc_profile, nuint size);


    /// <summary>
    /// Sets the desired output color profile of the decoded image by calling
    /// <see cref="JxlDecoderSetOutputColorProfile(JxlDecoderPtr, in JxlColorEncoding, nint, nuint)"/>, passing on <paramref name="color_encoding"/> and
    /// setting <c>icc_data</c> to <see langword="null"/>. See <see cref="JxlDecoderSetOutputColorProfile(JxlDecoderPtr, in JxlColorEncoding, nint, nuint)"/> for
    /// details.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="color_encoding">the default color encoding to set</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the preference was set successfully, 
    /// <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetPreferredColorProfile(JxlDecoderPtr dec, in JxlColorEncoding color_encoding);


    /// <summary>
    /// Requests that the decoder perform tone mapping to the peak display luminance
    /// passed as <paramref name="desired_intensity_target"/>, if appropriate.
    /// <para>
    /// This is provided for convenience and the exact tone mapping that is
    /// performed is not meant to be considered authoritative in any way. It may
    /// change from version to version.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="desired_intensity_target">the intended target peak luminance</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the preference was set successfully, 
    /// <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetDesiredIntensityTarget(JxlDecoderPtr dec, float desired_intensity_target);


    /// <summary>
    /// Sets the desired output color profile of the decoded image either from a
    /// color encoding or an ICC profile. Valid calls of this function have either <paramref name="color_encoding"/> or <paramref name="icc_data"/> set to <see langword="null"/> and <paramref name="icc_size"/> must be `0` if and
    /// only if <paramref name="icc_data"/> is <see langword="null"/>.
    /// <para>
    /// Depending on whether a color management system (CMS) has been set the
    /// behavior is as follows:
    /// </para>
    /// <para>
    /// If a color management system (CMS) has been set with <see cref="JxlDecoderSetCms"/>,
    /// and the CMS supports output to the desired color encoding or ICC profile,
    /// then it will provide the output in that color encoding or ICC profile. If the
    /// desired color encoding or the ICC is not supported, then an error will be
    /// returned.
    /// </para>
    /// <para>
    /// If no CMS has been set with <see cref="JxlDecoderSetCms"/>, there are two cases:
    /// </para>
    /// <para>
    /// (1) Calling this function with a color encoding will convert XYB images to
    /// the desired color encoding. In this case, if the requested color encoding has
    /// a narrower gamut, or the white points differ, then the resulting image can
    /// have significant color distortion. Non-XYB images will not be converted to
    /// the desired color space.
    /// </para>
    /// <para>
    /// (2) Calling this function with an ICC profile will result in an error.
    /// </para>
    /// <para>
    /// If called with an ICC profile (after a call to <see cref="JxlDecoderSetCms"/>), the
    /// ICC profile has to be a valid RGB or grayscale color profile.
    /// </para>
    /// <para>
    /// Can only be set after the <see cref="JxlDecoderStatus.ColorEncoding"/> event occurred and
    /// before any other event occurred, and should be used before getting
    /// <see cref="JxlColorProfileTarget.Data"/>.
    /// </para>
    /// <para>
    /// This function must not be called before <see cref="JxlDecoderSetCms"/>.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="color_encoding">the output color encoding</param>
    /// <param name="icc_data">bytes of the icc profile</param>
    /// <param name="icc_size">size of the icc profile in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the color profile was set successfully, 
    /// <see cref="JxlDecoderStatus.Error"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetOutputColorProfile(JxlDecoderPtr dec, in JxlColorEncoding color_encoding, IntPtr icc_data, nuint icc_size);


    /// <summary>
    /// Sets the color management system (CMS) that will be used for color
    /// conversion (if applicable) during decoding. May only be set before starting
    /// decoding and must not be called after <see cref="JxlDecoderSetOutputColorProfile(JxlDecoderPtr, in JxlColorEncoding, nint, nuint)"/>.
    /// <para>
    /// See <see cref="JxlDecoderSetOutputColorProfile(JxlDecoderPtr, in JxlColorEncoding, nint, nuint)"/> for how color conversions are done
    /// depending on whether or not a CMS has been set with <see cref="JxlDecoderSetCms"/>.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object.</param>
    /// <param name="cms">structure representing a CMS implementation. See <see cref="JxlCmsInterface"/> for more details.</param>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetCms(JxlDecoderPtr dec, JxlCmsInterface cms);


    /// <summary>
    /// Returns the minimum size in bytes of the preview image output pixel buffer
    /// for the given format. This is the buffer for <see cref="JxlDecoderSetPreviewOutBuffer"/>. Requires the preview header information is
    /// available in the decoder.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of pixels</param>
    /// <param name="size">output value, buffer size in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// information not available yet.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderPreviewOutBufferSize(JxlDecoderPtr dec, in JxlPixelFormat format, ref nuint size);


    /// <summary>
    /// Sets the buffer to write the low-resolution preview image
    /// to. The size of the buffer must be at least as large as given by <see cref="JxlDecoderPreviewOutBufferSize"/>. The buffer follows the format described
    /// by <see cref="JxlPixelFormat"/>. The preview image dimensions are given by the
    /// JxlPreviewHeader. The buffer is owned by the caller.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of pixels. Object owned by user and its contents are
    /// copied internally.</param>
    /// <param name="buffer">buffer type to output the pixel data to</param>
    /// <param name="size">size of buffer in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// size too small.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetPreviewOutBuffer(JxlDecoderPtr dec, in JxlPixelFormat format, IntPtr buffer, nuint size);


    /// <summary>
    /// Outputs the information from the frame, such as duration when have_animation.
    /// This function can be called when <see cref="JxlDecoderStatus.Frame"/> occurred for the current
    /// frame, even when have_animation in the JxlBasicInfo is <see langword="false"/>.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="header">struct to copy the information into, or <see langword="null"/> to only check
    /// whether the information is available through the return value.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the value is available, 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if not yet available, <see cref="JxlDecoderStatus.Error"/> in
    /// case of other error conditions.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetFrameHeader(JxlDecoderPtr dec, ref JxlFrameHeader header);


    /// <summary>
    /// Outputs name for the current frame. The buffer for name must have at least
    /// `name_length + 1` bytes allocated, gotten from the associated JxlFrameHeader.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="name">buffer to copy the name into</param>
    /// <param name="size">size of the name buffer in bytes, including zero termination
    /// character, so this must be at least <see cref="JxlFrameHeader.NameLength"/> + 1.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the value is available, 
    /// <see cref="JxlDecoderStatus.NeedMoreInput"/> if not yet available, <see cref="JxlDecoderStatus.Error"/> in
    /// case of other error conditions.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetFrameName(JxlDecoderPtr dec, IntPtr name, nuint size);


    /// <summary>
    /// Outputs the blend information for the current frame for a specific extra
    /// channel. This function can be called once the <see cref="JxlDecoderStatus.Frame"/> event occurred
    /// for the current frame, even if the `have_animation` field in the <see cref="JxlBasicInfo"/> is <see langword="false"/>. This information is only useful if coalescing
    /// is disabled; otherwise the decoder will have performed blending already.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="index">the index of the extra channel</param>
    /// <param name="blend_info">struct to copy the information into</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetExtraChannelBlendInfo(JxlDecoderPtr dec, nuint index, ref JxlBlendInfo blend_info);


    /// <summary>
    /// Returns the minimum size in bytes of the image output pixel buffer for the
    /// given format. This is the buffer for <see cref="JxlDecoderSetImageOutBuffer"/>.
    /// Requires that the basic image information is available in the decoder in the
    /// case of coalescing enabled (default). In case coalescing is disabled, this
    /// can only be called after the <see cref="JxlDecoderStatus.Frame"/> event occurs. In that case,
    /// it will return the size required to store the possibly cropped frame (which
    /// can be larger or smaller than the image dimensions).
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of the pixels.</param>
    /// <param name="size">output value, buffer size in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// information not available yet.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderImageOutBufferSize(JxlDecoderPtr dec, in JxlPixelFormat format, ref nuint size);


    /// <summary>
    /// Sets the buffer to write the full resolution image to. This can be set when
    /// the <see cref="JxlDecoderStatus.Frame"/> event occurs, must be set when the <see cref="JxlDecoderStatus.NeedImageOutBuffer"/> event occurs, and applies only for the
    /// current frame. The size of the buffer must be at least as large as given
    /// by <see cref="JxlDecoderImageOutBufferSize"/>. The buffer follows the format described
    /// by <see cref="JxlPixelFormat"/>. The buffer is owned by the caller.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of the pixels. Object owned by user and its contents
    /// are copied internally.</param>
    /// <param name="buffer">buffer type to output the pixel data to</param>
    /// <param name="size">size of buffer in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// size too small.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetImageOutBuffer(JxlDecoderPtr dec, in JxlPixelFormat format, IntPtr buffer, nuint size);


    /// <summary>
    /// Sets pixel output callback. This is an alternative to <see cref="JxlDecoderSetImageOutBuffer"/>. This can be set when the <see cref="JxlDecoderStatus.Frame"/>
    /// event occurs, must be set when the <see cref="JxlDecoderStatus.NeedImageOutBuffer"/> event
    /// occurs, and applies only for the current frame. Only one of <see cref="JxlDecoderSetImageOutBuffer"/> or <see cref="JxlDecoderSetImageOutCallback"/> may be used
    /// for the same frame, not both at the same time.
    /// <para>
    /// The callback will be called multiple times, to receive the image
    /// data in small chunks. The callback receives a horizontal stripe of pixel
    /// data, `1` pixel high, xsize pixels wide, called a scanline. The xsize here is
    /// not the same as the full image width, the scanline may be a partial section,
    /// and xsize may differ between calls. The user can then process and/or copy the
    /// partial scanline to an image buffer. The callback may be called
    /// simultaneously by different threads when using a threaded parallel runner, on
    /// different pixels.
    /// </para>
    /// <para>
    /// If <see cref="JxlDecoderFlushImage"/> is not used, then each pixel will be visited
    /// exactly once by the different callback calls, during processing with one or
    /// more <see cref="JxlDecoderProcessInput"/> calls. These pixels are decoded to full
    /// detail, they are not part of a lower resolution or lower quality progressive
    /// pass, but the final pass.
    /// </para>
    /// <para>
    /// If <see cref="JxlDecoderFlushImage"/> is used, then in addition each pixel will be
    /// visited zero or one times during the blocking <see cref="JxlDecoderFlushImage"/> call.
    /// Pixels visited as a result of <see cref="JxlDecoderFlushImage"/> may represent a lower
    /// resolution or lower quality intermediate progressive pass of the image. Any
    /// visited pixel will be of a quality at least as good or better than previous
    /// visits of this pixel. A pixel may be visited zero times if it cannot be
    /// decoded yet or if it was already decoded to full precision (this behavior is
    /// not guaranteed).
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of the pixels. Object owned by user; its contents are
    /// copied internally.</param>
    /// <param name="callback">the callback function receiving partial scanlines of pixel
    /// data.</param>
    /// <param name="opaque">optional user data, which will be passed on to the callback,
    /// may be NULL.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such
    /// as <see cref="JxlDecoderSetImageOutBuffer"/> already set.</returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial JxlDecoderStatus JxlDecoderSetImageOutCallback(JxlDecoderPtr dec, in JxlPixelFormat format, JxlImageOutCallback callback, IntPtr opaque);


    /// <summary>
    /// Similar to <see cref="JxlDecoderSetImageOutCallback"/> except that the callback is
    /// allowed an initialization phase during which it is informed of how many
    /// threads will call it concurrently, and those calls are further informed of
    /// which thread they are occurring in.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of the pixels. Object owned by user; its contents are
    /// copied internally.</param>
    /// <param name="init_callback">initialization callback.</param>
    /// <param name="run_callback">the callback function receiving partial scanlines of
    /// pixel data.</param>
    /// <param name="destroy_callback">clean-up callback invoked after all calls to @c
    /// run_callback. May be NULL if no clean-up is necessary.</param>
    /// <param name="init_opaque">optional user data passed to @c init_callback, may be NULL
    /// (unlike the return value from @c init_callback which may only be NULL if
    /// initialization failed).</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such
    /// as <see cref="JxlDecoderSetImageOutBuffer"/> having already been called.</returns>
    [LibraryImport(LibraryName)]
    public static unsafe partial JxlDecoderStatus JxlDecoderSetMultithreadedImageOutCallback(JxlDecoderPtr dec, in JxlPixelFormat format, JxlImageOutInitCallback init_callback, JxlImageOutRunCallback run_callback, JxlImageOutDestroyCallback destroy_callback, IntPtr init_opaque);


    /// <summary>
    /// Returns the minimum size in bytes of an extra channel pixel buffer for the
    /// given format. This is the buffer for <see cref="JxlDecoderSetExtraChannelBuffer"/>.
    /// Requires the basic image information is available in the decoder.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of the pixels. The num_channels value is ignored and is
    /// always treated to be `1`.</param>
    /// <param name="size">output value, buffer size in bytes</param>
    /// <param name="index">which extra channel to get, matching the index used in <see cref="JxlDecoderGetExtraChannelInfo"/>. Must be smaller than num_extra_channels in
    /// the associated <see cref="JxlBasicInfo"/>.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// information not available yet or invalid index.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderExtraChannelBufferSize(JxlDecoderPtr dec, in JxlPixelFormat format, ref nuint size, uint index);


    /// <summary>
    /// Sets the buffer to write an extra channel to. This can be set when
    /// the <see cref="JxlDecoderStatus.Frame"/> or <see cref="JxlDecoderStatus.NeedImageOutBuffer"/> event occurs,
    /// and applies only for the current frame. The size of the buffer must be at
    /// least as large as given by <see cref="JxlDecoderExtraChannelBufferSize"/>. The buffer
    /// follows the format described by <see cref="JxlPixelFormat"/>, but where num_channels
    /// is `1`. The buffer is owned by the caller. The amount of extra channels is
    /// given by the num_extra_channels field in the associated <see cref="JxlBasicInfo"/>,
    /// and the information of individual extra channels can be queried with <see cref="JxlDecoderGetExtraChannelInfo"/>. To get multiple extra channels, this function
    /// must be called multiple times, once for each wanted index. Not all images
    /// have extra channels. The alpha channel is an extra channel and can be gotten
    /// as part of the color channels when using an RGBA pixel buffer with <see cref="JxlDecoderSetImageOutBuffer"/>, but additionally also can be gotten
    /// separately as extra channel. The color channels themselves cannot be gotten
    /// this way.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="format">format of the pixels. Object owned by user and its contents
    /// are copied internally. The num_channels value is ignored and is always
    /// treated to be `1`.</param>
    /// <param name="buffer">buffer type to output the pixel data to</param>
    /// <param name="size">size of buffer in bytes</param>
    /// <param name="index">which extra channel to get, matching the index used in <see cref="JxlDecoderGetExtraChannelInfo"/>. Must be smaller than num_extra_channels in
    /// the associated <see cref="JxlBasicInfo"/>.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// size too small or invalid index.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetExtraChannelBuffer(JxlDecoderPtr dec, in JxlPixelFormat format, IntPtr buffer, nuint size, uint index);


    /// <summary>
    /// Sets output buffer for reconstructed JPEG codestream.
    /// <para>
    /// The data is owned by the caller and may be used by the decoder until <see cref="JxlDecoderReleaseJPEGBuffer"/> is called or the decoder is destroyed or
    /// reset so must be kept alive until then.
    /// </para>
    /// <para>
    /// If a JPEG buffer was set before and released with <see cref="JxlDecoderReleaseJPEGBuffer"/>, bytes that the decoder has already output
    /// should not be included, only the remaining bytes output must be set.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="data">pointer to next bytes to write to</param>
    /// <param name="size">amount of bytes available starting from data</param>
    /// <returns><see cref="JxlDecoderStatus.Error"/> if output buffer was already set and <see cref="JxlDecoderReleaseJPEGBuffer"/> was not called on it, <see cref="JxlDecoderStatus.Success"/>
    /// otherwise</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetJPEGBuffer(JxlDecoderPtr dec, IntPtr data, nuint size);


    /// <summary>
    /// Releases buffer which was provided with <see cref="JxlDecoderSetJPEGBuffer"/>.
    /// <para>
    /// Calling <see cref="JxlDecoderReleaseJPEGBuffer"/> is required whenever
    /// a buffer is already set and a new buffer needs to be added with <see cref="JxlDecoderSetJPEGBuffer"/>, but is not required before <see cref="JxlDecoderDestroy"/> or <see cref="JxlDecoderReset"/>.
    /// </para>
    /// <para>
    /// Calling <see cref="JxlDecoderReleaseJPEGBuffer"/> when no buffer is set is
    /// not an error and returns `0`.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns>the amount of bytes the decoder has not yet written to of the data
    /// set by <see cref="JxlDecoderSetJPEGBuffer"/>, or `0` if no buffer is set or <see cref="JxlDecoderReleaseJPEGBuffer"/> was already called.</returns>
    [LibraryImport(LibraryName)]
    public static partial nuint JxlDecoderReleaseJPEGBuffer(JxlDecoderPtr dec);


    /// <summary>
    /// Sets output buffer for box output codestream.
    /// <para>
    /// The data is owned by the caller and may be used by the decoder until <see cref="JxlDecoderReleaseBoxBuffer"/> is called or the decoder is destroyed or
    /// reset so must be kept alive until then.
    /// </para>
    /// <para>
    /// If for the current box a box buffer was set before and released with <see cref="JxlDecoderReleaseBoxBuffer"/>, bytes that the decoder has already output
    /// should not be included, only the remaining bytes output must be set.
    /// </para>
    /// <para>
    /// The <see cref="JxlDecoderReleaseBoxBuffer"/> must be used at the next <see cref="JxlDecoderStatus.Box"/>
    /// event or final <see cref="JxlDecoderStatus.Success"/> event to compute the size of the output
    /// box bytes.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="data">pointer to next bytes to write to</param>
    /// <param name="size">amount of bytes available starting from data</param>
    /// <returns><see cref="JxlDecoderStatus.Error"/> if output buffer was already set and <see cref="JxlDecoderReleaseBoxBuffer"/> was not called on it, <see cref="JxlDecoderStatus.Success"/>
    /// otherwise</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetBoxBuffer(JxlDecoderPtr dec, IntPtr data, nuint size);


    /// <summary>
    /// Releases buffer which was provided with <see cref="JxlDecoderSetBoxBuffer"/>.
    /// <para>
    /// Calling <see cref="JxlDecoderReleaseBoxBuffer"/> is required whenever
    /// a buffer is already set and a new buffer needs to be added with <see cref="JxlDecoderSetBoxBuffer"/>, but is not required before <see cref="JxlDecoderDestroy"/> or <see cref="JxlDecoderReset"/>.
    /// </para>
    /// <para>
    /// Calling <see cref="JxlDecoderReleaseBoxBuffer"/> when no buffer is set is
    /// not an error and returns `0`.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns>the amount of bytes the decoder has not yet written to of the data
    /// set by <see cref="JxlDecoderSetBoxBuffer"/>, or `0` if no buffer is set or <see cref="JxlDecoderReleaseBoxBuffer"/> was already called.</returns>
    [LibraryImport(LibraryName)]
    public static partial nuint JxlDecoderReleaseBoxBuffer(JxlDecoderPtr dec);


    /// <summary>
    /// Configures whether to get boxes in raw mode or in decompressed mode. In raw
    /// mode, boxes are output as their bytes appear in the container file, which may
    /// be decompressed, or compressed if their type is "brob". In decompressed mode,
    /// "brob" boxes are decompressed with Brotli before outputting them. The size of
    /// the decompressed stream is not known before the decompression has already
    /// finished.
    /// <para>
    /// The default mode is raw. This setting can only be changed before decoding, or
    /// directly after a <see cref="JxlDecoderStatus.Box"/> event, and is remembered until the decoder
    /// is reset or destroyed.
    /// </para>
    /// <para>
    /// Enabling decompressed mode requires Brotli support from the library.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="decompress">JXL_TRUE to transparently decompress, JXL_FALSE
    /// to get boxes in raw mode.</param>
    /// <returns><see cref="JxlDecoderStatus.Error"/> if decompressed mode is set and Brotli is not
    /// available, <see cref="JxlDecoderStatus.Success"/> otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetDecompressBoxes(JxlDecoderPtr dec, [MarshalAs(UnmanagedType.Bool)] bool decompress);


    /// <summary>
    /// Outputs the type of the current box, after a <see cref="JxlDecoderStatus.Box"/> event occurred,
    /// as `4` characters without null termination character. In case of a compressed
    /// "brob" box, this will return "brob" if the decompressed argument is
    /// <see langword="false"/>, or the underlying box type if the decompressed argument is
    /// <see langword="true"/>.
    /// <para>
    /// The following box types are currently described in ISO/IEC 18181-2:
    /// <list type="bullet">
    /// <item><description>"Exif": a box with EXIF metadata.  Starts with a 4-byte tiff header offset
    /// (big-endian uint32) that indicates the start of the actual EXIF data
    /// (which starts with a tiff header). Usually the offset will be zero and the
    /// EXIF data starts immediately after the offset field. The Exif orientation
    /// should be ignored by applications; the JPEG XL codestream orientation
    /// takes precedence and libjxl will by default apply the correct orientation
    /// automatically (see <see cref="JxlDecoderSetKeepOrientation"/>).</description></item>
    /// <item><description>"xml ": a box with XML data, in particular XMP metadata.</description></item>
    /// <item><description>"jumb": a JUMBF superbox (JPEG Universal Metadata Box Format, ISO/IEC 19566-5).</description></item>
    /// <item><description>"JXL ": mandatory signature box, must come first, `12` bytes long
    /// including the box header</description></item>
    /// <item><description>"ftyp": a second mandatory signature box, must come second, `20` bytes
    /// long including the box header</description></item>
    /// <item><description>"jxll": a JXL level box. This indicates if the codestream is level `5` or
    /// level `10` compatible. If not present, it is level `5`. Level `10` allows
    /// more features such as very high image resolution and bit-depths above `16`
    /// bits per channel. Added automatically by the encoder when
    /// JxlEncoderSetCodestreamLevel is used</description></item>
    /// <item><description>"jxlc": a box with the image codestream, in case the codestream is not
    /// split across multiple boxes. The codestream contains the JPEG XL image
    /// itself, including the basic info such as image dimensions, ICC color
    /// profile, and all the pixel data of all the image frames.</description></item>
    /// <item><description>"jxlp": a codestream box in case it is split across multiple boxes.
    /// The contents are the same as in case of a jxlc box, when concatenated.</description></item>
    /// <item><description>"brob": a Brotli-compressed box, which otherwise represents an existing
    /// type of box such as Exif or "xml ". When <see cref="JxlDecoderSetDecompressBoxes"/>
    /// is set to <see langword="true"/>, these boxes will be transparently decompressed by the
    /// decoder.</description></item>
    /// <item><description>"jxli": frame index box, can list the keyframes in case of a JPEG XL
    /// animation allowing the decoder to jump to individual frames more
    /// efficiently.</description></item>
    /// <item><description>"jbrd": JPEG reconstruction box, contains the information required to
    /// byte-for-byte losslessly reconstruct a JPEG-1 image. The JPEG DCT
    /// coefficients (pixel content) themselves as well as the ICC profile are
    /// encoded in the JXL codestream (jxlc or jxlp) itself. EXIF, XMP and JUMBF
    /// metadata is encoded in the corresponding boxes. The jbrd box itself
    /// contains information such as the remaining app markers of the JPEG-1 file
    /// and everything else required to fit the information together into the
    /// exact original JPEG file.</description></item>
    /// </list>
    /// Other application-specific boxes can exist. Their typename should not begin
    /// with "jxl" or "JXL" or conflict with other existing typenames.
    /// </para>
    /// <para>
    /// The signature, jxl* and jbrd boxes are processed by the decoder and would
    /// typically be ignored by applications. The typical way to use this function is
    /// to check if an encountered box contains metadata that the application is
    /// interested in (e.g. EXIF or XMP metadata), in order to conditionally set a
    /// box buffer.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="type">buffer to copy the type into</param>
    /// <param name="decompressed">which box type to get: <see langword="false"/> to get the raw box type,
    /// which can be "brob", <see langword="true"/>, get the underlying box type.</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if the value is available, <see cref="JxlDecoderStatus.Error"/> if
    /// not, for example the JPEG XL file does not use the container format.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetBoxType(JxlDecoderPtr dec, ref JxlBoxType type, [MarshalAs(UnmanagedType.Bool)] bool decompressed);


    /// <summary>
    /// Returns the size of a box as it appears in the container file, after the <see cref="JxlDecoderStatus.Box"/> event. This includes all the box headers.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="size">raw size of the box in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Error"/> if no box size is available, <see cref="JxlDecoderStatus.Success"/>
    /// otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetBoxSizeRaw(JxlDecoderPtr dec, ref ulong size);


    /// <summary>
    /// Returns the size of the contents of a box, after the <see cref="JxlDecoderStatus.Box"/> event. This does not include any of the headers of the box. For
    /// compressed "brob" boxes, this is the size of the compressed content. Even
    /// when <see cref="JxlDecoderSetDecompressBoxes"/> is enabled, the return value of
    /// function does not change, and the decompressed size is not known before it
    /// has already been decompressed and output.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="size">size of the payload of the box in bytes</param>
    /// <returns><see cref="JxlDecoderStatus.Error"/> if no box size is available, <see cref="JxlDecoderStatus.Success"/>
    /// otherwise.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderGetBoxSizeContents(JxlDecoderPtr dec, ref ulong size);


    /// <summary>
    /// Configures at which progressive steps in frame decoding these <see cref="JxlDecoderStatus.FrameProgression"/> event occurs. The default value for the level
    /// of detail if this function is never called is `kDC`.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="detail">at which level of detail to trigger <see cref="JxlDecoderStatus.FrameProgression"/></param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// an invalid value for the progressive detail.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetProgressiveDetail(JxlDecoderPtr dec, JxlProgressiveDetail detail);


    /// <summary>
    /// Returns the intended downsampling ratio for the progressive frame produced
    /// by <see cref="JxlDecoderFlushImage"/> after the latest <see cref="JxlDecoderStatus.FrameProgression"/>
    /// event.
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns>The intended downsampling ratio, can be `1`, `2`, `4` or `8`.</returns>
    [LibraryImport(LibraryName)]
    public static partial nuint JxlDecoderGetIntendedDownsamplingRatio(JxlDecoderPtr dec);


    /// <summary>
    /// Outputs progressive step towards the decoded image so far when only partial
    /// input was received. If the flush was successful, the buffer set with <see cref="JxlDecoderSetImageOutBuffer"/> will contain partial image data.
    /// <para>
    /// Can be called when <see cref="JxlDecoderProcessInput"/> returns <see cref="JxlDecoderStatus.NeedMoreInput"/>, after the <see cref="JxlDecoderStatus.Frame"/> event already occurred
    /// and before the <see cref="JxlDecoderStatus.FullImage"/> event occurred for a frame.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> if image data was flushed to the output buffer,
    /// or <see cref="JxlDecoderStatus.Error"/> when no flush was done, e.g. if not enough image
    /// data was available yet even for flush, or no output buffer was set yet.
    /// This error is not fatal, it only indicates no flushed image is available
    /// right now. Regular decoding can still be performed.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderFlushImage(JxlDecoderPtr dec);


    /// <summary>
    /// Sets the bit depth of the output buffer or callback.
    /// <para>
    /// Can be called after <see cref="JxlDecoderSetImageOutBuffer"/> or
    /// JxlDecoderSetImageOutCallback. For float pixel data types, only the default
    /// ::JXL_BIT_DEPTH_FROM_PIXEL_FORMAT setting is supported.
    /// </para>
    /// </summary>
    /// <param name="dec">decoder object</param>
    /// <param name="bit_depth">the bit depth setting of the pixel output</param>
    /// <returns><see cref="JxlDecoderStatus.Success"/> on success, <see cref="JxlDecoderStatus.Error"/> on error, such as
    /// incompatible custom bit depth and pixel data type.</returns>
    [LibraryImport(LibraryName)]
    public static partial JxlDecoderStatus JxlDecoderSetImageOutBitDepth(JxlDecoderPtr dec, in JxlBitDepth bit_depth);


}