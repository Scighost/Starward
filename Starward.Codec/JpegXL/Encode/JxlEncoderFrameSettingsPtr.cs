using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// An opaque pointer to the JxlEncoderFrameSettings object.
/// <para>Settings and metadata for a single image frame. This includes encoder options for a frame such as compression quality and speed.</para>
/// <para>Allocated and initialized with <see cref="JxlEncoderNativeMethod.JxlEncoderFrameSettingsCreate"/>.</para>
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlEncoderFrameSettingsPtr
{
    private IntPtr _ptr;

    /// <summary>
    /// Converts a <see cref="JxlEncoderFrameSettingsPtr"/> to an <see cref="IntPtr"/>.
    /// </summary>
    /// <param name="ptr">The pointer to convert.</param>
    public static implicit operator IntPtr(JxlEncoderFrameSettingsPtr ptr) => ptr._ptr;

    /// <summary>
    /// Converts an <see cref="IntPtr"/> to a <see cref="JxlEncoderFrameSettingsPtr"/>.
    /// </summary>
    /// <param name="ptr">The pointer to convert.</param>
    public static implicit operator JxlEncoderFrameSettingsPtr(IntPtr ptr) => new JxlEncoderFrameSettingsPtr { _ptr = ptr };
}