using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;


public enum avifPixelFormat
{
    /// <summary>
    /// No YUV pixels are present. Alpha plane can still be present.
    /// </summary>
    None = 0,

    YUV444,

    YUV422,

    YUV420,

    YUV400,

    Count,
}


[StructLayout(LayoutKind.Sequential)]
public struct avifPixelFormatInfo
{
    public avifBool Monochrome;

    public int ChromaShiftX;

    public int ChromaShiftY;
}