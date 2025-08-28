namespace Starward.Codec.AVIF;

// AVIF decoder struct. It may be extended in a future release. Code outside the libavif
// library must allocate avifDecoder by calling the avifDecoderCreate() function, and destroy it with
// avifDecoderDestroy().
// This struct contains three types of fields:
//   * Changeable settings, which users of the API may set.
//   * Output data fields, that are set by libavif and which users of the API may read.
//   * Internal fields, which users of the API should ignore.
public struct avifDecoder
{
    // --------------------------------------------------------------------------------------------
    // Inputs (changeable decoder settings)
    // Additional settings are available at the end of the struct after the version 1.1.0 end marker.

    // Defaults to AVIF_CODEC_CHOICE_AUTO: Preference determined by order in availableCodecs table (avif.c)
    public avifCodecChoice codecChoice;

    // Defaults to 1. If < 2, multithreading is disabled. See also 'Understanding maxThreads' above.
    public int maxThreads;

    // AVIF files can have multiple sets of images in them. This specifies which to decode.
    // Set this via avifDecoderSetSource().
    public avifDecoderSource requestedSource;

    // If this is true and a progressive AVIF is decoded, avifDecoder will behave as if the AVIF is
    // an image sequence, in that it will set imageCount to the number of progressive frames
    // available, and avifDecoderNextImage()/avifDecoderNthImage() will allow for specific layers
    // of a progressive image to be decoded. To distinguish between a progressive AVIF and an AVIF
    // image sequence, inspect avifDecoder.progressiveState.
    public avifBool allowProgressive;

    // If this is false, avifDecoderNextImage() will start decoding a frame only after there are
    // enough input bytes to decode all of that frame. If this is true, avifDecoder will decode each
    // subimage or grid cell as soon as possible. The benefits are: grid images may be partially
    // displayed before being entirely available, and the overall decoding may finish earlier.
    // Must be set before calling avifDecoderNextImage() or avifDecoderNthImage().
    // WARNING: Experimental feature.
    public avifBool allowIncremental;

    // Enable any of these to avoid reading and surfacing specific data to the decoded avifImage.
    // These can be useful if your avifIO implementation heavily uses AVIF_RESULT_WAITING_ON_IO for
    // streaming data, as some of these payloads are (unfortunately) packed at the end of the file,
    // which will cause avifDecoderParse() to return AVIF_RESULT_WAITING_ON_IO until it finds them.
    // If you don't actually leverage this data, it is best to ignore it here.
    public avifBool ignoreExif;
    public avifBool ignoreXMP;

    // This represents the maximum size of an image (in pixel count) that libavif and the underlying
    // AV1 decoder should attempt to decode. It defaults to AVIF_DEFAULT_IMAGE_SIZE_LIMIT, and can
    // be set to a smaller value. The value 0 is reserved.
    // Note: Only some underlying AV1 codecs support a configurable size limit (such as dav1d).
    public uint imageSizeLimit;

    // This represents the maximum dimension of an image (width or height) that libavif should
    // attempt to decode. It defaults to AVIF_DEFAULT_IMAGE_DIMENSION_LIMIT. Set it to 0 to ignore
    // the limit.
    public uint imageDimensionLimit;

    // This provides an upper bound on how many images the decoder is willing to attempt to decode,
    // to provide a bit of protection from malicious or malformed AVIFs citing millions upon
    // millions of frames, only to be invalid later. The default is AVIF_DEFAULT_IMAGE_COUNT_LIMIT
    // (see comment above), and setting this to 0 disables the limit.
    public uint imageCountLimit;

    // Strict flags. Defaults to AVIF_STRICT_ENABLED. See avifStrictFlag definitions above.
    public avifStrictFlag strictFlags;

    // --------------------------------------------------------------------------------------------
    // Outputs
    // Additional outputs are available at the end of the struct after the version 1.0.0 end marker.

    // All decoded image data; owned by the decoder. All information in this image is incrementally
    // added and updated as avifDecoder*() functions are called. After a successful call to
    // avifDecoderParse(), all values in decoder->image (other than the planes/rowBytes themselves)
    // will be pre-populated with all information found in the outer AVIF container, prior to any
    // AV1 decoding. If the contents of the inner AV1 payload disagree with the outer container,
    // these values may change after calls to avifDecoderRead*(),avifDecoderNextImage(), or
    // avifDecoderNthImage().
    //
    // The YUV and A contents of this image are likely owned by the decoder, so be sure to copy any
    // data inside of this image before advancing to the next image or reusing the decoder. It is
    // legal to call avifImageYUVToRGB() on this in between calls to avifDecoderNextImage(), but use
    // avifImageCopy() if you want to make a complete, permanent copy of this image's YUV content or
    // metadata.
    //
    // For each field among clap, irot and imir, if the corresponding avifTransformFlag is set, the
    // transform must be applied before rendering or converting the image, or forwarded along as
    // attached metadata.
    public unsafe avifImage* image;

    // Counts and timing for the current image in an image sequence. Uninteresting for single image files.
    public int imageIndex;                        // 0-based
    public int imageCount;                        // Always 1 for non-progressive, non-sequence AVIFs.
    public avifProgressiveState progressiveState; // See avifProgressiveState declaration
    public avifImageTiming imageTiming;           //
    public ulong timescale;                    // timescale of the media (Hz)
    public double duration;                       // duration of a single playback of the image sequence in seconds
                                                  // (durationInTimescales / timescale)
    public ulong durationInTimescales;         // duration of a single playback of the image sequence in "timescales"
    public int repetitionCount;                   // number of times the sequence has to be repeated. This can also be one of
                                                  // AVIF_REPETITION_COUNT_INFINITE or AVIF_REPETITION_COUNT_UNKNOWN. Essentially, if
                                                  // repetitionCount is a non-negative integer `n`, then the image sequence should be
                                                  // played back `n + 1` times.

    // This is true when avifDecoderParse() detects an alpha plane. Use this to find out if alpha is
    // present after a successful call to avifDecoderParse(), but prior to any call to
    // avifDecoderNextImage() or avifDecoderNthImage(), as decoder->image->alphaPlane won't exist yet.
    public avifBool alphaPresent;

    // stats from the most recent read, possibly 0s if reading an image sequence
    public avifIOStats ioStats;

    // Additional diagnostics (such as detailed error state)
    public avifDiagnostics diag;

    // --------------------------------------------------------------------------------------------
    // Internals

    // IO source. This field is managed by the decoder. Use one of the avifDecoderSetIO*() functions to set it.
    public unsafe avifIO* io;

    // Internals used by the decoder
    private IntPtr data;

    // Version 1.0.0 ends here.
    // --------------------------------------------------------------------------------------------

    // This is true when avifDecoderParse() detects an image sequence track in the image. If this is true, the image can be
    // decoded either as an animated image sequence or as a still image (the primary image item) by setting avifDecoderSetSource
    // to the appropriate source.
    public avifBool imageSequenceTrackPresent; // Output data field.

    // Version 1.1.0 ends here.
    // --------------------------------------------------------------------------------------------

    // Image content to decode (if present). Defaults to AVIF_IMAGE_CONTENT_DECODE_DEFAULT.
    public avifImageContentTypeFlag imageContentToDecode; // Changeable decoder setting.

    // Version 1.2.0 ends here. Add any new members after this line.
    // --------------------------------------------------------------------------------------------
}
