using System.Runtime.InteropServices;

namespace Starward.Codec.AVIF;

[StructLayout(LayoutKind.Sequential)]
public struct avifGainMap
{
    // Gain map pixels.
    // Owned by the avifGainMap and gets freed when calling avifGainMapDestroy().
    // Used fields: width, height, depth, yuvFormat, yuvRange,
    // yuvChromaSamplePosition, yuvPlanes, yuvRowBytes, imageOwnsYUVPlanes,
    // matrixCoefficients. The colorPrimaries and transferCharacteristics fields
    // shall be 2. Other fields are ignored.
    public unsafe avifImage* image;

    // Gain map metadata used to interpret and apply the gain map pixel data.
    // When encoding an image grid, all metadata below shall be identical for all
    // cells.

    // Parameters for converting the gain map from its image encoding to log2 space.
    // gainMapLog2 = lerp(gainMapMin, gainMapMax, pow(gainMapEncoded, gainMapGamma));
    // where 'lerp' is a linear interpolation function.
    // Minimum value in the gain map, log2-encoded, per RGB channel.
    public FixedArray3<avifSignedFraction> gainMapMin;
    // Maximum value in the gain map, log2-encoded, per RGB channel.
    public FixedArray3<avifSignedFraction> gainMapMax;
    // Gain map gamma value with which the gain map was encoded, per RGB channel.
    // For decoding, the inverse value (1/gamma) should be used.
    public FixedArray3<avifSignedFraction> gainMapGamma;

    // Parameters used in gain map computation/tone mapping to avoid numerical
    // instability.
    // toneMappedLinear = ((baseImageLinear + baseOffset) * exp(gainMapLog * w)) - alternateOffset;
    // Where 'w' is a weight parameter based on the display's HDR capacity
    // (see below).

    // Offset constants for the base image, per RGB channel.
    public FixedArray3<avifSignedFraction> baseOffset;
    // Offset constants for the alternate image, per RGB channel.
    public FixedArray3<avifSignedFraction> alternateOffset;

    // Log2-encoded HDR headroom of the base and alternate images respectively.
    // If baseHdrHeadroom is < alternateHdrHeadroom, the result of tone mapping
    // for a display with an HDR headroom that is <= baseHdrHeadroom is the base
    // image, and the result of tone mapping for a display with an HDR headroom >=
    // alternateHdrHeadroom is the alternate image.
    // Conversely, if baseHdrHeadroom is > alternateHdrHeadroom, the result of
    // tone mapping for a display with an HDR headroom that is >= baseHdrHeadroom
    // is the base image, and the result of tone mapping for a display with an HDR
    // headroom <= alternateHdrHeadroom is the alternate image.
    // For a display with a capacity between baseHdrHeadroom and alternateHdrHeadroom,
    // tone mapping results in an interpolation between the base and alternate
    // versions. baseHdrHeadroom and alternateHdrHeadroom can be tuned to change how
    // the gain map should be applied.
    //
    // If 'H' is the display's current log2-encoded HDR capacity (HDR to SDR ratio),
    // then the weight 'w' to apply the gain map is computed as follows:
    // f = clamp((H - baseHdrHeadroom) /
    //           (alternateHdrHeadroom - baseHdrHeadroom), 0, 1);
    // w = sign(alternateHdrHeadroom - baseHdrHeadroom) * f
    public avifUnsignedFraction baseHdrHeadroom;
    public avifUnsignedFraction alternateHdrHeadroom;

    // True if tone mapping should be performed in the color space of the
    // base image. If false, the color space of the alternate image should
    // be used.
    public avifBool useBaseColorSpace;

    // Colorimetry of the alternate image (ICC profile and/or CICP information
    // of the alternate image that the gain map was created from).
    public avifRWData altICC;
    public avifColorPrimaries altColorPrimaries;
    public avifTransferCharacteristics altTransferCharacteristics;
    public avifMatrixCoefficients altMatrixCoefficients;
    public avifRange altYUVRange;

    // Hint on the approximate amount of colour resolution available after fully
    // applying the gain map ('pixi' box content of the alternate image that the
    // gain map was created from).
    public uint altDepth;
    public uint altPlaneCount;

    // Optimal viewing conditions of the alternate image ('clli' box content
    // of the alternate image that the gain map was created from).
    public avifContentLightLevelInformationBox altCLLI;

    // Version 1.2.0 ends here. Add any new members after this line.
}
