namespace Starward.Codec.JpegXL.Decode;


/// <summary>
/// Function type for <see cref="JxlDecoderNativeMethod.JxlDecoderSetImageOutCallback"/>.
/// <para>
/// The callback may be called simultaneously by different threads when using a
/// threaded parallel runner, on different pixels.
/// </para>
/// </summary>
/// <param name="opaque">optional user data, as given to <see cref="JxlDecoderNativeMethod.JxlDecoderSetImageOutCallback"/>.</param>
/// <param name="x">horizontal position of leftmost pixel of the pixel data.</param>
/// <param name="y">vertical position of the pixel data.</param>
/// <param name="num_pixels">amount of pixels included in the pixel data, horizontally.
/// This is not the same as xsize of the full image, it may be smaller.</param>
/// <param name="pixels">pixel data as a horizontal stripe, in the format passed to <see cref="JxlDecoderNativeMethod.JxlDecoderSetImageOutCallback"/>. The memory is not owned by the user, and
/// is only valid during the time the callback is running.</param>
public unsafe delegate void JxlImageOutCallback(void* opaque, nuint x, nuint y, nuint num_pixels, void* pixels);


/// <summary>
/// Initialization callback for <see cref="JxlDecoderNativeMethod.JxlDecoderSetMultithreadedImageOutCallback"/>.
/// </summary>
/// <param name="init_opaque">optional user data, as given to <see cref="JxlDecoderNativeMethod.JxlDecoderSetMultithreadedImageOutCallback"/>.</param>
/// <param name="num_threads">maximum number of threads that will call the `run`
/// callback concurrently.</param>
/// <param name="num_pixels_per_thread">maximum number of pixels that will be passed in
/// one call to `run`.</param>
/// <returns>a pointer to data that will be passed to the `run` callback, or
/// <see langword="null"/> if initialization failed.</returns>
public unsafe delegate void* JxlImageOutInitCallback(void* init_opaque, nuint num_threads, nuint num_pixels_per_thread);


/// <summary>
/// Worker callback for <see cref="JxlDecoderNativeMethod.JxlDecoderSetMultithreadedImageOutCallback"/>.
/// </summary>
/// <param name="run_opaque">user data returned by the `init` callback.</param>
/// <param name="thread_id">number in `[0, num_threads)` identifying the thread of the
/// current invocation of the callback.</param>
/// <param name="x">horizontal position of the first (leftmost) pixel of the pixel data.</param>
/// <param name="y">vertical position of the pixel data.</param>
/// <param name="num_pixels">number of pixels in the pixel data. May be less than the
/// full xsize of the image, and will be at most equal to the `num_pixels_per_thread` that was passed to `init`.</param>
/// <param name="pixels">pixel data as a horizontal stripe, in the format passed to <see cref="JxlDecoderNativeMethod.JxlDecoderSetMultithreadedImageOutCallback"/>. The data pointed to
/// remains owned by the caller and is only guaranteed to outlive the current
/// callback invocation.</param>
public unsafe delegate void JxlImageOutRunCallback(void* run_opaque, nuint thread_id, nuint x, nuint y, nuint num_pixels, void* pixels);


/// <summary>
/// Destruction callback for <see cref="JxlDecoderNativeMethod.JxlDecoderSetMultithreadedImageOutCallback"/>,
/// called after all invocations of the `run` callback to perform any
/// appropriate clean-up of the `run_opaque` data returned by `init`.
/// </summary>
/// <param name="run_opaque">user data returned by the `init` callback.</param>
public unsafe delegate void JxlImageOutDestroyCallback(void* run_opaque);
