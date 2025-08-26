using System.Runtime.InteropServices;

namespace Starward.Codec.JpegXL.CodeStream;

/// <summary>
/// The codestream preview header
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public struct JxlPreviewHeader
{
    /// <summary>
    /// Preview width in pixels
    /// </summary>
    public uint XSize;

    /// <summary>
    /// Preview height in pixels
    /// </summary>
    public uint YSize;
}
