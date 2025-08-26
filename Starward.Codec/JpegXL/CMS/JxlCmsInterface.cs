using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Interface for performing colorspace transforms. The <see cref="init"/> function can be
/// called several times to instantiate several transforms, including before
/// other transforms have been destroyed.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public partial struct JxlCmsInterface
{
    /// <summary>
    /// CMS-specific data that will be passed to <see cref="set_fields_from_icc"/>.
    /// </summary>
    public IntPtr set_fields_data;

    /// <summary>
    /// Populates a <see cref="JxlColorEncoding"/> from an ICC profile. <see cref="JpegxlCmsSetFieldsFromIccFunc"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, byte*, nuint, JxlColorEncoding*, JxlBool*, JxlBool> set_fields_from_icc;

    /// <summary>
    /// CMS-specific data that will be passed to <see cref="init"/>.
    /// </summary>
    public IntPtr init_data;

    /// <summary>
    /// Prepares a colorspace transform as described in the documentation of <see cref="JpegxlCmsInitFunc"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, nuint, nuint, JxlColorProfile*, JxlColorProfile*, float, void*> init;

    /// <summary>
    /// Returns a buffer that can be used as input to <see cref="run"/>. <see cref="JpegxlCmsGetBufferFunc"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, nuint, float*> get_src_buf;

    /// <summary>
    /// Returns a buffer that can be used as output from <see cref="run"/>. <see cref="JpegxlCmsGetBufferFunc"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, nuint, float*> get_dst_buf;

    /// <summary>
    /// Executes the transform on a batch of pixels, per <see cref="JpegxlCmsRunFunc"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, nuint, float*, float*, nuint, JxlBool> run;

    /// <summary>
    /// Cleans up the transform. <see cref="JpegxlCmsDestroyFunc"/>
    /// </summary>
    public unsafe delegate* unmanaged[Cdecl]<void*, void> destroy;



    /// <summary>
    /// Parses an ICC profile and populates <paramref name="c"/> and <paramref name="cmyk"/> with the data.
    /// </summary>
    /// <param name="user_data"><see cref="JxlCmsInterface.set_fields_data"/> passed as-is.</param>
    /// <param name="icc_data">the ICC data to parse.</param>
    /// <param name="icc_size">how many bytes of icc_data are valid.</param>
    /// <param name="c">a <see cref="JxlColorEncoding"/> to populate if applicable.</param>
    /// <param name="cmyk">a boolean to set to whether the colorspace is a CMYK colorspace.</param>
    /// <returns>Whether the relevant fields in <paramref name="c"/> were successfully populated.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate JxlBool JpegxlCmsSetFieldsFromIccFunc(void* user_data, byte* icc_data, nuint icc_size, JxlColorEncoding* c, JxlBool* cmyk);


    /// <summary>
    /// Allocates and returns the data needed for <paramref name="num_threads"/> parallel transforms
    /// from the <paramref name="input_profile"/> colorspace to <paramref name="output_profile"/>, with up to <paramref name="pixels_per_thread"/>
    /// pixels to transform per call to <see cref="JxlCmsInterface.run"/>. <paramref name="init_data"/> comes
    /// directly from the <see cref="JxlCmsInterface"/> instance. Since <see cref="run"/> only receives
    /// the data returned by <see cref="init"/>, a reference to <paramref name="init_data"/> should be kept
    /// there if access to it is desired in <see cref="run"/>. Likewise for <see cref="JxlCmsInterface.destroy"/>.
    /// </summary>
    /// <param name="init_data"><see cref="JxlCmsInterface.init_data"/> passed as-is.</param>
    /// <param name="num_threads">the maximum number of threads from which <see cref="JxlCmsInterface.run"/> will be called.</param>
    /// <param name="pixels_per_thread">the maximum number of pixels that each call to <see cref="JxlCmsInterface.run"/> will have to transform.</param>
    /// <param name="input_profile">the input colorspace for the transform.</param>
    /// <param name="output_profile">the colorspace to which <see cref="JxlCmsInterface.run"/> should convert the input data.</param>
    /// <param name="intensity_target">for colorspaces where luminance is relative
    /// (essentially: not PQ), indicates the luminance at which (1, 1, 1) will
    /// be displayed. This is useful for conversions between PQ and a relative
    /// luminance colorspace, in either direction: <paramref name="intensity_target"/> cd/m²
    /// in PQ should map to and from (1, 1, 1) in the relative one.
    /// It is also used for conversions to and from HLG, as it is
    /// scene-referred while other colorspaces are assumed to be
    /// display-referred. That is, conversions from HLG should apply the OOTF
    /// for a peak display luminance of <paramref name="intensity_target"/>, and conversions
    /// to HLG should undo it. The OOTF is a gamma function applied to the
    /// luminance channel (https://www.itu.int/rec/R-REC-BT.2100-2-201807-I
    /// page 7), with the gamma value computed as
    /// <code>1.2 * 1.111^log2(intensity_target / 1000)</code>
    /// (footnote 2 page 8 of the same document).</param>
    /// <returns>The data needed for the transform, or <see langword="null"/> in case of failure.
    /// This will be passed to the other functions as <c>user_data</c>.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void* JpegxlCmsInitFunc(void* init_data, nuint num_threads, nuint pixels_per_thread, JxlColorProfile* input_profile, JxlColorProfile* output_profile, float intensity_target);


    /// <summary>
    /// Returns a buffer that can be used by callers of the interface to store the
    /// input of the conversion or read its result, if they pass it as the input or
    /// output of the <see cref="run"/> function.
    /// </summary>
    /// <param name="user_data">the data returned by <see cref="init"/>.</param>
    /// <param name="thread">the index of the thread for which to return a buffer.</param>
    /// <returns>A buffer that can be used by the caller for passing to <see cref="run"/>.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate float* JpegxlCmsGetBufferFunc(void* user_data, nuint thread);


    /// <summary>
    /// Executes one transform and returns true on success or false on error. It
    /// must be possible to call this from different threads with different values
    /// for <paramref name="thread"/>, all between 0 (inclusive) and the value of num_threads
    /// passed to init (exclusive). It is allowed to implement this by locking
    /// such that the transforms are essentially performed sequentially, if such a
    /// performance profile is acceptable. <paramref name="user_data"/> is the data returned by
    /// init.
    /// <para>
    /// The buffers each contain <c>num_pixels</c> × num_channels interleaved floating
    /// point (0..1) samples where num_channels is the number of color channels of
    /// their respective color profiles. It is guaranteed that the only case in which
    /// they might overlap is if the output has fewer channels than the input, in
    /// which case the pointers may be identical.
    /// </para>
    /// <para>
    /// For CMYK data, 0 represents the maximum amount of ink while 1 represents no
    /// ink.
    /// </para>
    /// </summary>
    /// <param name="user_data">the data returned by init.</param>
    /// <param name="thread">the index of the thread from which the function is being called.</param>
    /// <param name="input_buffer">the buffer containing the pixel data to be transformed.</param>
    /// <param name="output_buffer">the buffer receiving the transformed pixel data.</param>
    /// <param name="num_pixels">the number of pixels to transform from input to output.</param>
    /// <returns>true on success, false on failure.</returns>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate JxlBool JpegxlCmsRunFunc(void* user_data, nuint thread, float* input_buffer, float* output_buffer, nuint num_pixels);


    /// <summary>
    /// Performs the necessary clean-up and frees the memory allocated for user data.
    /// </summary>
    /// <param name="user_data">The data returned by <see cref="init"/></param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public unsafe delegate void JpegxlCmsDestroyFunc(void* user_data);



    [LibraryImport("jxl_cms")]
    private unsafe static partial JxlCmsInterface* JxlGetDefaultCms();


    /// <summary>
    /// Returns a default <see cref="JxlCmsInterface"/> that can be used to accurately
    /// transform colors.
    /// </summary>
    /// <returns></returns>
    public unsafe static JxlCmsInterface GetDefault()
    {
        return *JxlGetDefaultCms();
    }

}
