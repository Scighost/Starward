using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CMS;

/// <summary>
/// Represents an input or output colorspace to a color transform, as a serialized ICC profile.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlColorProfile
{
    /// <summary>
    /// The serialized ICC profile. This is guaranteed to be present and valid.
    /// </summary>
    public JxlIccData ICC;

    /// <summary>
    /// Structured representation of the colorspace, if applicable.
    /// </summary>
    public JxlColorEncoding ColorEncoding;

    /// <summary>
    /// Number of components per pixel.
    /// </summary>
    public nuint NumChannels;


    /// <summary>
    /// The serialized ICC data. This is guaranteed to be present and valid.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public record struct JxlIccData
    {
        /// <summary>
        /// Pointer to the ICC profile data.
        /// </summary>
        public IntPtr Data;

        /// <summary>
        /// The size of the ICC profile data in bytes.
        /// </summary>
        public nuint Size;
    }
}



