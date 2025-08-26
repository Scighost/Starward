using Starward.Codec.JpegXL.CMS;
using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Encode;



/// <summary>
/// Function type for JxlEncoderSetDebugImageCallback.
/// <para>
/// The callback may be called simultaneously by different threads when using a
/// threaded parallel runner, on different debug images.
/// </para>
/// </summary>
/// <param name="opaque">optional user data, as given to
/// <see cref="JxlEncoderNativeMethod.JxlEncoderSetDebugImageCallback"/>.</param>
/// <param name="label">label of debug image, can be used in filenames</param>
/// <param name="xsize">width of debug image</param>
/// <param name="ysize">height of debug image</param>
/// <param name="color">color encoding of debug image</param>
/// <param name="pixels">pixel data of debug image as big-endian 16-bit unsigned
/// samples. The memory is not owned by the user, and is only valid during the
/// time the callback is running.</param>
[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
public unsafe delegate void JxlDebugImageCallback(void* opaque, byte* label, nuint xsize, nuint ysize, JxlColorEncoding* color, ushort* pixels);

