using Starward.Codec.JpegXL.CMS;
using Starward.Codec.JpegXL.CodeStream;

namespace Starward.Codec.JpegXL.Encode;

/// <summary>
/// A simplified, high-level API for encoding a single image to JPEG XL.
/// </summary>
public class JxlEncoderLite
{

    /// <summary>
    /// Encodes a raw image buffer into a JPEG XL byte array.
    /// </summary>
    /// <param name="bytes">The raw pixel data.</param>
    /// <param name="width">The width of the image.</param>
    /// <param name="height">The height of the image.</param>
    /// <param name="colorEncoding">The color encoding of the source image.</param>
    /// <param name="pixelFormat">The pixel format of the source image.</param>
    /// <param name="alphaPremultiplied">Whether the alpha channel is premultiplied.</param>
    /// <param name="distance">The Butteraugli distance for the encoding (0.0 is lossless, 1.0 is visually lossless).</param>
    /// <returns>A byte array containing the compressed JPEG XL image.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> bytes, uint width, uint height, JxlColorEncoding colorEncoding, JxlPixelFormat pixelFormat, bool alphaPremultiplied, float distance)
    {
        using var encoder = new JxlEncoder();
        encoder.SetBasicInfo(new JxlBasicInfo(width, height, pixelFormat, alphaPremultiplied));
        encoder.SetColorEncoding(colorEncoding);
        var frameSettings = encoder.CreateFrameSettings();
        frameSettings.Distance = distance;
        frameSettings.AddImageFrame(pixelFormat, bytes);
        return encoder.Encode();
    }


}
