using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// Opaque structure that holds the JPEG XL encoder.
/// <para>Allocated and initialized with <see cref="JxlEncoderNativeMethod.JxlEncoderCreate(JxlMemoryManagerPtr)"/>.</para>
/// <para>Cleaned up and deallocated with <see cref="JxlEncoderNativeMethod.JxlEncoderDestroy"/>.</para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlEncoderPtr
{
    private IntPtr _ptr;

    /// <summary>
    /// Converts a <see cref="JxlEncoderPtr"/> to an <see cref="IntPtr"/>.
    /// </summary>
    /// <param name="handle">The pointer to convert.</param>
    public static implicit operator IntPtr(JxlEncoderPtr handle) => handle._ptr;

    /// <summary>
    /// Converts an <see cref="IntPtr"/> to a <see cref="JxlEncoderPtr"/>.
    /// </summary>
    /// <param name="ptr">The pointer to convert.</param>
    public static implicit operator JxlEncoderPtr(IntPtr ptr) => new JxlEncoderPtr { _ptr = ptr };
}