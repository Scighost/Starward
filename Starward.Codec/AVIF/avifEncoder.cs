namespace Starward.Codec.AVIF;


// AVIF encoder struct. It may be extended in a future release. Code outside the libavif library
// must allocate avifEncoder by calling the avifEncoderCreate() function, and destroy it with
// avifEncoderDestroy().
// This struct contains three types of fields:
//   * Changeable settings, which users of the API may set.
//   * Output data fields, that are set by libavif and which users of the API may read.
//   * Internal fields, which users of the API should ignore.
// Some encoder settings can be changed after encoding starts. Changes will take effect in the next
// call to avifEncoderAddImage().
public struct avifEncoder
{
    // --------------------------------------------------------------------------------------------
    // Changeable encoder settings
    // Additional settings are available at the end of the struct after the version 1.0.0 end marker.

    // Defaults to AVIF_CODEC_CHOICE_AUTO: Preference determined by order in availableCodecs table (avif.c)
    public avifCodecChoice codecChoice;

    // Defaults to 1. If < 2, multithreading is disabled. See also 'Understanding maxThreads' above.
    public int maxThreads;
    // Speed range: [AVIF_SPEED_SLOWEST - AVIF_SPEED_FASTEST]. Slower should make for a better quality
    // image in fewer bytes. AVIF_SPEED_DEFAULT means "Leave the AV1 codec to its default speed settings".
    // If avifEncoder uses rav1e, the speed value is directly passed through (0-10). If libaom is used,
    // a combination of settings are tweaked to simulate this speed range.
    public int speed;

    // For image sequences (animations), maximum interval between keyframes. Any set of |keyframeInterval|
    // consecutive frames will have at least one keyframe. When it is 0, no restriction is applied.
    public int keyframeInterval;
    // For image sequences (animations), timescale of the media in Hz, i.e. the number of time units per second.
    public ulong timescale;
    // For image sequences, number of times the image sequence should be repeated. This can also be set to
    // AVIF_REPETITION_COUNT_INFINITE for infinite repetitions.
    // Essentially, if repetitionCount is a non-negative integer `n`, then the image sequence should be
    // played back `n + 1` times. Defaults to AVIF_REPETITION_COUNT_INFINITE.
    public int repetitionCount;

    // EXPERIMENTAL: A non-zero value indicates a layered (progressive) image.
    // Range: [0 - (AVIF_MAX_AV1_LAYER_COUNT-1)].
    // To encode a progressive image, set `extraLayerCount` to the number of extra images, then call
    // `avifEncoderAddImage()` or `avifEncoderAddImageGrid()` exactly `encoder->extraLayerCount+1` times.
    public uint extraLayerCount;

    // Encode quality for the YUV image, in [AVIF_QUALITY_WORST - AVIF_QUALITY_BEST].
    public int quality;
    // Encode quality for the alpha layer if present, in [AVIF_QUALITY_WORST - AVIF_QUALITY_BEST].
    public int qualityAlpha;
    public int minQuantizer;      // Deprecated, use `quality` instead.
    public int maxQuantizer;      // Deprecated, use `quality` instead.
    public int minQuantizerAlpha; // Deprecated, use `qualityAlpha` instead.
    public int maxQuantizerAlpha; // Deprecated, use `qualityAlpha` instead.

    // Tiling splits the image into a grid of smaller images (tiles), allowing parallelization of
    // encoding/decoding and/or incremental decoding. Tiling also allows encoding larger images.
    // To enable tiling, set tileRowsLog2 > 0 and/or tileColsLog2 > 0, or set autoTiling to AVIF_TRUE.
    // Range: [0-6], where the value indicates a request for 2^n tiles in that dimension.
    public int tileRowsLog2;
    public int tileColsLog2;
    // If autoTiling is set to AVIF_TRUE, libavif ignores tileRowsLog2 and tileColsLog2 and
    // automatically chooses suitable tiling values.
    public avifBool autoTiling;

    // Up/down scaling of the image to perform before encoding.
    public avifScalingMode scalingMode;

    // --------------------------------------------------------------------------------------------
    // Outputs

    // Stats from the most recent write.
    public avifIOStats ioStats;

    // Additional diagnostics (such as detailed error state).
    public avifDiagnostics diag;

    // --------------------------------------------------------------------------------------------
    // Internals

    private IntPtr data;
    private IntPtr csOptions;

    // Version 1.0.0 ends here.
    // --------------------------------------------------------------------------------------------

    // Defaults to AVIF_HEADER_DEFAULT
    public int headerFormat; // Changeable encoder setting.

    // Version 1.1.0 ends here.
    // --------------------------------------------------------------------------------------------

    // Encode quality for the gain map image if present, in [AVIF_QUALITY_WORST - AVIF_QUALITY_BEST].
    public int qualityGainMap; // Changeable encoder setting.

    // Version 1.2.0 ends here. Add any new members after this line.
    // --------------------------------------------------------------------------------------------

#if AVIF_ENABLE_EXPERIMENTAL_SAMPLE_TRANSFORM
    // Perform extra steps at encoding and decoding to extend AV1 features using bundled additional image items.
    avifSampleTransformRecipe sampleTransformRecipe; // Changeable encoder setting.
#endif
}
