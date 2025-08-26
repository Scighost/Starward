using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Decode;

/// <summary>
/// Opaque structure that holds the JPEG XL decoder.
/// Allocated and initialized with <see cref="JxlDecoderNativeMethod.JxlDecoderCreate"/>.
/// Cleaned up and deallocated with <see cref="JxlDecoderNativeMethod.JxlDecoderDestroy"/>.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlDecoderPtr
{
    private IntPtr _ptr;

    /// <summary>
    /// Casts a <see cref="JxlDecoderPtr"/> to an <see cref="IntPtr"/>.
    /// </summary>
    /// <param name="handle"></param>
    public static implicit operator IntPtr(JxlDecoderPtr handle) => handle._ptr;

    /// <summary>
    /// Casts an <see cref="IntPtr"/> to a <see cref="JxlDecoderPtr"/>.
    /// </summary>
    /// <param name="ptr"></param>
    public static implicit operator JxlDecoderPtr(IntPtr ptr) => new JxlDecoderPtr { _ptr = ptr };
}
