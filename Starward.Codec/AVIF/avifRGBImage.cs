using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public struct avifRGBImage
{
    public uint width;                        // must match associated avifImage
    public uint height;                       // must match associated avifImage
    public uint depth;                        // legal depths [8, 10, 12, 16]. if depth>8, pixels must be uint16_t internally
    public avifRGBFormat format;                  // all channels are always full range
    public avifChromaUpsampling chromaUpsampling; // How to upsample from 4:2:0 or 4:2:2 UV when converting to RGB (ignored for 4:4:4 and 4:0:0).
                                                  // Ignored when converting to YUV. Defaults to AVIF_CHROMA_UPSAMPLING_AUTOMATIC.
    public avifChromaDownsampling chromaDownsampling; // How to downsample to 4:2:0 or 4:2:2 UV when converting from RGB (ignored for 4:4:4 and 4:0:0).
                                                      // Ignored when converting to RGB. Defaults to AVIF_CHROMA_DOWNSAMPLING_AUTOMATIC.
    public avifBool avoidLibYUV; // If AVIF_FALSE and libyuv conversion between RGB and YUV (including upsampling or downsampling if any)
                                 // is available for the avifImage/avifRGBImage combination, then libyuv is used. Default is AVIF_FALSE.
    public avifBool ignoreAlpha; // Used for XRGB formats, treats formats containing alpha (such as ARGB) as if they were RGB, treating
                                 // the alpha bits as if they were all 1.
    public avifBool alphaPremultiplied; // indicates if RGB value is pre-multiplied by alpha. Default: false
    public avifBool isFloat; // indicates if RGBA values are in half float (f16) format. Valid only when depth == 16. Default: false
    public int maxThreads; // Number of threads to be used for the YUV to RGB conversion. Note that this value is ignored for RGB to YUV
                           // conversion. Setting this to zero has the same effect as setting it to one. Negative values are invalid.
                           // Default: 1.

    public IntPtr pixels;
    public uint rowBytes;


    public unsafe ReadOnlySpan<byte> GetPixelBytes()
    {
        if (pixels == IntPtr.Zero)
        {
            return ReadOnlySpan<byte>.Empty;
        }
        else
        {
            return new ReadOnlySpan<byte>((void*)pixels, (int)(rowBytes * height));
        }
    }

}
